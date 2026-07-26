namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    using GeneratedAddressClient = BlazorShop.Storefront.Client.IStorefrontAddressClient;

    public interface IStorefrontRuntimeAddressFacade
    {
        Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontAddressCountryResponse>>> ListCountriesAsync(
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>> ListStatesAsync(
            string? countryCode,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontAddressFieldConfigurationResponse>> GetConfigurationAsync(
            CancellationToken cancellationToken = default);
    }

    public sealed class StorefrontRuntimeAddressFacade : IStorefrontRuntimeAddressFacade
    {
        private readonly IStorefrontRuntimeContext context;
        private readonly GeneratedAddressClient addressClient;

        public StorefrontRuntimeAddressFacade(
            IStorefrontRuntimeContext context,
            GeneratedAddressClient addressClient)
        {
            this.context = context;
            this.addressClient = addressClient;
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontAddressCountryResponse>>> ListCountriesAsync(
            CancellationToken cancellationToken = default)
        {
            return ExecuteListAsync<StorefrontAddressCountryResponseIReadOnlyListCommerceNodeApiResponse, StorefrontAddressCountryResponse>(
                storeKey => this.addressClient.ListCountriesAsync(storeKey, cancellationToken),
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
                "Unable to load address countries right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>> ListStatesAsync(
            string? countryCode,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeCountryCode(countryCode);
            if (normalized is null)
            {
                return Task.FromResult(StorefrontRuntimeResult<IReadOnlyList<StorefrontAddressStateProvinceResponse>>.Succeeded([]));
            }

            return ExecuteListAsync<StorefrontAddressStateProvinceResponseIReadOnlyListCommerceNodeApiResponse, StorefrontAddressStateProvinceResponse>(
                storeKey => this.addressClient.ListStatesAsync(normalized, storeKey, cancellationToken),
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
                "Unable to load address states right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontAddressFieldConfigurationResponse>> GetConfigurationAsync(
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontAddressFieldConfigurationResponseCommerceNodeApiResponse, StorefrontAddressFieldConfigurationResponse>(
                storeKey => this.addressClient.GetConfigurationAsync(storeKey, cancellationToken),
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
                "Unable to load address configuration right now.",
                cancellationToken);
        }

        private async Task<StorefrontRuntimeResult<TData>> ExecuteAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            Func<TEnvelope, bool?> successSelector,
            Func<TEnvelope, TData?> dataSelector,
            Func<TEnvelope, string?> messageSelector,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            return await StorefrontRuntimeEnvelopeExecutor.ExecuteResultAsync(
                this.context,
                (storeKey, _) => execute(storeKey),
                successSelector,
                dataSelector,
                messageSelector,
                fallbackMessage,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<StorefrontRuntimeResult<IReadOnlyList<TData>>> ExecuteListAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            Func<TEnvelope, bool?> successSelector,
            Func<TEnvelope, IEnumerable<TData>?> dataSelector,
            Func<TEnvelope, string?> messageSelector,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            return await StorefrontRuntimeEnvelopeExecutor.ExecuteListResultAsync(
                this.context,
                (storeKey, _) => execute(storeKey),
                successSelector,
                dataSelector,
                messageSelector,
                fallbackMessage,
                cancellationToken).ConfigureAwait(false);
        }

        private static string? NormalizeCountryCode(string? countryCode)
        {
            return string.IsNullOrWhiteSpace(countryCode)
                ? null
                : countryCode.Trim().ToUpperInvariant();
        }
    }
}
