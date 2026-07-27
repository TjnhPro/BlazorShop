namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Services.Contracts;


    public partial class StorefrontApiClient :
        IStorefrontAddressClient,
        IStorefrontCartClient,
        IStorefrontCatalogClient,
        IStorefrontCheckoutClient,
        IStorefrontConsentClient,
        IStorefrontContentClient,
        IStorefrontCustomerClient,
        IStorefrontPaymentClient,
        IStorefrontStoreConfigurationClient
    {
        // Static informational pages should degrade faster than catalog-backed pages when the API is offline.
        private static readonly TimeSpan CatalogRequestTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan RedirectResolutionRequestTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan SeoSettingsRequestTimeout = TimeSpan.FromMilliseconds(500);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly bool _enableLegacyFallback;

        public StorefrontApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _enableLegacyFallback = false;
        }

    }
}
