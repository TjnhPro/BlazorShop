namespace BlazorShop.Storefront.Configuration
{
    using System.Threading.RateLimiting;

    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services;

    using Microsoft.AspNetCore.RateLimiting;

    public static class StorefrontRateLimitPolicies
    {
        public const string LocalCartPolicyName = "storefront-local-cart";

        public static void ConfigureStorefrontRateLimiter(
            RateLimiterOptions options,
            StorefrontRateLimitingOptions rateLimitingOptions)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(rateLimitingOptions);

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                await httpContext.Response.WriteAsJsonAsync(
                    new StorefrontLocalCartErrorResponse("Too many cart requests. Try again shortly."),
                    cancellationToken);
            };

            options.AddPolicy(
                LocalCartPolicyName,
                httpContext => CreateStorefrontRateLimitPartition(httpContext, rateLimitingOptions.Cart));
        }

        private static RateLimitPartition<string> CreateStorefrontRateLimitPartition(
            HttpContext httpContext,
            StorefrontRateLimitPolicyOptions policyOptions)
        {
            var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var storeKey = StorefrontStoreKeyResolver.Resolve(configuration) ?? "unknown-store";
            var route = httpContext.GetEndpoint()?.DisplayName
                ?? httpContext.Request.Path.Value
                ?? "unknown-route";
            var actor = StorefrontRateLimitIdentity.ResolveLocalCartActor(httpContext);
            var partitionKey = string.Join('|', storeKey, route, actor);

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
    }

    public sealed record StorefrontLocalCartErrorResponse(string Message);
}
