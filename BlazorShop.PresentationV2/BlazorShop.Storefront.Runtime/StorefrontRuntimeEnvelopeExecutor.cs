namespace BlazorShop.Storefront.Runtime
{
    internal static class StorefrontRuntimeEnvelopeExecutor
    {
        public static async Task<StorefrontRuntimeResult<TData>> ExecuteResultAsync<TEnvelope, TData>(
            IStorefrontRuntimeContext context,
            Func<string, CancellationToken, Task<TEnvelope>> execute,
            Func<TEnvelope, bool?> successSelector,
            Func<TEnvelope, TData?> dataSelector,
            Func<TEnvelope, string?> messageSelector,
            string fallbackMessage,
            CancellationToken cancellationToken,
            string? idempotencyKey = null)
        {
            try
            {
                var response = await execute(context.RequireStoreKey(), cancellationToken).ConfigureAwait(false);
                if (response is null)
                {
                    return StorefrontRuntimeResult<TData>.Failed(ServiceUnavailable(fallbackMessage));
                }

                return successSelector(response) == true && dataSelector(response) is { } data
                    ? StorefrontRuntimeResult<TData>.Succeeded(data)
                    : StorefrontRuntimeResult<TData>.Failed(ServiceUnavailable(messageSelector(response) ?? fallbackMessage));
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

        public static async Task<StorefrontRuntimeResult<IReadOnlyList<TData>>> ExecuteListResultAsync<TEnvelope, TData>(
            IStorefrontRuntimeContext context,
            Func<string, CancellationToken, Task<TEnvelope>> execute,
            Func<TEnvelope, bool?> successSelector,
            Func<TEnvelope, IEnumerable<TData>?> dataSelector,
            Func<TEnvelope, string?> messageSelector,
            string fallbackMessage,
            CancellationToken cancellationToken)
        {
            var result = await ExecuteResultAsync<TEnvelope, IEnumerable<TData>>(
                context,
                execute,
                successSelector,
                dataSelector,
                messageSelector,
                fallbackMessage,
                cancellationToken).ConfigureAwait(false);

            return result.Success && result.Value is not null
                ? StorefrontRuntimeResult<IReadOnlyList<TData>>.Succeeded(result.Value.ToArray())
                : StorefrontRuntimeResult<IReadOnlyList<TData>>.Failed(result.Error ?? ServiceUnavailable(fallbackMessage));
        }

        public static async Task<StorefrontRuntimeSubmitResult<TData>> ExecuteSubmitAsync<TEnvelope, TData>(
            IStorefrontRuntimeContext context,
            Func<string, CancellationToken, Task<TEnvelope>> execute,
            Func<TEnvelope, bool?> successSelector,
            Func<TEnvelope, TData?> dataSelector,
            Func<TEnvelope, string?> messageSelector,
            string fallbackMessage,
            CancellationToken cancellationToken,
            string? idempotencyKey = null)
        {
            try
            {
                var response = await execute(context.RequireStoreKey(), cancellationToken).ConfigureAwait(false);
                if (response is null)
                {
                    return StorefrontRuntimeSubmitResult<TData>.Failed(ServiceUnavailable(fallbackMessage), idempotencyKey);
                }

                return successSelector(response) == true && dataSelector(response) is { } data
                    ? StorefrontRuntimeSubmitResult<TData>.Succeeded(data, idempotencyKey)
                    : StorefrontRuntimeSubmitResult<TData>.Failed(ServiceUnavailable(messageSelector(response) ?? fallbackMessage), idempotencyKey);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return StorefrontRuntimeSubmitResult<TData>.Failed(StorefrontRuntimeErrorMapper.FromException(exception), idempotencyKey);
            }
        }

        private static StorefrontRuntimeError ServiceUnavailable(string message)
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.ServiceUnavailable,
                "storefront.unavailable",
                message,
                null,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));
        }
    }
}
