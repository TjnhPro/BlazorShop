using System.Net;
using System.Threading.RateLimiting;

using BlazorShop.Application.ControlPlane;
using BlazorShop.ControlPlane.API.Authorization;
using BlazorShop.ControlPlane.API.Middleware;
using BlazorShop.ControlPlane.API.Responses;
using BlazorShop.Infrastructure.Data.ControlPlane;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration
    .GetSection("ControlPlane:Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.AddServiceDefaults();

ValidateProductionConfiguration(builder.Configuration, builder.Environment, allowedOrigins);

builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(
        options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value?.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The request value is invalid."
                                : error.ErrorMessage)
                            .ToArray() ?? []);

                return ControlPlaneApiResponseWriter.Failure(
                    StatusCodes.Status400BadRequest,
                    "Validation failed.",
                    new { errors });
            };
        });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ForwardedHeadersOptions>(
    options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                   | ForwardedHeaders.XForwardedProto
                                   | ForwardedHeaders.XForwardedHost;

        foreach (var proxy in builder.Configuration.GetSection("ControlPlane:ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }
    });
builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            await ControlPlaneApiResponseWriter.WriteFailureAsync(
                context.HttpContext,
                StatusCodes.Status429TooManyRequests,
                "Too many Control Plane requests. Try again shortly.",
                ControlPlaneApiResponseWriter.CreateCorrelationData(context.HttpContext),
                cancellationToken);
        };

        options.AddFixedWindowLimiter(
            "control-plane-api",
            limiterOptions =>
            {
                limiterOptions.PermitLimit = Math.Clamp(builder.Configuration.GetValue("ControlPlane:RateLimiting:PermitLimit", 120), 1, 10_000);
                limiterOptions.Window = TimeSpan.FromSeconds(Math.Clamp(builder.Configuration.GetValue("ControlPlane:RateLimiting:WindowSeconds", 60), 1, 3600));
                limiterOptions.QueueLimit = Math.Clamp(builder.Configuration.GetValue("ControlPlane:RateLimiting:QueueLimit", 0), 0, 1000);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
    });
builder.Services.AddControlPlaneAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ControlPlaneWeb",
        policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else
            {
                policy.AllowAnyHeader()
                    .AllowAnyMethod();
            }
        });
});
builder.Services.AddControlPlaneApplication(builder.Configuration);
builder.Services.AddControlPlaneInfrastructure(builder.Configuration);
builder.Services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.Events ??= new JwtBearerEvents();
        options.Events.OnChallenge = async context =>
        {
            context.HandleResponse();
            await ControlPlaneApiResponseWriter.WriteFailureAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Sign in with a Control Plane account to continue.",
                ControlPlaneApiResponseWriter.CreateCorrelationData(context.HttpContext));
        };

        options.Events.OnForbidden = async context =>
        {
            await ControlPlaneApiResponseWriter.WriteFailureAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Your Control Plane account does not have permission for this action.",
                ControlPlaneApiResponseWriter.CreateCorrelationData(context.HttpContext));
        };
    });

var app = builder.Build();

if (ShouldMigrateDatabase(app))
{
    await BlazorShop.ControlPlane.API.ControlPlaneDatabaseBootstrapper.MigrateAsync(app.Services);
}

app.UseExceptionHandler(
    exceptionApp =>
    {
        exceptionApp.Run(
            async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("BlazorShop.ControlPlane.API.ExceptionHandler");
                logger.LogError(
                    "Unhandled Control Plane API exception for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);

                await ControlPlaneApiResponseWriter.WriteFailureAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "An unexpected Control Plane error occurred.",
                    ControlPlaneApiResponseWriter.CreateCorrelationData(context));
            });
    });

app.UseStatusCodePages(
    async context =>
    {
        var httpContext = context.HttpContext;
        if (httpContext.Response.HasStarted
            || httpContext.Response.ContentLength.HasValue
            || !ShouldWriteStatusCodeEnvelope(httpContext.Response.StatusCode))
        {
            return;
        }

        await ControlPlaneApiResponseWriter.WriteFailureAsync(
            httpContext,
            httpContext.Response.StatusCode,
            ResolveStatusCodeMessage(httpContext.Response.StatusCode),
            ControlPlaneApiResponseWriter.CreateCorrelationData(httpContext));
    });
app.UseForwardedHeaders();
app.UseMiddleware<ControlPlaneCorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("ControlPlaneWeb");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("control-plane-api");
app.MapDefaultEndpoints();

app.Run();

static void ValidateProductionConfiguration(IConfiguration configuration, IWebHostEnvironment environment, string[] allowedOrigins)
{
    if (environment.IsDevelopment())
    {
        return;
    }

    var missing = new List<string>();
    var controlPlaneConnection = configuration.GetConnectionString("ControlPlaneConnection");
    var jwtKey = configuration["Jwt:Key"];

    if (string.IsNullOrWhiteSpace(controlPlaneConnection))
    {
        missing.Add("ConnectionStrings:ControlPlaneConnection");
    }

    if (string.IsNullOrWhiteSpace(jwtKey)
        || jwtKey.Contains("<set-", StringComparison.OrdinalIgnoreCase)
        || jwtKey.Contains("development-only", StringComparison.OrdinalIgnoreCase))
    {
        missing.Add("Jwt:Key");
    }

    if (allowedOrigins.Length == 0)
    {
        missing.Add("ControlPlane:Cors:AllowedOrigins");
    }

    if (missing.Count > 0)
    {
        throw new InvalidOperationException($"Control Plane production configuration is missing or unsafe: {string.Join(", ", missing)}.");
    }
}

static bool ShouldMigrateDatabase(WebApplication app)
{
    return app.Configuration.GetValue("ControlPlane:Database:MigrateOnStartup", app.Environment.IsDevelopment());
}

static bool ShouldWriteStatusCodeEnvelope(int statusCode)
{
    return statusCode is >= 400 and < 600;
}

static string ResolveStatusCodeMessage(int statusCode)
{
    return statusCode switch
    {
        StatusCodes.Status400BadRequest => "The Control Plane request is invalid.",
        StatusCodes.Status401Unauthorized => "Sign in with a Control Plane account to continue.",
        StatusCodes.Status403Forbidden => "Your Control Plane account does not have permission for this action.",
        StatusCodes.Status404NotFound => "The requested Control Plane resource was not found.",
        StatusCodes.Status409Conflict => "The Control Plane request conflicts with the current state.",
        StatusCodes.Status429TooManyRequests => "Too many Control Plane requests. Try again shortly.",
        StatusCodes.Status500InternalServerError => "An unexpected Control Plane error occurred.",
        _ => "The Control Plane request could not be completed."
    };
}
