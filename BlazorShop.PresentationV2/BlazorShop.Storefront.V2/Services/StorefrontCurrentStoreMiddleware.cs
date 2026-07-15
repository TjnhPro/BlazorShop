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
                await WriteUnavailableAsync(
                    context,
                    resolution,
                    StatusCodes.Status404NotFound,
                    "Storefront store was not found.");
                return;
            }

            await WriteUnavailableAsync(
                context,
                resolution,
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
                || string.Equals(path, "/alive", StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, StorefrontRoutes.Maintenance, StringComparison.OrdinalIgnoreCase))
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

        private static Task WriteUnavailableAsync(
            HttpContext context,
            StorefrontCurrentStoreResolution resolution,
            int statusCode,
            string message)
        {
            if (IsHtmlGet(context.Request) && !IsDiscoveryDocument(context.Request.Path))
            {
                StorefrontResponseHeaders.ApplyPrivatePage(context);
                context.Response.Redirect(
                    $"{StorefrontRoutes.Maintenance}?reason={Uri.EscapeDataString(ToMaintenanceReason(resolution.Status))}",
                    permanent: false);
                return Task.CompletedTask;
            }

            return WriteErrorAsync(context, statusCode, message);
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

        private static bool IsHtmlGet(HttpRequest request)
        {
            if (!HttpMethods.IsGet(request.Method))
            {
                return false;
            }

            var accept = request.Headers.Accept.ToString();
            return string.IsNullOrWhiteSpace(accept)
                || accept.Contains("text/html", StringComparison.OrdinalIgnoreCase)
                || accept.Contains("*/*", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToMaintenanceReason(StorefrontCurrentStoreResolutionStatus status)
        {
            return status switch
            {
                StorefrontCurrentStoreResolutionStatus.Maintenance => "maintenance",
                StorefrontCurrentStoreResolutionStatus.Closed => "closed",
                StorefrontCurrentStoreResolutionStatus.NotReady => "not-ready",
                StorefrontCurrentStoreResolutionStatus.NotFound => "not-found",
                _ => "unavailable",
            };
        }

        private static bool IsDiscoveryDocument(PathString pathString)
        {
            var path = pathString.Value;
            return string.Equals(path, StorefrontRoutes.Robots, StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, StorefrontRoutes.Sitemap, StringComparison.OrdinalIgnoreCase);
        }
    }
}
