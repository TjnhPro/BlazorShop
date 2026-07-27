namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using Microsoft.AspNetCore.Mvc;

    using static BlazorShop.Storefront.Endpoints.StorefrontLocalEndpointSupport;

    public static class StorefrontAuthFormEndpoints
    {
        public static WebApplication MapStorefrontAuthFormEndpoints(this WebApplication app)
        {
            app.MapPost(StorefrontRoutes.AccountProfile, async (
                [FromForm] StorefrontAccountProfileForm form,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                CancellationToken cancellationToken) =>
            {
                var session = await sessionResolver.GetCurrentUserAsync(cancellationToken);
                if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(StorefrontRoutes.AccountProfile));
                }
            
                if (string.IsNullOrWhiteSpace(form.FullName) || string.IsNullOrWhiteSpace(form.Email))
                {
                    return Results.Redirect(StorefrontReturnUrl.BuildAccountProfileUrl("Full name and email are required."));
                }
            
                var result = await apiClient.UpdateCustomerProfileAsync(
                    session.AccessToken,
                    new StorefrontCustomerProfileUpdateRequest
                    {
                        FullName = form.FullName.Trim(),
                        Email = form.Email.Trim(),
                        FirstName = NormalizeOptionalFormValue(form.FirstName),
                        LastName = NormalizeOptionalFormValue(form.LastName),
                        Company = NormalizeOptionalFormValue(form.Company),
                        PhoneNumber = NormalizeOptionalFormValue(form.PhoneNumber),
                        PreferredLanguage = NormalizeOptionalFormValue(form.PreferredLanguage),
                        PreferredCurrencyCode = NormalizeOptionalFormValue(form.PreferredCurrencyCode),
                    },
                    cancellationToken);
            
                return result.Success
                    ? Results.Redirect(StorefrontReturnUrl.BuildAccountProfileUrl(saved: true))
                    : Results.Redirect(StorefrontReturnUrl.BuildAccountProfileUrl(result.Message));
            });
            app.MapPost(StorefrontRoutes.AccountChangePassword, async (
                [FromForm] StorefrontChangePasswordForm form,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontAuthClient authClient,
                CancellationToken cancellationToken) =>
            {
                var session = await sessionResolver.GetCurrentUserAsync(cancellationToken);
                if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(StorefrontRoutes.AccountChangePassword));
                }
            
                if (string.IsNullOrWhiteSpace(form.CurrentPassword)
                    || string.IsNullOrWhiteSpace(form.NewPassword)
                    || string.IsNullOrWhiteSpace(form.ConfirmPassword))
                {
                    return Results.Redirect(StorefrontReturnUrl.BuildAccountChangePasswordUrl("All password fields are required."));
                }
            
                if (!string.Equals(form.NewPassword, form.ConfirmPassword, StringComparison.Ordinal))
                {
                    return Results.Redirect(StorefrontReturnUrl.BuildAccountChangePasswordUrl("Passwords do not match."));
                }
            
                var result = await authClient.ChangePasswordAsync(
                    session.AccessToken,
                    new ChangePassword
                    {
                        CurrentPassword = form.CurrentPassword,
                        NewPassword = form.NewPassword,
                        ConfirmPassword = form.ConfirmPassword,
                    },
                    cancellationToken);
            
                return result.Success
                    ? Results.Redirect(StorefrontReturnUrl.BuildAccountChangePasswordUrl(saved: true))
                    : Results.Redirect(StorefrontReturnUrl.BuildAccountChangePasswordUrl(result.Message));
            });
            app.MapPost(StorefrontRoutes.AccountAddresses, async (
                [FromForm] StorefrontAccountAddressForm form,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                CancellationToken cancellationToken) =>
            {
                var session = await sessionResolver.GetCurrentUserAsync(cancellationToken);
                if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(StorefrontRoutes.AccountAddresses));
                }
            
                var result = await ExecuteCustomerAddressCommandAsync(apiClient, session.AccessToken, form, cancellationToken);
            
                return result.Success
                    ? Results.Redirect(StorefrontReturnUrl.BuildAccountAddressesUrl(saved: true))
                    : Results.Redirect(StorefrontReturnUrl.BuildAccountAddressesUrl(result.Message));
            });
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
    }
}

