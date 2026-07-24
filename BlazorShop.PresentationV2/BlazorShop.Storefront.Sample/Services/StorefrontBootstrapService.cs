namespace BlazorShop.Storefront.Sample.Services
{
    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Runtime;

    public sealed class StorefrontBootstrapService
    {
        private readonly IStorefrontRuntimeContext runtimeContext;
        private readonly IStorefrontStoreClient storeClient;
        private readonly IStorefrontConfigurationClient configurationClient;
        private readonly IStorefrontCatalogClient catalogClient;

        public StorefrontBootstrapService(
            IStorefrontRuntimeContext runtimeContext,
            IStorefrontStoreClient storeClient,
            IStorefrontConfigurationClient configurationClient,
            IStorefrontCatalogClient catalogClient)
        {
            this.runtimeContext = runtimeContext;
            this.storeClient = storeClient;
            this.configurationClient = configurationClient;
            this.catalogClient = catalogClient;
        }

        public async Task<StarterBootstrapSnapshot> LoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var storeResponse = await this.storeClient.GetCurrentAsync(this.runtimeContext.StoreKey, cancellationToken);
                var configurationResponse = await this.configurationClient.GetAsync(this.runtimeContext.StoreKey, cancellationToken);
                var productsResponse = await this.catalogClient.QueryProductsAsync(
                    pageNumber: 1,
                    pageSize: 4,
                    categoryId: null,
                    categorySlug: null,
                    includeSubcategories: null,
                    searchTerm: null,
                    minPrice: null,
                    maxPrice: null,
                    inStock: null,
                    sortBy: null,
                    createdAfterUtc: null,
                    currencyCode: configurationResponse.Data?.CurrencyOptions?.DefaultCurrencyCode,
                    storeKey: this.runtimeContext.StoreKey,
                    cancellationToken);

                return StarterBootstrapSnapshot.Ready(
                    storeName: Normalize(storeResponse.Data?.Name, "Storefront"),
                    storeKey: Normalize(storeResponse.Data?.StoreKey, this.runtimeContext.StoreKey),
                    currencyCode: Normalize(configurationResponse.Data?.CurrencyOptions?.DefaultCurrencyCode, "USD"),
                    features: MapCapabilities(configurationResponse.Data?.Features),
                    products: productsResponse.Data?.Items?
                        .Select(MapProduct)
                        .ToArray() ?? []);
            }
            catch (StorefrontApiException exception)
            {
                return StarterBootstrapSnapshot.Failed(StorefrontRuntimeErrorMapper.FromApiException(exception));
            }
        }

        private static StarterProductSummary MapProduct(StorefrontCatalogProductResponse product)
        {
            return new StarterProductSummary(
                product.Id,
                Normalize(product.Name, "Untitled product"),
                product.Slug,
                product.Price,
                product.Image,
                product.Purchasable == true);
        }

        private static IReadOnlyDictionary<string, StorefrontRuntimeCapability> MapCapabilities(
            IDictionary<string, StorefrontCapabilityResponse>? features)
        {
            if (features is null || features.Count == 0)
            {
                return new Dictionary<string, StorefrontRuntimeCapability>(StringComparer.Ordinal);
            }

            return features.ToDictionary(
                pair => pair.Key,
                pair => new StorefrontRuntimeCapability(
                    pair.Value.Supported == true,
                    pair.Value.Enabled == true,
                    pair.Value.Reason),
                StringComparer.Ordinal);
        }

        private static string Normalize(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    public sealed record StarterBootstrapSnapshot(
        bool IsReady,
        string StoreName,
        string StoreKey,
        string CurrencyCode,
        IReadOnlyDictionary<string, StorefrontRuntimeCapability> Features,
        IReadOnlyList<StarterProductSummary> Products,
        StorefrontRuntimeError? Error)
    {
        public static StarterBootstrapSnapshot Ready(
            string storeName,
            string storeKey,
            string currencyCode,
            IReadOnlyDictionary<string, StorefrontRuntimeCapability> features,
            IReadOnlyList<StarterProductSummary> products)
        {
            return new StarterBootstrapSnapshot(true, storeName, storeKey, currencyCode, features, products, null);
        }

        public static StarterBootstrapSnapshot Failed(StorefrontRuntimeError error)
        {
            return new StarterBootstrapSnapshot(
                false,
                "Storefront",
                string.Empty,
                "USD",
                new Dictionary<string, StorefrontRuntimeCapability>(StringComparer.Ordinal),
                [],
                error);
        }
    }

    public sealed record StarterProductSummary(
        Guid? Id,
        string Name,
        string? Slug,
        double? Price,
        string? ImageUrl,
        bool Purchasable);
}

