namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    public sealed record StorefrontRuntimeError(
        int Status,
        string Code,
        string Message,
        string? TraceId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> FieldErrors);

    public static class StorefrontRuntimeErrorMapper
    {
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
                "The storefront service request could not be completed.",
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
