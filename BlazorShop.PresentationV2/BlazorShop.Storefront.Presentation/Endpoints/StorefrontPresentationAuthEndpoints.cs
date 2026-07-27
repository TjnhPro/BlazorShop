namespace BlazorShop.Storefront.Presentation.Endpoints;

using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

public static class StorefrontPresentationAuthEndpoints
{
    public static WebApplication MapStorefrontPresentationAuthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost(StorefrontRoutes.SignIn, async (
            [FromForm] StorefrontLoginForm form,
            IStorefrontAuthClient authClient,
            IStorefrontCartMergeService cartMergeService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
            if (string.IsNullOrWhiteSpace(form.Email) || string.IsNullOrWhiteSpace(form.Password))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl, "Email and password are required."));
            }

            var result = await authClient.LoginAsync(
                new LoginUser
                {
                    Email = form.Email.Trim(),
                    Password = form.Password,
                    CaptchaToken = form.CaptchaToken,
                },
                cancellationToken);

            if (!result.Success || result.Data is null || string.IsNullOrWhiteSpace(result.Data.AccessToken))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl, result.Message));
            }

            StorefrontCookieBridge.CopySetCookieHeaders(result.SetCookieHeaders, httpContext.Response);
            await cartMergeService.MergeCurrentCustomerAsync(httpContext, result.Data.AccessToken, cancellationToken);
            return Results.Redirect(safeReturnUrl);
        });

        app.MapPost(StorefrontRoutes.Register, async (
            [FromForm] StorefrontRegisterForm form,
            IStorefrontAuthClient authClient,
            CancellationToken cancellationToken) =>
        {
            var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
            var policy = await authClient.GetRegistrationPolicyAsync(cancellationToken);
            if (policy.Success
                && policy.Data is not null
                && !policy.Data.RegistrationAllowed)
            {
                return Results.Redirect(StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl, policy.Data.Message));
            }

            if (string.IsNullOrWhiteSpace(form.FullName)
                || string.IsNullOrWhiteSpace(form.Email)
                || string.IsNullOrWhiteSpace(form.Password)
                || string.IsNullOrWhiteSpace(form.ConfirmPassword))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl, "All fields are required."));
            }

            if (!string.Equals(form.Password, form.ConfirmPassword, StringComparison.Ordinal))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl, "Passwords do not match."));
            }

            var result = await authClient.RegisterAsync(
                new CreateUser
                {
                    FullName = form.FullName.Trim(),
                    Email = form.Email.Trim(),
                    Password = form.Password,
                    ConfirmPassword = form.ConfirmPassword,
                    CaptchaToken = form.CaptchaToken,
                },
                cancellationToken);

            if (!result.Success)
            {
                return Results.Redirect(StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl, result.Message));
            }

            return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl, registered: true));
        });

        app.MapPost(StorefrontRoutes.ForgotPassword, async (
            [FromForm] StorefrontForgotPasswordForm form,
            IStorefrontAuthClient authClient,
            CancellationToken cancellationToken) =>
        {
            var email = form.Email?.Trim();
            if (!IsValidEmail(email))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildForgotPasswordUrl(email, "Enter a valid email address."));
            }

            var result = await authClient.ForgotPasswordAsync(email!, form.CaptchaToken, cancellationToken);
            return result.Success
                ? Results.Redirect(StorefrontReturnUrl.BuildForgotPasswordUrl(email, sent: true))
                : Results.Redirect(StorefrontReturnUrl.BuildForgotPasswordUrl(email, "Password recovery is temporarily unavailable. Try again shortly."));
        });

        app.MapPost(StorefrontRoutes.ResetPassword, async (
            [FromForm] StorefrontResetPasswordForm form,
            IStorefrontAuthClient authClient,
            CancellationToken cancellationToken) =>
        {
            var email = form.Email?.Trim();
            var token = form.Token?.Trim();
            if (!IsValidEmail(email) || string.IsNullOrWhiteSpace(token))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildResetPasswordUrl(email, token, "This reset link is invalid or expired."));
            }

            if (string.IsNullOrWhiteSpace(form.Password)
                || string.IsNullOrWhiteSpace(form.ConfirmPassword))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildResetPasswordUrl(email, token, "Password and confirmation are required."));
            }

            if (!string.Equals(form.Password, form.ConfirmPassword, StringComparison.Ordinal))
            {
                return Results.Redirect(StorefrontReturnUrl.BuildResetPasswordUrl(email, token, "Passwords do not match."));
            }

            var result = await authClient.ResetPasswordAsync(email!, token!, form.Password, form.ConfirmPassword, cancellationToken);
            return result.Success
                ? Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(passwordReset: true))
                : Results.Redirect(StorefrontReturnUrl.BuildResetPasswordUrl(email, token, "This reset link is invalid or expired."));
        });

        app.MapPost(StorefrontRoutes.Logout, async (
            [FromForm] StorefrontLogoutForm form,
            IStorefrontAuthClient authClient,
            IConfiguration configuration,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
            var cookieHeader = StorefrontAuthCookies.BuildRefreshTokenCookieHeader(httpContext.Request, configuration);
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var result = await authClient.LogoutAsync(cookieHeader, userAgent, cancellationToken);
            StorefrontCookieBridge.CopySetCookieHeaders(result.SetCookieHeaders, httpContext.Response);

            if (result.SetCookieHeaders.Count == 0)
            {
                httpContext.Response.Cookies.Delete(
                    StorefrontAuthCookies.GetRefreshTokenCookieName(configuration),
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Path = "/",
                    });
            }

            return Results.Redirect(safeReturnUrl);
        });

        return app;
    }

    private static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && email.Contains('@', StringComparison.Ordinal)
            && email.IndexOf('@', StringComparison.Ordinal) > 0
            && email.LastIndexOf('@') < email.Length - 1;
    }
}
