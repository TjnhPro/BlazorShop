using System.Security.Claims;
using System.Threading.RateLimiting;

using BlazorShop.CommerceNode.API.Configuration;
using BlazorShop.CommerceNode.API.Deployment;
using BlazorShop.CommerceNode.API.Endpoints;
using BlazorShop.CommerceNode.API.Middleware;
using BlazorShop.CommerceNode.API.ProductMedia;
using BlazorShop.CommerceNode.API.Responses;
using BlazorShop.CommerceNode.API.Swagger;
using BlazorShop.CommerceNode.API.Tasks;
using BlazorShop.CommerceNode.API.Workers;
using BlazorShop.Application.CommerceNode.Media;
using BlazorShop.Application.CommerceNode.Tasks;
using BlazorShop.Infrastructure.Data.CommerceNode;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var runtimeOptions = builder.Configuration
    .GetSection(CommerceNodeRuntimeOptions.SectionName)
    .Get<CommerceNodeRuntimeOptions>() ?? new CommerceNodeRuntimeOptions();

builder.AddServiceDefaults();

builder.Services.AddOptions<CommerceNodeOptions>()
    .Bind(builder.Configuration.GetSection(CommerceNodeOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<CommerceNodeOptions>, CommerceNodeOptionsValidator>();
builder.Services.AddOptions<CommerceNodeRuntimeOptions>()
    .Bind(builder.Configuration.GetSection(CommerceNodeRuntimeOptions.SectionName));
builder.Services.ConfigureOptions<CommerceNodeForwardedHeadersOptionsSetup>();
builder.Services.AddOptions<CommerceTaskWorkerOptions>()
    .Bind(builder.Configuration.GetSection(CommerceTaskWorkerOptions.SectionName));
builder.Services.AddOptions<StorefrontDeploymentOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontDeploymentOptions.SectionName));
builder.Services.AddOptions<NginxDeploymentOptions>()
    .Bind(builder.Configuration.GetSection(NginxDeploymentOptions.SectionName));
builder.Services.AddOptions<ProductMediaStorageOptions>()
    .Bind(builder.Configuration.GetSection(ProductMediaStorageOptions.SectionName));
builder.Services.AddOptions<CommerceMediaStorageOptions>()
    .Bind(builder.Configuration.GetSection(CommerceMediaStorageOptions.SectionName));
builder.Services.AddCommerceNodeInfrastructure(builder.Configuration);
builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.Events ??= new JwtBearerEvents();
        options.Events.OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new CommerceNodeApiErrorResponse(
                false,
                "auth.unauthenticated",
                "Authentication is required.",
                context.HttpContext.TraceIdentifier));
        };
    });
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IProductMediaDownloader, ProductMediaDownloader>();
builder.Services.AddSingleton<IStorefrontDockerDeploymentService, StorefrontDockerDeploymentService>();
builder.Services.AddSingleton<INginxDeploymentService, NginxDeploymentService>();
builder.Services.AddScoped<ICommerceTaskHandler, CompleteTestCommerceTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, FailTestCommerceTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, WaitTestCommerceTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, StoreCreateAndDeployTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, ProductMediaImportTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, ProductImportTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, CurrencyExchangeRateUpdateTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, MessageDeliverTaskHandler>();
builder.Services.AddScoped<ICommerceTaskHandler, OrderCreatedTaskHandler>();
builder.Services.AddHostedService<CommerceTaskWorker>();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var fieldErrors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The field is invalid."
                            : error.ErrorMessage)
                        .ToArray());

            return new BadRequestObjectResult(new CommerceNodeApiErrorResponse(
                false,
                "validation.failed",
                "The request validation failed.",
                context.HttpContext.TraceIdentifier,
                fieldErrors));
        };
    });
builder.Services.AddAuthorization();
if (runtimeOptions.RateLimiting.Enabled)
{
    builder.Services.AddRateLimiter(options => ConfigureStorefrontRateLimiter(options, runtimeOptions.RateLimiting));
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCommerceNodeSwagger();

var app = builder.Build();

if (ShouldMigrateDatabase(app))
{
    await BlazorShop.CommerceNode.API.CommerceNodeDatabaseBootstrapper.MigrateAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.UseCommerceNodeSwaggerUi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(CreateUploadsStaticFileOptions(uploadsPath));

app.MapGet("/", () => Results.Redirect("/swagger"));
app.UseWhen(
    context => StorefrontStoreScopeMiddleware.IsStorefrontOrPublicMediaPath(context.Request.Path),
    branch => branch.UseMiddleware<StorefrontStoreScopeMiddleware>());
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/commerce"),
    branch => branch.UseMiddleware<CommerceNodeCredentialMiddleware>());
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/commerce/admin"),
    branch => branch.UseMiddleware<CommerceAdminStoreScopeMiddleware>());
