namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontCurrentStoreProvider : IStorefrontCurrentStoreProvider
    {
        private static readonly object ContextItemKey = new();

        private readonly StorefrontApiClient _apiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<StorefrontCurrentStoreProvider> _logger;

        public StorefrontCurrentStoreProvider(
            StorefrontApiClient apiClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<StorefrontCurrentStoreProvider> logger)
        {
            _apiClient = apiClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<StorefrontCurrentStoreResolution> ResolveAsync(CancellationToken cancellationToken = default)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(ContextItemKey, out var cached) == true
                && cached is StorefrontCurrentStoreResolution cachedResolution)
            {
                return cachedResolution;
            }

            var apiResult = await _apiClient.GetCurrentStoreAsync(cancellationToken);
            var resolution = MapResult(apiResult);

            if (httpContext is not null)
            {
                httpContext.Items[ContextItemKey] = resolution;
            }

            if (resolution.Status != StorefrontCurrentStoreResolutionStatus.Success)
            {
                _logger.LogWarning(
                    "Storefront current store resolution returned {Status}. Message: {Message}",
                    resolution.Status,
                    resolution.Message);
            }

            return resolution;
        }

        private static StorefrontCurrentStoreResolution MapResult(StorefrontApiResult<StorefrontCurrentStore> apiResult)
        {
            if (apiResult.IsSuccess && apiResult.Value is { } store)
            {
                return store.MaintenanceModeEnabled
                    ? StorefrontCurrentStoreResolution.Maintenance(store)
                    : StorefrontCurrentStoreResolution.Succeeded(store);
            }

            if (apiResult.IsNotFound)
            {
                return StorefrontCurrentStoreResolution.NotFound();
            }

            return StorefrontCurrentStoreResolution.ServiceUnavailable();
        }
    }
}
