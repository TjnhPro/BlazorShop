using System.Net;
using System.Threading.RateLimiting;

using BlazorShop.Application.ControlPlane;
using BlazorShop.ControlPlane.API.Authorization;
using BlazorShop.ControlPlane.API.Middleware;
using BlazorShop.Infrastructure.Data.ControlPlane;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration
    .GetSection("ControlPlane:Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.AddServiceDefaults();

ValidateProductionConfiguration(builder.Configuration, builder.Environment, allowedOrigins);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
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

var app = builder.Build();

if (ShouldMigrateDatabase(app))
{
    await BlazorShop.ControlPlane.API.ControlPlaneDatabaseBootstrapper.MigrateAsync(app.Services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();
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
