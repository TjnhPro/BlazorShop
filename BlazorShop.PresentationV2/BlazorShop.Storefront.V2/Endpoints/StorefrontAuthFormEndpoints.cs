namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using BlazorShop.Web.SharedV2.Models;

    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;

    using static BlazorShop.Storefront.Endpoints.StorefrontLocalEndpointSupport;

    public static class StorefrontAuthFormEndpoints
    {
        public static WebApplication MapStorefrontAuthFormEndpoints(this WebApplication app)
        {
            app.MapPost(StorefrontRoutes.SignIn, async (
                [FromForm] StorefrontLoginForm form,
                IStorefrontAuthClient authClient,
                StorefrontCartTokenService cartTokenService,
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
                await cartTokenService.MergeCurrentCustomerAsync(httpContext, result.Data.AccessToken, cancellationToken);
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
            app.MapPost(StorefrontRoutes.Checkout, async (
                [FromForm] StorefrontCheckoutForm form,
                IStorefrontCartClient cartClient,
                IStorefrontCheckoutClient checkoutClient,
                IStorefrontSessionResolver sessionResolver,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
            
                var cartToken = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
                if (string.IsNullOrWhiteSpace(cartToken))
                {
                    return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", "Your cart is empty."));
                }
            
                var cartResult = await cartClient.GetCartAsync(cartToken, cancellationToken);
                if (!cartResult.Success || cartResult.Data is null || cartResult.Data.Lines.Count == 0)
                {
                    return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", "Your cart is empty."));
                }
            
                if (form.CartVersion > 0 && form.CartVersion != cartResult.Data.Version)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("Your cart changed. Review the latest cart and try checkout again."));
                }
            
                var startResult = await checkoutClient.StartCheckoutAsync(cartToken, cancellationToken);
                if (!startResult.Success || startResult.Data is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(startResult.Message));
                }
            
                var customerSession = await sessionResolver.GetCurrentUserAsync(cancellationToken);
                var customerAccessToken = customerSession.IsAuthenticated
                    ? customerSession.AccessToken
                    : null;
                var addressResult = await checkoutClient.UpdateCheckoutAddressesAsync(
                    cartToken,
                    startResult.Data.CheckoutSessionId,
                    BuildCheckoutAddressStepRequest(form),
                    cancellationToken,
                    customerAccessToken);
                if (!addressResult.Success || addressResult.Data is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(addressResult.Message));
                }
            
                var checkoutState = addressResult.Data;
                var shippingOptionKey = ResolveShippingOptionKey(checkoutState);
                if (checkoutState.ShippingRequired && string.IsNullOrWhiteSpace(shippingOptionKey))
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("Shipping is not available for this checkout."));
                }
            
                if (!string.IsNullOrWhiteSpace(shippingOptionKey))
                {
                    var shippingResult = await checkoutClient.SelectCheckoutShippingMethodAsync(
                        cartToken,
                        checkoutState.CheckoutSessionId,
                        new StorefrontCheckoutShippingMethodRequest { ShippingOptionKey = shippingOptionKey },
                        cancellationToken);
                    if (!shippingResult.Success || shippingResult.Data is null)
                    {
                        return Results.Redirect(BuildCheckoutErrorUrl(shippingResult.Message));
                    }
            
                    checkoutState = shippingResult.Data;
                }
            
                var paymentMethodKey = ResolvePaymentMethodKey(form, checkoutState);
                if (string.IsNullOrWhiteSpace(paymentMethodKey))
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("No payment method is currently available."));
                }
            
                var paymentResult = await checkoutClient.SelectCheckoutPaymentMethodAsync(
                    cartToken,
                    checkoutState.CheckoutSessionId,
                    new StorefrontCheckoutPaymentMethodRequest { PaymentMethodKey = paymentMethodKey },
                    cancellationToken);
                if (!paymentResult.Success || paymentResult.Data is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(paymentResult.Message));
                }
            
                var reviewResult = await checkoutClient.ReviewCheckoutAsync(
                    cartToken,
                    paymentResult.Data.CheckoutSessionId,
                    new StorefrontCheckoutReviewRequest(),
                    cancellationToken);
                if (!reviewResult.Success || reviewResult.Data is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(reviewResult.Message));
                }
            
                if (!reviewResult.Data.PlaceOrderAllowed)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(
                        reviewResult.Data.Issues.FirstOrDefault()?.Message
                            ?? "Review checkout details before placing the order."));
                }
            
                var placeOrderResult = await checkoutClient.PlaceOrderAsync(
                    new StorefrontPlaceOrderRequest
                    {
                        CheckoutSessionId = reviewResult.Data.CheckoutSessionId,
                        ExpectedCheckoutVersion = reviewResult.Data.CheckoutVersion,
                        ExpectedCartVersion = reviewResult.Data.CartVersion,
                        IdempotencyKey = string.IsNullOrWhiteSpace(form.IdempotencyKey)
                            ? Guid.NewGuid().ToString("N")
                            : form.IdempotencyKey.Trim(),
                    },
                    cartToken,
                    cancellationToken);
                if (!placeOrderResult.Success || placeOrderResult.Data is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(placeOrderResult.Message));
                }
            
                var nextAction = placeOrderResult.Data.NextAction;
                var nextActionUrl = nextAction?.Url;
                if (string.Equals(nextAction?.Type, "redirect", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(nextActionUrl))
                {
                    return Results.Redirect(nextActionUrl);
                }
            
                if (string.IsNullOrWhiteSpace(placeOrderResult.Data.Reference))
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("Order confirmation is not available yet."));
                }
            
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.Cart, new CookieOptions { Path = "/" });
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.CartToken, new CookieOptions { Path = "/" });
            
                return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("orderReference", placeOrderResult.Data.Reference));
            });

            return app;
        }
    }
}

