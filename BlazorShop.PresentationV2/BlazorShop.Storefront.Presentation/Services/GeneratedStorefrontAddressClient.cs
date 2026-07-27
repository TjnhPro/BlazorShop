namespace BlazorShop.Storefront.Services
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Services.Contracts;

    using GeneratedClients = BlazorShop.Storefront.Client;

    public sealed class GeneratedStorefrontAddressClient : IStorefrontAddressClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IStorefrontRuntimeAddressFacade addressFacade;

        public GeneratedStorefrontAddressClient(IStorefrontRuntimeAddressFacade addressFacade)
        {
            this.addressFacade = addressFacade;
        }

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>> GetAddressCountriesAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await this.addressFacade.ListCountriesAsync(cancellationToken);
            if (result.Success && result.Value is not null)
            {
                return StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.Success(
                    Project<IReadOnlyList<StorefrontAddressCountryResponse>>(result.Value));
            }

            return result.Error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.Success([])
                : StorefrontApiResult<IReadOnlyList<StorefrontAddressCountryResponse>>.ServiceUnavailable();
        }

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>> GetAddressStatesAsync(
            string? countryCode,
            CancellationToken cancellationToken = default)
        {
            var result = await this.addressFacade.ListStatesAsync(countryCode, cancellationToken);
            if (result.Success && result.Value is not null)
            {
                return StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success(
                    Project<IReadOnlyList<StorefrontAddressStateProvinceResponse>>(result.Value));
            }

            return result.Error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Success([])
                : StorefrontApiResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.ServiceUnavailable();
        }

        public async Task<StorefrontApiResult<StorefrontAddressFieldConfigurationResponse>> GetAddressConfigurationAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await this.addressFacade.GetConfigurationAsync(cancellationToken);
            if (result.Success && result.Value is not null)
            {
                return StorefrontApiResult<StorefrontAddressFieldConfigurationResponse>.Success(
                    Project<StorefrontAddressFieldConfigurationResponse>(result.Value));
            }

            return result.Error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<StorefrontAddressFieldConfigurationResponse>.NotFound()
                : StorefrontApiResult<StorefrontAddressFieldConfigurationResponse>.ServiceUnavailable();
        }

        private static TTarget Project<TTarget>(object source)
        {
            return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
                ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
        }
    }
}
