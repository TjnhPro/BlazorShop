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
            var (storeKey, host) = this.ResolveRequestStoreHints();

            return this.resolver.ResolveAsync(storeKey, host, cancellationToken);
        }

        public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(
            CancellationToken cancellationToken = default)
        {
            var (storeKey, host) = this.ResolveRequestStoreHints();

            return this.resolver.ResolveStoreIdAsync(storeKey, host, cancellationToken);
        }

        private (string? StoreKey, string? Host) ResolveRequestStoreHints()
        {
            var request = this.httpContextAccessor.HttpContext?.Request;
            var storeKey = FirstHeaderValue(request, "X-Store-Key");
            var host = FirstHeaderValue(request, "X-Store-Host")
                       ?? FirstHeaderValue(request, "X-Forwarded-Host")
                       ?? request?.Host.Value;

            return (storeKey, host);
        }

        private static string? FirstHeaderValue(HttpRequest? request, string headerName)
        {
            if (request is null || !request.Headers.TryGetValue(headerName, out var values))
            {
                return null;
            }

            return values.FirstOrDefault();
        }
    }
}
