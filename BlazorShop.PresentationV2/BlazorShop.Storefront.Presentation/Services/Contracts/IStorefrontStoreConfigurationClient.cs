namespace BlazorShop.Storefront.Presentation.Contracts
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Presentation.Options;

    using Microsoft.Extensions.Options;

    using BlazorShop.Storefront.Presentation.Services;

    public interface IStorefrontStoreConfigurationClient
    {
        Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
                    StorefrontCurrencyPreferenceRequest request,
                    CancellationToken cancellationToken = default);
    }
}
