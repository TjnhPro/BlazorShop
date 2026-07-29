using System.Net;

using BlazorShop.Storefront.Components.Browser;

namespace BlazorShop.Storefront.Browser;

public sealed record StorefrontLocalApiResult<T>(
    bool Success,
    HttpStatusCode StatusCode,
    T? Data,
    string Message,
    StorefrontLocalApiError? Error = null)
{
    public static StorefrontLocalApiResult<T> Succeeded(HttpStatusCode statusCode, T? data)
    {
        return new StorefrontLocalApiResult<T>(true, statusCode, data, string.Empty);
    }

    public static StorefrontLocalApiResult<T> Failed(HttpStatusCode statusCode, string message)
    {
        var error = StorefrontLocalApiError.Create(statusCode, response: null) with
        {
            Message = string.IsNullOrWhiteSpace(message) ? null : message,
        };
        return Failed(error);
    }

    public static StorefrontLocalApiResult<T> Failed(StorefrontLocalApiError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new StorefrontLocalApiResult<T>(false, error.StatusCode, default, error.DisplayMessage, error);
    }
}

public sealed record StorefrontLocalApiError(
    HttpStatusCode StatusCode,
    string Code,
    string? TraceId,
    IReadOnlyDictionary<string, string[]> FieldErrors,
    bool Retryable,
    string DefaultMessage,
    string? Message = null)
{
    public string DisplayMessage => string.IsNullOrWhiteSpace(Message) ? DefaultMessage : Message;

    public static StorefrontLocalApiError Create(HttpStatusCode statusCode, StorefrontLocalApiErrorResponse? response)
    {
        var statusCodeValue = response?.StatusCode is > 0
            ? (HttpStatusCode)response.StatusCode.Value
            : statusCode;

        var code = string.IsNullOrWhiteSpace(response?.Code)
            ? DefaultCode(statusCodeValue)
            : response.Code.Trim();
        var defaultMessage = DefaultMessageFor(statusCodeValue);
        var fieldErrors = response?.FieldErrors is { Count: > 0 }
            ? response.FieldErrors
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is { Length: > 0 })
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        return new StorefrontLocalApiError(
            statusCodeValue,
            code,
            string.IsNullOrWhiteSpace(response?.TraceId) ? null : response.TraceId,
            fieldErrors,
            response?.Retryable ?? IsRetryable(statusCodeValue),
            defaultMessage,
            string.IsNullOrWhiteSpace(response?.Message) ? null : response.Message);
    }

    public static StorefrontLocalApiError Semantic(
        HttpStatusCode statusCode,
        string code,
        string defaultMessage,
        bool retryable)
    {
        return new StorefrontLocalApiError(
            statusCode,
            code,
            TraceId: null,
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase),
            retryable,
            defaultMessage);
    }

    private static string DefaultCode(HttpStatusCode statusCode)
    {
        var value = (int)statusCode;
        return value switch
        {
            400 => "validation_error",
            401 => "authentication_required",
            403 => "forbidden",
            404 => "not_found",
            408 => "timeout",
            409 => "conflict",
            422 => "unprocessable",
            429 => "rate_limited",
            >= 500 => "service_unavailable",
            _ => "http_" + value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    private static string DefaultMessageFor(HttpStatusCode statusCode)
    {
        return (int)statusCode switch
        {
            400 => "The request is invalid.",
            401 => "Sign in is required.",
            403 => "This action is not allowed.",
            404 => "The requested resource was not found.",
            408 => "The request timed out. Try again.",
            409 => "The request conflicted with the latest storefront state.",
            422 => "The request could not be processed.",
            429 => "Too many requests. Try again shortly.",
            >= 500 => "The storefront service is unavailable. Try again shortly.",
            _ => "The storefront request could not be completed.",
        };
    }

    private static bool IsRetryable(HttpStatusCode statusCode)
    {
        var value = (int)statusCode;
        return value is 408 or 429 || value >= 500;
    }
}
