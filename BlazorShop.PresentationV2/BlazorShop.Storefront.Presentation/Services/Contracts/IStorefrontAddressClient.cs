namespace BlazorShop.Storefront.Presentation.Contracts
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Presentation.Options;

    using Microsoft.Extensions.Options;

    using BlazorShop.Storefront.Presentation.Services;

    public interface IStorefrontAddressClient
    {
        Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>> GetAddressCountriesAsync(
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>> GetAddressStatesAsync(
                    string? countryCode,
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontAddressFieldConfigurationResponse>> GetAddressConfigurationAsync(
                    CancellationToken cancellationToken = default);
    }
}
