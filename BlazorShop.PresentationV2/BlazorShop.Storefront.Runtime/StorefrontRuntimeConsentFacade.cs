namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    using GeneratedConsentClient = BlazorShop.Storefront.Client.IStorefrontConsentClient;

    public interface IStorefrontRuntimeConsentFacade
    {
        Task<StorefrontRuntimeSubmitResult<StorefrontConsentResponse>> GetCurrentAsync(
            string? visitorKey,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontConsentResponse>> SaveAsync(
            string? visitorKey,
            StorefrontConsentSaveRequest request,
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeSubmitResult<StorefrontConsentResponse>> RevokeAsync(
            string? visitorKey,
            CancellationToken cancellationToken = default);
    }

    public sealed class StorefrontRuntimeConsentFacade : IStorefrontRuntimeConsentFacade
    {
        private readonly IStorefrontRuntimeContext context;
        private readonly GeneratedConsentClient consentClient;

        public StorefrontRuntimeConsentFacade(
            IStorefrontRuntimeContext context,
            GeneratedConsentClient consentClient)
        {
            this.context = context;
            this.consentClient = consentClient;
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontConsentResponse>> GetCurrentAsync(
            string? visitorKey,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontConsentResponseCommerceNodeApiResponse, StorefrontConsentResponse>(
                storeKey => this.consentClient.CurrentAsync(NormalizeVisitorKey(visitorKey), storeKey, cancellationToken),
                "Unable to load consent state right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontConsentResponse>> SaveAsync(
            string? visitorKey,
            StorefrontConsentSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontConsentResponseCommerceNodeApiResponse, StorefrontConsentResponse>(
                storeKey => this.consentClient.SaveAsync(NormalizeVisitorKey(visitorKey), storeKey, request, cancellationToken),
                "Unable to save consent right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontConsentResponse>> RevokeAsync(
            string? visitorKey,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontConsentResponseCommerceNodeApiResponse, StorefrontConsentResponse>(
                storeKey => this.consentClient.RevokeAsync(NormalizeVisitorKey(visitorKey), storeKey, cancellationToken),
                "Unable to revoke consent right now.",
                cancellationToken);
        }

        private async Task<StorefrontRuntimeSubmitResult<TData>> ExecuteAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await execute(this.context.RequireStoreKey()).ConfigureAwait(false);
                if (response is null)
                {
                    return StorefrontRuntimeSubmitResult<TData>.Failed(ServiceUnavailable(fallbackMessage));
                }

                var success = response.GetType().GetProperty("Success")?.GetValue(response) as bool?;
                var data = response.GetType().GetProperty("Data")?.GetValue(response);
                var message = response.GetType().GetProperty("Message")?.GetValue(response) as string;
                return success == true && data is TData typedData
                    ? StorefrontRuntimeSubmitResult<TData>.Succeeded(typedData)
                    : StorefrontRuntimeSubmitResult<TData>.Failed(ServiceUnavailable(message ?? fallbackMessage));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeSubmitResult<TData>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        private static string? NormalizeVisitorKey(string? visitorKey)
        {
            return string.IsNullOrWhiteSpace(visitorKey) ? null : visitorKey.Trim();
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
