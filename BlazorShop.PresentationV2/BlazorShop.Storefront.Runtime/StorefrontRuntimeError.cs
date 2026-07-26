namespace BlazorShop.Storefront.Runtime
{
    using System.Net.Http;

    using BlazorShop.Storefront.Client;

    public sealed record StorefrontRuntimeError(
        int Status,
        string Code,
        string Message,
        string? TraceId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors)
    {
        public string DefaultMessage => this.Message;

        public bool Retryable =>
            string.Equals(this.Code, "network.timeout", StringComparison.Ordinal)
            || string.Equals(this.Code, "network.failure", StringComparison.Ordinal);

        public IReadOnlyList<StorefrontRuntimeValidationError> ValidationErrors =>
            this.FieldErrors
                .Select(pair => new StorefrontRuntimeValidationError(pair.Key, pair.Value))
                .ToArray();

        public StorefrontRuntimeConflict? Conflict =>
            this.Status == StorefrontRuntimeStatusCodes.Conflict
                ? new StorefrontRuntimeConflict(this.Code, this.Message, this.TraceId)
                : null;
    }

    public sealed record StorefrontRuntimeValidationError(
        string Field,
        IReadOnlyList<string> Messages);

    public sealed record StorefrontRuntimeConflict(
        string Code,
        string Message,
        string? TraceId);

    public static class StorefrontRuntimeStatusCodes
    {
        public const int Unauthorized = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int Conflict = 409;
        public const int Validation = 422;
        public const int ServiceUnavailable = 503;
    }

    public static class StorefrontRuntimeErrorMapper
    {
        public static StorefrontRuntimeError FromException(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception switch
            {
                StorefrontApiException apiException => FromApiException(apiException),
                TimeoutException => ServiceUnavailable("network.timeout", "The storefront service request timed out."),
                TaskCanceledException => ServiceUnavailable("network.timeout", "The storefront service request timed out."),
                HttpRequestException => ServiceUnavailable("network.failure", "The storefront service could not be reached."),
                _ => ServiceUnavailable("network.failure", "The storefront service request could not be completed."),
            };
        }

        public static StorefrontRuntimeError FromApiException(StorefrontApiException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (exception is StorefrontApiException<CommerceNodeApiErrorResponse> typedException
                && typedException.Result is not null)
            {
                return new StorefrontRuntimeError(
                    typedException.StatusCode,
                    NormalizeCode(typedException.Result.Code, typedException.StatusCode),
                    NormalizeMessage(typedException.Result.Message),
                    string.IsNullOrWhiteSpace(typedException.Result.TraceId) ? null : typedException.Result.TraceId,
                    NormalizeFieldErrors(typedException.Result.FieldErrors));
            }

            return new StorefrontRuntimeError(
                exception.StatusCode,
                NormalizeCode(null, exception.StatusCode),
                NormalizeStatusMessage(exception.StatusCode),
                null,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));
        }

        private static StorefrontRuntimeError ServiceUnavailable(string code, string message)
        {
            return new StorefrontRuntimeError(
                StorefrontRuntimeStatusCodes.ServiceUnavailable,
                code,
                message,
                null,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal));
        }

        private static string NormalizeCode(string? code, int status)
        {
            return string.IsNullOrWhiteSpace(code) ? $"http.{status}" : code.Trim();
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The storefront service request could not be completed."
                : message.Trim();
        }

        private static string NormalizeStatusMessage(int status)
        {
            return status switch
            {
                StorefrontRuntimeStatusCodes.Unauthorized => "Authentication is required for this storefront request.",
                StorefrontRuntimeStatusCodes.Forbidden => "This storefront request is not allowed.",
                StorefrontRuntimeStatusCodes.NotFound => "The requested storefront resource was not found.",
                StorefrontRuntimeStatusCodes.Conflict => "The storefront resource changed before the request completed.",
                StorefrontRuntimeStatusCodes.Validation => "The storefront request contains validation errors.",
                StorefrontRuntimeStatusCodes.ServiceUnavailable => "The storefront service is unavailable.",
                _ => "The storefront service request could not be completed.",
            };
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> NormalizeFieldErrors(
            IDictionary<string, ICollection<string>>? fieldErrors)
        {
            if (fieldErrors is null || fieldErrors.Count == 0)
            {
                return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            }

            return fieldErrors.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.ToArray(),
                StringComparer.Ordinal);
        }
    }
}
