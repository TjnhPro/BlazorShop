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
        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>> GetAddressCountriesAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<StorefrontAddressCountryResponse>>(
                StorefrontAddressCountriesRoute,
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.Success([]);
        }
        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>> GetAddressStatesAsync(
            string? countryCode,
            CancellationToken cancellationToken = default)
        {
            var normalizedCountryCode = NormalizeCountryCode(countryCode);
            if (normalizedCountryCode is null)
            {
                return StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success([]);
            }

            var result = await GetAsync<List<StorefrontAddressStateProvinceResponse>>(
                $"{StorefrontAddressCountriesRoute}/{Uri.EscapeDataString(normalizedCountryCode)}/states",
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success([]);
        }
        public Task<StorefrontApiResult<StorefrontAddressFieldConfigurationResponse>> GetAddressConfigurationAsync(
            CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundAsync<StorefrontAddressFieldConfigurationResponse>(
                StorefrontAddressConfigurationRoute,
                cancellationToken,
                CatalogRequestTimeout);
        }
    }
}
