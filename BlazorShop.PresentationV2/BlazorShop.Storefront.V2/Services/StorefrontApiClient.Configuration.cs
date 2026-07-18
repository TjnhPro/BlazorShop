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
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;
    using BlazorShop.Web.SharedV2.Models.Seo;

    using Microsoft.Extensions.Options;

    using GetCategoryTreeNode = BlazorShop.Application.DTOs.Category.GetCategoryTreeNode;

    public partial class StorefrontApiClient
    {
        public Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<StorefrontCurrentStore>(
                StorefrontStoreCurrentRoute,
                cancellationToken,
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<StorefrontPublicConfiguration>(
                StorefrontConfigurationRoute,
                cancellationToken,
                CatalogRequestTimeout);
        }
        public Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
            StorefrontCurrencyPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            return PostAsync<StorefrontCurrencyPreferenceRequest, StorefrontCurrencyPreferenceResponse>(
                StorefrontCurrencyPreferenceRoute,
                request,
                "Unable to update currency preference right now.",
                cancellationToken);
        }
    }
}
