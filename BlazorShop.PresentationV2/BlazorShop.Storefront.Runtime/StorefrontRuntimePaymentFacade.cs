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
                "Unable to load payment attempt right now.",
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
