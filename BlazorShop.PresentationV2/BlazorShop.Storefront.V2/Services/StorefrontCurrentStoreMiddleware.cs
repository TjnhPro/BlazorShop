namespace BlazorShop.Storefront.Services
{
    using System.IO;

    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.Extensions.Options;

    public sealed class StorefrontCurrentStoreMiddleware
    {
        private static readonly string[] ExcludedPrefixes =
        [
            "/api",
            "/_",
            "/css",
            "/js",
            "/images",
            "/uploads",
            "/favicon",
            "/icon-",
            "/lib",
            "/assets",
            "/manifest",
        ];

        private readonly RequestDelegate _next;
        private readonly ILogger<StorefrontCurrentStoreMiddleware> _logger;

        public StorefrontCurrentStoreMiddleware(RequestDelegate next, ILogger<StorefrontCurrentStoreMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IStorefrontCurrentStoreProvider currentStoreProvider,
            IOptions<StorefrontStoreResolutionOptions> options,
            IHostEnvironment hostEnvironment,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!StorefrontStoreResolutionOptions.IsCurrentStoreRequired(options.Value, hostEnvironment)
                || ShouldSkip(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var resolution = await currentStoreProvider.ResolveAsync(context.RequestAborted);
            if (resolution.Status == StorefrontCurrentStoreResolutionStatus.Success)
            {
                await _next(context);
                return;
            }

            var storeKey = StorefrontStoreKeyResolver.Resolve(configuration) ?? "(missing)";
            _logger.LogWarning(
                "Storefront current store guard blocked {Method} {Path} for store key {StoreKey} with {Status}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                storeKey,
                resolution.Status,
                context.TraceIdentifier);

            if (resolution.Status == StorefrontCurrentStoreResolutionStatus.NotFound)
            {
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status404NotFound,
                    "Storefront store was not found.");
                return;
            }

            await WriteErrorAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                resolution.Message);
        }

        private static bool ShouldSkip(PathString pathString)
        {
            var path = pathString.Value;
            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, "/alive", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ExcludedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return Path.HasExtension(path)
                && !string.Equals(path, StorefrontRoutes.Robots, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(path, StorefrontRoutes.Sitemap, StringComparison.OrdinalIgnoreCase);
        }

        private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";

            if (statusCode == StatusCodes.Status404NotFound)
            {
                StorefrontResponseHeaders.ApplyNotFound(context);
            }
            else
            {
                StorefrontResponseHeaders.ApplyServiceUnavailable(context);
            }

            return context.Response.WriteAsync(message, context.RequestAborted);
        }
    }
}
