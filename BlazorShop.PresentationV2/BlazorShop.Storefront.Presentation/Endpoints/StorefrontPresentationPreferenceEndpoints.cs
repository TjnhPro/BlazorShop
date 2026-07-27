namespace BlazorShop.Storefront.Presentation.Endpoints;

using BlazorShop.Storefront.Configuration;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

public static class StorefrontPresentationPreferenceEndpoints
{
    public static WebApplication MapStorefrontPresentationPreferenceEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(StorefrontRoutes.CurrencyPreference, async (
            [FromForm] StorefrontCurrencyPreferenceForm form,
            IStorefrontStoreConfigurationClient apiClient,
            HttpContext httpContext,
            IHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
            var requestedCurrencyCode = NormalizeCurrencyCode(form.CurrencyCode);
            if (requestedCurrencyCode is null)
            {
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.CurrencyPreference, new CookieOptions { Path = "/" });
                return Results.Redirect(safeReturnUrl);
            }

            var result = await apiClient.SetCurrencyPreferenceAsync(
                new StorefrontCurrencyPreferenceRequest { CurrencyCode = requestedCurrencyCode },
                cancellationToken);
            if (!result.Success || result.Data is null || !result.Data.RequestedCurrencySupported || !result.Data.CheckoutCurrencyEnabled)
            {
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.CurrencyPreference, new CookieOptions { Path = "/" });
                return Results.Redirect(safeReturnUrl);
            }

            httpContext.Response.Cookies.Append(
                StorefrontCookieNames.CurrencyPreference,
                result.Data.CurrencyCode,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = TimeSpan.FromDays(30),
                });

            return Results.Redirect(safeReturnUrl);
        });

        return app;
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim().ToUpperInvariant();
        return normalized is { Length: 3 } && normalized.All(char.IsLetter)
            ? normalized
            : null;
    }
}