app.Use(
    async (context, next) =>
    {
        if (IsStorefrontMutation(context.Request))
        {
            context.Response.OnStarting(
                static state =>
                {
                    var response = (HttpResponse)state;
                    response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
                    response.Headers["X-Robots-Tag"] = "noindex, nofollow";
                    return Task.CompletedTask;
                },
                context.Response);
        }

        await next();
    });
app.UseAuthentication();
if (runtimeOptions.RateLimiting.Enabled)
{
    app.UseRateLimiter();
}

app.UseAuthorization();
app.MapCommerceHealthEndpoints();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

static bool ShouldMigrateDatabase(WebApplication app)
{
    return app.Configuration.GetValue("CommerceNode:Database:MigrateOnStartup", app.Environment.IsDevelopment());
}

static StaticFileOptions CreateUploadsStaticFileOptions(string uploadsPath)
{
    var contentTypeProvider = new FileExtensionContentTypeProvider();
    contentTypeProvider.Mappings.Clear();
    contentTypeProvider.Mappings[".jpg"] = "image/jpeg";
    contentTypeProvider.Mappings[".jpeg"] = "image/jpeg";
    contentTypeProvider.Mappings[".png"] = "image/png";
    contentTypeProvider.Mappings[".webp"] = "image/webp";
    contentTypeProvider.Mappings[".gif"] = "image/gif";
    contentTypeProvider.Mappings[".bmp"] = "image/bmp";

    return new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        ContentTypeProvider = contentTypeProvider,
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            context.Context.Response.Headers.XContentTypeOptions = "nosniff";
        },
    };
}

static void ConfigureStorefrontRateLimiter(RateLimiterOptions options, CommerceNodeRateLimitingOptions rateLimitOptions)
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        await httpContext.Response.WriteAsJsonAsync(
            new CommerceNodeApiErrorResponse(
                false,
                "rate_limit_exceeded",
                "Too many Storefront requests. Try again shortly.",
                httpContext.TraceIdentifier),
            cancellationToken);
    };

    options.AddPolicy(
        StorefrontRateLimitPolicyNames.AuthStrict,
        httpContext => CreateStorefrontRateLimitPartition(httpContext, rateLimitOptions.AuthStrict));
    options.AddPolicy(
        StorefrontRateLimitPolicyNames.Cart,
        httpContext => CreateStorefrontRateLimitPartition(httpContext, rateLimitOptions.Cart));
    options.AddPolicy(
        StorefrontRateLimitPolicyNames.Checkout,
        httpContext => CreateStorefrontRateLimitPartition(httpContext, rateLimitOptions.Checkout));
    options.AddPolicy(
        StorefrontRateLimitPolicyNames.Newsletter,
        httpContext => CreateStorefrontRateLimitPartition(httpContext, rateLimitOptions.Newsletter));
    options.AddPolicy(
        StorefrontRateLimitPolicyNames.Currency,
        httpContext => CreateStorefrontRateLimitPartition(httpContext, rateLimitOptions.Currency));
}

static RateLimitPartition<string> CreateStorefrontRateLimitPartition(
    HttpContext httpContext,
    CommerceNodeRateLimitPolicyOptions policyOptions)
{
    var storeKey = httpContext.Request.RouteValues.TryGetValue("storeKey", out var routeStoreKey)
        ? routeStoreKey?.ToString()
        : null;
    var route = httpContext.GetEndpoint()?.DisplayName
        ?? httpContext.Request.Path.Value
        ?? "unknown-route";
    var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var actor = httpContext.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(userId)
        ? $"user:{userId}"
        : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    var partitionKey = string.Join(
        '|',
        string.IsNullOrWhiteSpace(storeKey) ? "unknown-store" : storeKey,
        route,
        actor);

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Clamp(policyOptions.PermitLimit, 1, 10_000),
            Window = TimeSpan.FromSeconds(Math.Clamp(policyOptions.WindowSeconds, 1, 3600)),
            QueueLimit = Math.Clamp(policyOptions.QueueLimit, 0, 1000),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
}

static bool IsStorefrontMutation(HttpRequest request)
{
    return request.Path.StartsWithSegments("/api/storefront")
        && !HttpMethods.IsGet(request.Method)
        && !HttpMethods.IsHead(request.Method)
        && !HttpMethods.IsOptions(request.Method);
}

public partial class Program
{
}
