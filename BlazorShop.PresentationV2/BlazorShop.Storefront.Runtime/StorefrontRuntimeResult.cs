namespace BlazorShop.Storefront.Runtime
{
    public sealed record StorefrontRuntimeResult<T>
    {
        private StorefrontRuntimeResult(bool success, T? value, StorefrontRuntimeError? error)
        {
            this.Success = success;
            this.Value = value;
            this.Error = error;
        }

        public bool Success { get; }

        public T? Value { get; }

        public StorefrontRuntimeError? Error { get; }

        public static StorefrontRuntimeResult<T> Succeeded(T value)
        {
            return new StorefrontRuntimeResult<T>(true, value, null);
        }

        public static StorefrontRuntimeResult<T> Failed(StorefrontRuntimeError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new StorefrontRuntimeResult<T>(false, default, error);
        }
    }

    public sealed record StorefrontRuntimeSubmitResult<T>
    {
        private StorefrontRuntimeSubmitResult(bool success, T? value, StorefrontRuntimeError? error, string? idempotencyKey)
        {
            this.Success = success;
            this.Value = value;
            this.Error = error;
            this.IdempotencyKey = idempotencyKey;
        }

        public bool Success { get; }

        public T? Value { get; }

        public StorefrontRuntimeError? Error { get; }

        public string? IdempotencyKey { get; }

        public static StorefrontRuntimeSubmitResult<T> Succeeded(T value, string? idempotencyKey = null)
        {
            return new StorefrontRuntimeSubmitResult<T>(true, value, null, idempotencyKey);
        }

        public static StorefrontRuntimeSubmitResult<T> Failed(StorefrontRuntimeError error, string? idempotencyKey = null)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new StorefrontRuntimeSubmitResult<T>(false, default, error, idempotencyKey);
        }
    }
}
