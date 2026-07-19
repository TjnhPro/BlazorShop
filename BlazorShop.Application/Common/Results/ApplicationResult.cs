namespace BlazorShop.Application.Common.Results
{
    public sealed record ApplicationResult<TValue>
    {
        public ApplicationResult(
            bool Success,
            string? Message = null,
            TValue? Payload = default,
            ApplicationErrorKind? Failure = null,
            bool AlreadyExists = false)
            : this(
                Success,
                Payload,
                ToError(Success, Failure, AlreadyExists, Message),
                Message,
                AlreadyExists)
        {
        }

        private ApplicationResult(
            bool success,
            TValue? value,
            ApplicationError? error,
            string? message,
            bool alreadyExists = false)
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
            AlreadyExists = alreadyExists || string.Equals(error?.Code, "task.already_exists", StringComparison.Ordinal);
            Message = string.IsNullOrWhiteSpace(message)
                ? error?.Message
                : message.Trim();
        }

        public bool Success { get; }

        public TValue? Value { get; }

        public TValue? Payload => Value;

        public ApplicationError? Error { get; }

        public ApplicationErrorKind? Failure => Error?.Kind;

        public bool AlreadyExists { get; }

        public string? Message { get; }

        public static ApplicationResult<TValue> Succeeded(TValue value, string? message = null)
        {
            return new ApplicationResult<TValue>(
                success: true,
                value,
                error: null,
                message);
        }

        public static ApplicationResult<TValue> Failed(ApplicationError error, TValue? value = default)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new ApplicationResult<TValue>(
                success: false,
                value,
                error,
                error.Message);
        }

        private static ApplicationError? ToError(
            bool success,
            ApplicationErrorKind? failure,
            bool alreadyExists,
            string? message)
        {
            if (success)
            {
                return null;
            }

            var safeMessage = string.IsNullOrWhiteSpace(message)
                ? "The application request could not be completed."
                : message;

            if (alreadyExists)
            {
                return ApplicationError.Conflict("task.already_exists", safeMessage);
            }

            return failure switch
            {
                ApplicationErrorKind.Validation => ApplicationError.Validation("application.validation", safeMessage),
                ApplicationErrorKind.NotFound => ApplicationError.NotFound("application.not_found", safeMessage),
                ApplicationErrorKind.Conflict => ApplicationError.Conflict("application.conflict", safeMessage),
                ApplicationErrorKind.Unauthorized => ApplicationError.Unauthorized("application.unauthorized", safeMessage),
                ApplicationErrorKind.Forbidden => ApplicationError.Forbidden("application.forbidden", safeMessage),
                ApplicationErrorKind.RemoteFailure => ApplicationError.RemoteFailure("application.remote_failure", safeMessage),
                _ => ApplicationError.Failure("application.failure", safeMessage),
            };
        }
    }
}
