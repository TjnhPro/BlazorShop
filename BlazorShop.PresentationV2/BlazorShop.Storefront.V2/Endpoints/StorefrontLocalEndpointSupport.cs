namespace BlazorShop.Storefront.Endpoints
{
    using System.Globalization;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using BlazorShop.Web.SharedV2.Models;

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
            return LocalApiError(message, StatusCodes.Status400BadRequest);
        }

        internal static IResult LocalCartValidationError(string? message)
        {
            return LocalCartError(message, StatusCodes.Status400BadRequest);
        }

        internal static IResult LocalSignInRequired()
        {
            return LocalApiError("Sign in is required.", StatusCodes.Status401Unauthorized);
        }

        internal static IResult LocalForbidden(string? message)
        {
            return LocalApiError(message, StatusCodes.Status403Forbidden);
        }

        internal static IResult LocalConflict(string? message)
        {
            return LocalApiError(message, StatusCodes.Status409Conflict);
        }

        internal static IResult LocalUnprocessable(string? message)
        {
            return LocalApiError(message, StatusCodes.Status422UnprocessableEntity);
        }

        internal static IResult LocalServerError(string? message = null)
        {
            return LocalApiError(
                string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message,
                StatusCodes.Status500InternalServerError);
        }

        internal static IResult LocalApiError(string? message, int statusCode)
        {
            return Results.Json(
                new StorefrontLocalApiErrorResponse(NormalizeLocalErrorMessage(message)),
                statusCode: statusCode);
        }

        internal static IResult LocalCartError(string? message, int statusCode)
        {
            return Results.Json(
                new StorefrontLocalCartErrorResponse(NormalizeLocalErrorMessage(message)),
                statusCode: statusCode);
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
