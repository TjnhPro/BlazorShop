namespace BlazorShop.Storefront.Presentation.Services.Auth;

using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.AspNetCore.Http;

public sealed class StorefrontAuthPageService
{
    private readonly IStorefrontSessionResolver sessionResolver;
    private readonly IStorefrontAuthClient authClient;

    public StorefrontAuthPageService(
        IStorefrontSessionResolver sessionResolver,
        IStorefrontAuthClient authClient)
    {
        this.sessionResolver = sessionResolver;
        this.authClient = authClient;
    }

    public async Task<StorefrontAuthPageResult> GetSignInAsync(
        string? returnUrl,
        string? error,
        string? registered,
        string? passwordReset,
        CancellationToken cancellationToken = default)
    {
        var safeReturnUrl = StorefrontReturnUrl.Normalize(returnUrl);
        var session = await this.sessionResolver.GetCurrentUserAsync(cancellationToken);
        if (session.IsAuthenticated)
        {
            return new StorefrontAuthPageResult(null, safeReturnUrl);
        }

        var successMessage = registered == "1"
            ? "Account created. Sign in to continue."
            : passwordReset == "1"
                ? "Password updated. Sign in with your new password."
                : null;

        return new StorefrontAuthPageResult(
            new StorefrontAuthPageContext(
                StorefrontAuthPageKind.SignIn,
                "Sign in",
                "Customer account",
                "Access checkout, order history, and customer details for this storefront.",
                error,
                successMessage,
                safeReturnUrl,
                IsRejectedReturnUrl(returnUrl, safeReturnUrl),
                StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl),
                StorefrontRoutes.SignIn,
                BuildRegisterUrl(safeReturnUrl),
                StorefrontRoutes.ForgotPassword,
                null,
                null,
                true,
                "Customer registration is disabled.",
                false),
            null);
    }

    public async Task<StorefrontAuthPageResult> GetRegisterAsync(
        string? returnUrl,
        string? error,
        CancellationToken cancellationToken = default)
    {
        var safeReturnUrl = StorefrontReturnUrl.Normalize(returnUrl);
        var session = await this.sessionResolver.GetCurrentUserAsync(cancellationToken);
        if (session.IsAuthenticated)
        {
            return new StorefrontAuthPageResult(null, safeReturnUrl);
        }

        var registrationAllowed = true;
        var registrationMessage = "Customer registration is disabled.";
        var policy = await this.authClient.GetRegistrationPolicyAsync(cancellationToken);
        if (policy.Success && policy.Data is not null)
        {
            registrationAllowed = policy.Data.RegistrationAllowed;
            registrationMessage = policy.Data.Message;
        }

        return new StorefrontAuthPageResult(
            new StorefrontAuthPageContext(
                StorefrontAuthPageKind.Register,
                "Create account",
                "Customer account",
                "Create a customer profile for checkout and order tracking on this storefront.",
                error,
                null,
                safeReturnUrl,
                IsRejectedReturnUrl(returnUrl, safeReturnUrl),
                StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl),
                StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl),
                StorefrontRoutes.Register,
                StorefrontRoutes.ForgotPassword,
                null,
                null,
                registrationAllowed,
                registrationMessage,
                false),
            null);
    }

    public StorefrontAuthPageResult GetForgotPassword(string? email, string? error, string? sent)
    {
        return new StorefrontAuthPageResult(
            new StorefrontAuthPageContext(
                StorefrontAuthPageKind.ForgotPassword,
                "Forgot password",
                "Account recovery",
                "Enter your email address and we will send reset instructions if a matching customer account exists.",
                error,
                sent == "1" ? "If that email is registered, password reset instructions will arrive shortly." : null,
                StorefrontRoutes.Home,
                false,
                StorefrontRoutes.ForgotPassword,
                StorefrontRoutes.SignIn,
                StorefrontRoutes.Register,
                StorefrontRoutes.ForgotPassword,
                email,
                null,
                true,
                "Customer registration is disabled.",
                false),
            null);
    }

    public StorefrontAuthPageResult GetResetPassword(string? email, string? token, string? error)
    {
        return new StorefrontAuthPageResult(
            new StorefrontAuthPageContext(
                StorefrontAuthPageKind.ResetPassword,
                "Reset password",
                "Account recovery",
                "Choose a new password for your storefront customer account.",
                error,
                null,
                StorefrontRoutes.Home,
                false,
                StorefrontRoutes.ResetPassword,
                StorefrontRoutes.SignIn,
                StorefrontRoutes.Register,
                StorefrontRoutes.ForgotPassword,
                email,
                token,
                true,
                "Customer registration is disabled.",
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token)),
            null);
    }

    private static string BuildRegisterUrl(string safeReturnUrl)
    {
        return string.IsNullOrWhiteSpace(safeReturnUrl) || safeReturnUrl == StorefrontRoutes.Home
            ? StorefrontRoutes.Register
            : $"{StorefrontRoutes.Register}{QueryString.Create("returnUrl", safeReturnUrl)}";
    }

    private static bool IsRejectedReturnUrl(string? returnUrl, string safeReturnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
            && !string.Equals(returnUrl.Trim(), safeReturnUrl, StringComparison.Ordinal);
    }
}
