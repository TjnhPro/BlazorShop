namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    using GeneratedPaymentsClient = BlazorShop.Storefront.Client.IStorefrontPaymentsClient;

    public interface IStorefrontRuntimePaymentFacade
    {
        Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontPaymentMethodResponse>>> ListMethodsAsync(
            CancellationToken cancellationToken = default);

        Task<StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>> GetAttemptAsync(
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default);
    }

    public sealed class StorefrontRuntimePaymentFacade : IStorefrontRuntimePaymentFacade
    {
        private readonly IStorefrontRuntimeContext context;
        private readonly GeneratedPaymentsClient paymentsClient;

        public StorefrontRuntimePaymentFacade(
            IStorefrontRuntimeContext context,
            GeneratedPaymentsClient paymentsClient)
        {
            this.context = context;
            this.paymentsClient = paymentsClient;
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontPaymentMethodResponse>>> ListMethodsAsync(
            CancellationToken cancellationToken = default)
        {
            return ExecuteListAsync<StorefrontPaymentMethodResponseIReadOnlyListCommerceNodeApiResponse, StorefrontPaymentMethodResponse>(
                storeKey => this.paymentsClient.ListMethodsAsync(storeKey, cancellationToken),
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
                "Unable to load payment methods right now.",
                cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>> GetAttemptAsync(
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default)
        {
            if (paymentAttemptId == Guid.Empty)
            {
                return Task.FromResult(StorefrontRuntimeResult<StorefrontPaymentAttemptResponse>.Failed(NotFound()));
            }

            return ExecuteAsync<StorefrontPaymentAttemptResponseCommerceNodeApiResponse, StorefrontPaymentAttemptResponse>(
                storeKey => this.paymentsClient.GetAttemptAsync(paymentAttemptId, storeKey, cancellationToken),
                envelope => envelope.Success,
                envelope => envelope.Data,
                envelope => envelope.Message,
                "Unable to load payment attempt right now.",
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

        private static StorefrontRuntimeError NotFound()
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.NotFound,
                "http.404",
                "The requested storefront resource was not found.",
                null,
                EmptyFieldErrors());
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
