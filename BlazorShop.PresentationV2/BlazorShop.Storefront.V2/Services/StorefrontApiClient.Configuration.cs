namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


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
