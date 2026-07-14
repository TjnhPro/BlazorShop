namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Stores;

    using Microsoft.AspNetCore.Http;

    public sealed class CommerceStoreContext : ICommerceStoreContext
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICommerceStoreDomainResolver resolver;

        public CommerceStoreContext(
            IHttpContextAccessor httpContextAccessor,
            ICommerceStoreDomainResolver resolver)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.resolver = resolver;
        }

        public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
            CancellationToken cancellationToken = default)
        {
            var hints = this.ResolveRequestStoreHints();
            if (hints.FailureMessage is not null)
            {
                return Task.FromResult(Failed<CommerceCurrentStore>(hints.FailureMessage));
            }

            return this.resolver.ResolveAsync(hints.StoreKey, hints.Host, cancellationToken);
        }

        public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(
            CancellationToken cancellationToken = default)
        {
            var hints = this.ResolveRequestStoreHints();
            if (hints.FailureMessage is not null)
            {
                return Task.FromResult(Failed<Guid>(hints.FailureMessage));
            }

            return this.resolver.ResolveStoreIdAsync(hints.StoreKey, hints.Host, cancellationToken);
        }

        private StoreScopeHints ResolveRequestStoreHints()
        {
            var request = this.httpContextAccessor.HttpContext?.Request;
            if (request is null)
            {
                return new StoreScopeHints(null, null, "Store request context is required.");
            }

            if (request.Path.StartsWithSegments("/api/storefront/stores"))
            {
                var routeStoreKey = Convert.ToString(request.RouteValues["storeKey"]);
                return string.IsNullOrWhiteSpace(routeStoreKey)
                    ? new StoreScopeHints(null, null, "storeKey route value is required.")
                    : new StoreScopeHints(routeStoreKey, null, null);
            }

            if (request.Path.StartsWithSegments("/api/commerce/admin"))
            {
                var queryStoreKey = FirstQueryValue(request, "storeKey");
                return string.IsNullOrWhiteSpace(queryStoreKey)
                    ? new StoreScopeHints(null, null, "storeKey query parameter is required.")
                    : new StoreScopeHints(queryStoreKey, null, null);
            }

            var storeKey = FirstHeaderValue(request, "X-Store-Key");
            var host = FirstHeaderValue(request, "X-Store-Host")
                       ?? FirstHeaderValue(request, "X-Forwarded-Host")
                       ?? request.Host.Value;

            return new StoreScopeHints(storeKey, host, null);
        }

        private static string? FirstHeaderValue(HttpRequest? request, string headerName)
        {
            if (request is null || !request.Headers.TryGetValue(headerName, out var values))
            {
                return null;
            }

            return values.FirstOrDefault();
        }

        private static string? FirstQueryValue(HttpRequest request, string queryName)
        {
            return request.Query.TryGetValue(queryName, out var values)
                ? values.FirstOrDefault()
                : null;
        }

        private static CommerceStoreOperationResult<TPayload> Failed<TPayload>(string message)
        {
            return new CommerceStoreOperationResult<TPayload>(
                false,
                message,
                Failure: CommerceStoreOperationFailure.Validation);
        }

        private sealed record StoreScopeHints(string? StoreKey, string? Host, string? FailureMessage);
    }
}
