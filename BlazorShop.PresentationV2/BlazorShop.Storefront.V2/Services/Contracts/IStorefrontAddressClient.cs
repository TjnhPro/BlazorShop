namespace BlazorShop.Storefront.Services.Contracts
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

    using BlazorShop.Storefront.Services;

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
