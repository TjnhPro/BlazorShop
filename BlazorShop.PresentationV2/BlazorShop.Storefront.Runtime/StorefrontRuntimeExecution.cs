namespace BlazorShop.Storefront.Runtime
{
    public static class StorefrontRuntimeExecution
    {
        public static string RequireStoreKey(this IStorefrontRuntimeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return RequireStoreKey(context.StoreKey);
        }

        public static string RequireStoreKey(string storeKey)
        {
            if (string.IsNullOrWhiteSpace(storeKey))
            {
                throw new InvalidOperationException("A Storefront runtime call requires an explicit storeKey.");
            }

            return storeKey.Trim();
        }

        public static async Task<StorefrontRuntimeResult<T>> ExecuteAsync<T>(
            this IStorefrontRuntimeContext context,
            Func<string, CancellationToken, Task<T>> call,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(call);

            var storeKey = context.RequireStoreKey();

            try
            {
                var value = await call(storeKey, cancellationToken).ConfigureAwait(false);
                return StorefrontRuntimeResult<T>.Succeeded(value);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || exception is TaskCanceledException)
            {
                return StorefrontRuntimeResult<T>.Failed(StorefrontRuntimeErrorMapper.FromException(exception));
            }
        }

        public static async Task<StorefrontRuntimeSubmitResult<T>> ExecuteSubmitAsync<T>(
            this IStorefrontRuntimeContext context,
            Func<string, CancellationToken, Task<T>> call,
            string? idempotencyKey = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(call);

            var storeKey = context.RequireStoreKey();

            try
            {
                var value = await call(storeKey, cancellationToken).ConfigureAwait(false);
                return StorefrontRuntimeSubmitResult<T>.Succeeded(value, idempotencyKey);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || exception is TaskCanceledException)
            {
                return StorefrontRuntimeSubmitResult<T>.Failed(StorefrontRuntimeErrorMapper.FromException(exception), idempotencyKey);
            }
        }
    }
}
