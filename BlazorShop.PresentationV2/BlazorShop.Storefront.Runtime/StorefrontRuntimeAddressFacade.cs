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
                "Unable to load address states right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontAddressFieldConfigurationResponse>> GetConfigurationAsync(
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontAddressFieldConfigurationResponseCommerceNodeApiResponse, StorefrontAddressFieldConfigurationResponse>(
                storeKey => this.addressClient.GetConfigurationAsync(storeKey, cancellationToken),
                "Unable to load address configuration right now.",
                cancellationToken);
        }

        private async Task<StorefrontRuntimeResult<TData>> ExecuteAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await execute(this.context.RequireStoreKey()).ConfigureAwait(false);
                if (response is null)
                {
                    return StorefrontRuntimeResult<TData>.Failed(ServiceUnavailable(fallbackMessage));
                }

                var success = response.GetType().GetProperty("Success")?.GetValue(response) as bool?;
                var data = response.GetType().GetProperty("Data")?.GetValue(response);
                var message = response.GetType().GetProperty("Message")?.GetValue(response) as string;
                return success == true && data is TData typedData
                    ? StorefrontRuntimeResult<TData>.Succeeded(typedData)
                    : StorefrontRuntimeResult<TData>.Failed(ServiceUnavailable(message ?? fallbackMessage));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeResult<TData>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        private async Task<StorefrontRuntimeResult<IReadOnlyList<TData>>> ExecuteListAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await execute(this.context.RequireStoreKey()).ConfigureAwait(false);
                if (response is null)
                {
                    return StorefrontRuntimeResult<IReadOnlyList<TData>>.Failed(ServiceUnavailable(fallbackMessage));
                }

                var success = response.GetType().GetProperty("Success")?.GetValue(response) as bool?;
                var data = response.GetType().GetProperty("Data")?.GetValue(response);
                var message = response.GetType().GetProperty("Message")?.GetValue(response) as string;
                return success == true && data is IEnumerable<TData> typedData
                    ? StorefrontRuntimeResult<IReadOnlyList<TData>>.Succeeded(typedData.ToArray())
                    : StorefrontRuntimeResult<IReadOnlyList<TData>>.Failed(ServiceUnavailable(message ?? fallbackMessage));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeResult<IReadOnlyList<TData>>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        private static string? NormalizeCountryCode(string? countryCode)
        {
            return string.IsNullOrWhiteSpace(countryCode)
                ? null
                : countryCode.Trim().ToUpperInvariant();
        }

        private static StorefrontRuntimeError ServiceUnavailable(string message)
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.ServiceUnavailable,
                "storefront.unavailable",
                message,
                null,
                EmptyFieldErrors());
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyFieldErrors()
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }
    }
}
