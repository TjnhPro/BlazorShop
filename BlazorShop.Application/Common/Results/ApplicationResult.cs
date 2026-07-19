namespace BlazorShop.Application.Common.Results
{
    public sealed record ApplicationResult<TValue>
    {
        private ApplicationResult(
            bool success,
            TValue? value,
            ApplicationError? error,
            string? message)
        {
            if (success && error is not null)
            {
                throw new ArgumentException("Successful application results cannot carry an error.", nameof(error));
            }

            if (!success && error is null)
            {
                throw new ArgumentException("Failed application results must carry an error.", nameof(error));
            }

            Success = success;
            Value = value;
            Error = error;
            Message = string.IsNullOrWhiteSpace(message)
                ? error?.Message
                : message.Trim();
        }

        public bool Success { get; }

        public TValue? Value { get; }

        public ApplicationError? Error { get; }

        public string? Message { get; }

        public static ApplicationResult<TValue> Succeeded(TValue value, string? message = null)
        {
            return new ApplicationResult<TValue>(
                success: true,
                value,
                error: null,
                message);
        }

        public static ApplicationResult<TValue> Failed(ApplicationError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new ApplicationResult<TValue>(
                success: false,
                value: default,
                error,
                error.Message);
        }
    }
}
