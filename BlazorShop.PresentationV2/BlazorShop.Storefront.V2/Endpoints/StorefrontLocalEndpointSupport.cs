namespace BlazorShop.Storefront.Endpoints
{
    using System.Diagnostics;
    using System.Globalization;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using Microsoft.AspNetCore.Antiforgery;

    internal static partial class StorefrontLocalEndpointSupport
    {
        private const string StorefrontConsentVisitorCookieName = "bs-consent-visitor";

        internal static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }

        internal static bool IsValidEmail(string? email)
        {
            return !string.IsNullOrWhiteSpace(email)
                && email.Length <= 254
                && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email);
        }

        internal static string? NormalizeOptionalFormValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        internal static string FormatMoney(decimal amount, string? currencyCode)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{amount:0.00} {currencyCode ?? string.Empty}").Trim();
        }

        internal static async Task<IResult?> ValidateLocalCartAntiforgeryAsync(HttpContext httpContext, IAntiforgery antiforgery)
        {
            StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
                return null;
            }
            catch (AntiforgeryValidationException)
            {
                return LocalCartValidationError("Security validation failed. Refresh the page and try again.");
            }
        }

        internal static IResult LocalApiValidationError(string? message)
        {
            return LocalApiError(message, StatusCodes.Status400BadRequest, "validation_error");
        }

        internal static IResult LocalCartValidationError(string? message)
        {
            return LocalCartError(message, StatusCodes.Status400BadRequest, "validation_error");
        }

        internal static IResult LocalSignInRequired()
        {
            return LocalApiError("Sign in is required.", StatusCodes.Status401Unauthorized, "authentication_required");
        }

        internal static IResult LocalForbidden(string? message)
        {
            return LocalApiError(message, StatusCodes.Status403Forbidden, "forbidden");
        }

        internal static IResult LocalConflict(string? message)
        {
            return LocalApiError(message, StatusCodes.Status409Conflict, "conflict");
        }

        internal static IResult LocalNotFound(string? message)
        {
            return LocalApiError(message, StatusCodes.Status404NotFound, "not_found");
        }

        internal static IResult LocalUnprocessable(string? message)
        {
            return LocalApiError(message, StatusCodes.Status422UnprocessableEntity, "unprocessable");
        }

        internal static IResult LocalUnavailable(string? message)
        {
            return LocalApiError(message, StatusCodes.Status503ServiceUnavailable, "service_unavailable", retryable: true);
        }

        internal static IResult LocalServerError(string? message = null)
        {
            return LocalApiError(
                string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message,
                StatusCodes.Status500InternalServerError,
                "server_error",
                retryable: true);
        }

        internal static IResult LocalApiError(
            string? message,
            int statusCode,
            string? code = null,
            IReadOnlyDictionary<string, string[]>? fieldErrors = null,
            bool? retryable = null)
        {
            return Results.Json(
                new StorefrontLocalApiErrorResponse(
                    NormalizeLocalErrorMessage(message),
                    code ?? DefaultLocalErrorCode(statusCode),
                    CurrentTraceId(),
                    NormalizeFieldErrors(fieldErrors),
                    retryable ?? IsRetryableLocalError(statusCode),
                    statusCode),
                statusCode: statusCode);
        }

        internal static IResult LocalCartError(
            string? message,
            int statusCode,
            string? code = null,
            IReadOnlyDictionary<string, string[]>? fieldErrors = null,
            bool? retryable = null)
        {
            return Results.Json(
                new StorefrontLocalCartErrorResponse(
                    NormalizeLocalErrorMessage(message),
                    code ?? DefaultLocalErrorCode(statusCode),
                    CurrentTraceId(),
                    NormalizeFieldErrors(fieldErrors),
                    retryable ?? IsRetryableLocalError(statusCode),
                    statusCode),
                statusCode: statusCode);
        }

        private static Dictionary<string, string[]> NormalizeFieldErrors(IReadOnlyDictionary<string, string[]>? fieldErrors)
        {
            return fieldErrors is { Count: > 0 }
                ? fieldErrors
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is { Length: > 0 })
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

        private static string? CurrentTraceId()
        {
            return Activity.Current?.TraceId.ToString();
        }

        private static string DefaultLocalErrorCode(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "validation_error",
                StatusCodes.Status401Unauthorized => "authentication_required",
                StatusCodes.Status403Forbidden => "forbidden",
                StatusCodes.Status404NotFound => "not_found",
                StatusCodes.Status408RequestTimeout => "timeout",
                StatusCodes.Status409Conflict => "conflict",
                StatusCodes.Status422UnprocessableEntity => "unprocessable",
                StatusCodes.Status429TooManyRequests => "rate_limited",
                >= 500 => "service_unavailable",
                _ => "http_" + statusCode.ToString(CultureInfo.InvariantCulture),
            };
        }

        private static bool IsRetryableLocalError(int statusCode)
        {
            return statusCode is StatusCodes.Status408RequestTimeout or StatusCodes.Status429TooManyRequests
                || statusCode >= 500;
        }

        private static string NormalizeLocalErrorMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "The request could not be completed."
                : message;
        }

        internal static string ResolveConsentVisitorKey(HttpContext httpContext, bool createIfMissing)
        {
            if (httpContext.Request.Cookies.TryGetValue(StorefrontConsentVisitorCookieName, out var existing)
                && !string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            if (!createIfMissing)
            {
                return string.Empty;
            }

            var visitorKey = Guid.NewGuid().ToString("N");
            httpContext.Response.Cookies.Append(
                StorefrontConsentVisitorCookieName,
                visitorKey,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    IsEssential = true,
                    MaxAge = TimeSpan.FromDays(180),
                });
            return visitorKey;
        }
    }
}
