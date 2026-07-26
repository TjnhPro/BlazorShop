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
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
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
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
                "Unable to save consent right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontConsentResponse>> RevokeAsync(
            string? visitorKey,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<StorefrontConsentResponseCommerceNodeApiResponse, StorefrontConsentResponse>(
                storeKey => this.consentClient.RevokeAsync(NormalizeVisitorKey(visitorKey), storeKey, cancellationToken),
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
                "Unable to revoke consent right now.",
                cancellationToken);
        }

        private async Task<StorefrontRuntimeSubmitResult<TData>> ExecuteAsync<TEnvelope, TData>(
            Func<string, Task<TEnvelope>> execute,
            Func<TEnvelope, bool?> successSelector,
            Func<TEnvelope, TData?> dataSelector,
            Func<TEnvelope, string?> messageSelector,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            return await StorefrontRuntimeEnvelopeExecutor.ExecuteSubmitAsync(
                this.context,
                (storeKey, _) => execute(storeKey),
                successSelector,
                dataSelector,
                messageSelector,
                fallbackMessage,
                cancellationToken).ConfigureAwait(false);
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
