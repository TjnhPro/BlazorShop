namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models.Discovery;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;
    using BlazorShop.Web.SharedV2.Models.Seo;

    using Microsoft.Extensions.Options;

    using GetCategoryTreeNode = BlazorShop.Application.DTOs.Category.GetCategoryTreeNode;

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

        public StorefrontApiClient(HttpClient httpClient, IOptions<StorefrontApiOptions> options)
        {
            _httpClient = httpClient;
            _enableLegacyFallback = options.Value.EnableLegacyFallback;
        }

    }
}
