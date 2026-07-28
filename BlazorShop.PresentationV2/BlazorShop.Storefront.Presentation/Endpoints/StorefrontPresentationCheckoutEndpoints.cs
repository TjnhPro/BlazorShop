namespace BlazorShop.Storefront.Presentation.Endpoints
{
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Presentation.Configuration;
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Presentation.Services;
    using BlazorShop.Storefront.Presentation.Contracts;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using static BlazorShop.Storefront.Presentation.Endpoints.StorefrontLocalEndpointSupport;
    using StorefrontCheckoutPaymentMethodRequest = BlazorShop.Storefront.Client.StorefrontCheckoutPaymentMethodRequest;
    using StorefrontCheckoutReviewRequest = BlazorShop.Storefront.Client.StorefrontCheckoutReviewRequest;
    using StorefrontCheckoutShippingMethodRequest = BlazorShop.Storefront.Client.StorefrontCheckoutShippingMethodRequest;
    using StorefrontPlaceOrderRequest = BlazorShop.Storefront.Client.StorefrontPlaceOrderRequest;

    public static class StorefrontPresentationCheckoutEndpoints
    {
        public static WebApplication MapStorefrontPresentationCheckoutEndpoints(this WebApplication app)
        {
            app.MapGet("/api/checkout", async (
                StorefrontCartTokenService cartTokenService,
                IStorefrontRuntimeCheckoutFacade checkoutFacade,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var cartResolution = await cartTokenService.ResolveAsync(httpContext, cancellationToken: cancellationToken);
                if (!cartResolution.Success
                    || string.IsNullOrWhiteSpace(cartResolution.CartToken)
                    || cartResolution.Cart?.Lines.Count is null or 0)
                {
                    return Results.Ok(CreateEmptyCheckoutState("Your cart is empty."));
                }

                var checkoutResult = await checkoutFacade.StartAsync(cartResolution.CartToken, cancellationToken);
                if (!checkoutResult.Success || checkoutResult.Value is null)
                {
                    return LocalUnavailable(checkoutResult.Error?.Message);
                }

                var displayContext = await displayContextProvider.GetAsync(cancellationToken);
                return Results.Ok(ToBrowserCheckoutState(checkoutResult.Value, displayContext, priceFormatter));
            });
            app.MapPost(StorefrontRoutes.Checkout, async (
                [FromForm] StorefrontCheckoutForm form,
                StorefrontCartTokenService cartTokenService,
                IStorefrontRuntimeCheckoutFacade checkoutFacade,
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

                var cartResolution = await cartTokenService.ResolveAsync(httpContext, importLegacyCart: false, cancellationToken: cancellationToken);
                if (!cartResolution.Success || cartResolution.Cart?.Lines.Count is null or 0)
                {
                    return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", "Your cart is empty."));
                }

                if (form.CartVersion > 0 && form.CartVersion != cartResolution.Cart.Version)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("Your cart changed. Review the latest cart and try checkout again."));
                }

                var startResult = await checkoutFacade.StartAsync(cartToken, cancellationToken);
                if (!startResult.Success || startResult.Value is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(startResult.Error?.Message));
                }

                var addressResult = await checkoutFacade.UpdateAddressesAsync(
                    cartToken,
                    startResult.Value.CheckoutSessionId.GetValueOrDefault(),
                    BuildCheckoutAddressStepRequest(form),
                    await ResolveOptionalAccessTokenAsync(sessionResolver, cancellationToken),
                    cancellationToken);
                if (!addressResult.Success || addressResult.Value is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(addressResult.Error?.Message));
                }

                var checkoutState = addressResult.Value;
                var shippingOptionKey = ResolveShippingOptionKey(checkoutState);
                if (checkoutState.ShippingRequired == true && string.IsNullOrWhiteSpace(shippingOptionKey))
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("Shipping is not available for this checkout."));
                }

                if (!string.IsNullOrWhiteSpace(shippingOptionKey))
                {
                    var shippingResult = await checkoutFacade.SelectShippingMethodAsync(
                        cartToken,
                        checkoutState.CheckoutSessionId.GetValueOrDefault(),
                        new StorefrontCheckoutShippingMethodRequest { ShippingOptionKey = shippingOptionKey },
                        cancellationToken);
                    if (!shippingResult.Success || shippingResult.Value is null)
                    {
                        return Results.Redirect(BuildCheckoutErrorUrl(shippingResult.Error?.Message));
                    }

                    checkoutState = shippingResult.Value;
                }

                var paymentMethodKey = ResolvePaymentMethodKey(form, checkoutState);
                if (string.IsNullOrWhiteSpace(paymentMethodKey))
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("No payment method is currently available."));
                }

                var paymentResult = await checkoutFacade.SelectPaymentMethodAsync(
                    cartToken,
                    checkoutState.CheckoutSessionId.GetValueOrDefault(),
                    new StorefrontCheckoutPaymentMethodRequest { PaymentMethodKey = paymentMethodKey },
                    cancellationToken);
                if (!paymentResult.Success || paymentResult.Value is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(paymentResult.Error?.Message));
                }

                var reviewResult = await checkoutFacade.ReviewAsync(
                    cartToken,
                    paymentResult.Value.CheckoutSessionId.GetValueOrDefault(),
                    new StorefrontCheckoutReviewRequest(),
                    cancellationToken);
                if (!reviewResult.Success || reviewResult.Value is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(reviewResult.Error?.Message));
                }

                if (reviewResult.Value.PlaceOrderAllowed != true)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(
                        reviewResult.Value.Issues.FirstOrDefault()?.Message
                            ?? "Review checkout details before placing the order."));
                }

                var placeOrderResult = await checkoutFacade.PlaceOrderAsync(
                    new StorefrontPlaceOrderRequest
                    {
                        CheckoutSessionId = reviewResult.Value.CheckoutSessionId.GetValueOrDefault(),
                        ExpectedCheckoutVersion = reviewResult.Value.CheckoutVersion,
                        ExpectedCartVersion = reviewResult.Value.CartVersion,
                        IdempotencyKey = string.IsNullOrWhiteSpace(form.IdempotencyKey)
                            ? Guid.NewGuid().ToString("N")
                            : form.IdempotencyKey.Trim(),
                    },
                    cancellationToken);
                if (!placeOrderResult.Success || placeOrderResult.Value is null)
                {
                    return Results.Redirect(BuildCheckoutErrorUrl(placeOrderResult.Error?.Message));
                }

                var nextAction = placeOrderResult.Value.NextAction;
                var nextActionUrl = nextAction?.Url;
                if (string.Equals(nextAction?.Type, "redirect", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(nextActionUrl))
                {
                    return Results.Redirect(nextActionUrl);
                }

                if (string.IsNullOrWhiteSpace(placeOrderResult.Value.Reference))
                {
                    return Results.Redirect(BuildCheckoutErrorUrl("Order confirmation is not available yet."));
                }

                httpContext.Response.Cookies.Delete(StorefrontCookieNames.Cart, new CookieOptions { Path = "/" });
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.CartToken, new CookieOptions { Path = "/" });

                return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("orderReference", placeOrderResult.Value.Reference));
            });
            app.MapPost("/api/checkout/addresses", async (
                StorefrontBrowserCheckoutAddressRequest request,
                IStorefrontRuntimeCheckoutFacade checkoutFacade,
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IStorefrontSessionResolver sessionResolver,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartTokenService, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }

                var result = await checkoutFacade.UpdateAddressesAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    ToCheckoutAddressStepRequest(request),
                    await ResolveOptionalAccessTokenAsync(sessionResolver, cancellationToken),
                    cancellationToken);
                return await ToLocalCheckoutStateResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            }).RequireRateLimiting(StorefrontPresentationRateLimitPolicyNames.LocalCart);
            app.MapPost("/api/checkout/shipping-method", async (
                StorefrontBrowserCheckoutSelectionRequest request,
                IStorefrontRuntimeCheckoutFacade checkoutFacade,
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartTokenService, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }

                var result = await checkoutFacade.SelectShippingMethodAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    new StorefrontCheckoutShippingMethodRequest { ShippingOptionKey = request.Key },
                    cancellationToken);
                return await ToLocalCheckoutStateResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            }).RequireRateLimiting(StorefrontPresentationRateLimitPolicyNames.LocalCart);
            app.MapPost("/api/checkout/payment-method", async (
                StorefrontBrowserCheckoutSelectionRequest request,
                IStorefrontRuntimeCheckoutFacade checkoutFacade,
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartTokenService, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }

                var result = await checkoutFacade.SelectPaymentMethodAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    new StorefrontCheckoutPaymentMethodRequest { PaymentMethodKey = request.Key },
                    cancellationToken);
                return await ToLocalCheckoutStateResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            }).RequireRateLimiting(StorefrontPresentationRateLimitPolicyNames.LocalCart);
            app.MapPost("/api/checkout/review", async (
                StorefrontBrowserCheckoutReviewRequest request,
                IStorefrontRuntimeCheckoutFacade checkoutFacade,
                StorefrontCartTokenService cartTokenService,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartTokenService, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }

                var result = await checkoutFacade.ReviewAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    new StorefrontCheckoutReviewRequest
                    {
                        TermsAccepted = request.TermsAccepted,
                        TermsVersion = request.TermsVersion,
                    },
                    cancellationToken);
                if (!result.Success || result.Value is null)
                {
                    return result.Error?.Status == StorefrontRuntimeStatusCodes.Conflict
                        ? LocalConflict(result.Error.Message)
                        : LocalApiValidationError(result.Error?.Message);
                }

                var displayContext = await displayContextProvider.GetAsync(cancellationToken);
                return Results.Ok(ToBrowserCheckoutReviewState(result.Value, displayContext, priceFormatter));
            }).RequireRateLimiting(StorefrontPresentationRateLimitPolicyNames.LocalCart);
            app.MapPost("/api/checkout/place-order", async (
                StorefrontBrowserCheckoutPlaceOrderRequest request,
                IStorefrontRuntimeCheckoutFacade checkoutFacade,
                StorefrontCartTokenService cartTokenService,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartTokenService, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }

                if (request.ExpectedCheckoutVersion < 1)
                {
                    return LocalApiValidationError("Review checkout before placing the order.");
                }

                var result = await checkoutFacade.PlaceOrderAsync(
                    new StorefrontPlaceOrderRequest
                    {
                        CheckoutSessionId = request.CheckoutSessionId,
                        ExpectedCheckoutVersion = request.ExpectedCheckoutVersion,
                        ExpectedCartVersion = request.ExpectedCartVersion,
                        IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                            ? Guid.NewGuid().ToString("N")
                            : request.IdempotencyKey.Trim(),
                    },
                    cancellationToken);
                if (!result.Success || result.Value is null)
                {
                    return result.Error?.Status == StorefrontRuntimeStatusCodes.Conflict
                        ? LocalConflict(result.Error.Message)
                        : LocalApiValidationError(result.Error?.Message);
                }

                var nextActionUrl = result.Value.NextAction?.Url;
                if (string.Equals(result.Value.NextAction?.Type, "redirect", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(nextActionUrl))
                {
                    return Results.Ok(new StorefrontBrowserCheckoutPlaceOrderResult(
                        true,
                        "Continue payment.",
                        result.Value.Reference,
                        nextActionUrl));
                }

                if (string.IsNullOrWhiteSpace(result.Value.Reference))
                {
                    return LocalApiValidationError("Order confirmation is not available yet.");
                }

                httpContext.Response.Cookies.Delete(StorefrontCookieNames.Cart, new CookieOptions { Path = "/" });
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.CartToken, new CookieOptions { Path = "/" });
                return Results.Ok(new StorefrontBrowserCheckoutPlaceOrderResult(
                    true,
                    "Order placed.",
                    result.Value.Reference,
                    StorefrontRoutes.Checkout + QueryString.Create("orderReference", result.Value.Reference)));
            }).RequireRateLimiting(StorefrontPresentationRateLimitPolicyNames.LocalCart);

            return app;
        }

        private static async Task<string?> ResolveOptionalAccessTokenAsync(
            IStorefrontSessionResolver sessionResolver,
            CancellationToken cancellationToken)
        {
            var session = await sessionResolver.GetCurrentUserAsync(cancellationToken);
            return session.IsAuthenticated && !string.IsNullOrWhiteSpace(session.AccessToken)
                ? session.AccessToken
                : null;
        }
    }
}
