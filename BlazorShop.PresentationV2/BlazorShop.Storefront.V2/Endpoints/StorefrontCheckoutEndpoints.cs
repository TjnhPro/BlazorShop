namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Application.DTOs.UserIdentity;
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
    using Microsoft.AspNetCore.Mvc;

    using static BlazorShop.Storefront.Endpoints.StorefrontLocalEndpointSupport;

    public static class StorefrontCheckoutEndpoints
    {
        public static WebApplication MapStorefrontCheckoutEndpoints(this WebApplication app)
        {
            app.MapGet("/api/checkout", async (
                StorefrontCartTokenService cartTokenService,
                IStorefrontCheckoutClient apiClient,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var cartResolution = await cartTokenService.ResolveAsync(httpContext, cancellationToken: cancellationToken);
                if (!cartResolution.Success || string.IsNullOrWhiteSpace(cartResolution.CartToken) || cartResolution.Cart?.Lines.Count is null or 0)
                {
                    return Results.Ok(CreateEmptyCheckoutState("Your cart is empty."));
                }
            
                var checkoutResult = await apiClient.StartCheckoutAsync(cartResolution.CartToken, cancellationToken);
                if (!checkoutResult.Success || checkoutResult.Data is null)
                {
                    return Results.Json(new StorefrontLocalApiErrorResponse(checkoutResult.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            
                var displayContext = await displayContextProvider.GetAsync(cancellationToken);
                return Results.Ok(ToBrowserCheckoutState(checkoutResult.Data, displayContext, priceFormatter));
            });
            app.MapPost("/api/checkout/addresses", async (
                StorefrontBrowserCheckoutAddressRequest request,
                IStorefrontCheckoutClient apiClient,
                IStorefrontCartClient cartClient,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IStorefrontSessionResolver sessionResolver,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartClient, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }
            
                var customerSession = await sessionResolver.GetCurrentUserAsync(cancellationToken);
                var customerAccessToken = customerSession.IsAuthenticated
                    ? customerSession.AccessToken
                    : null;
                var result = await apiClient.UpdateCheckoutAddressesAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    ToCheckoutAddressStepRequest(request),
                    cancellationToken,
                    customerAccessToken);
                return await ToLocalCheckoutStateResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            });
            app.MapPost("/api/checkout/shipping-method", async (
                StorefrontBrowserCheckoutSelectionRequest request,
                IStorefrontCheckoutClient apiClient,
                IStorefrontCartClient cartClient,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartClient, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }
            
                var result = await apiClient.SelectCheckoutShippingMethodAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    new StorefrontCheckoutShippingMethodRequest { ShippingOptionKey = request.Key },
                    cancellationToken);
                return await ToLocalCheckoutStateResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            });
            app.MapPost("/api/checkout/payment-method", async (
                StorefrontBrowserCheckoutSelectionRequest request,
                IStorefrontCheckoutClient apiClient,
                IStorefrontCartClient cartClient,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartClient, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }
            
                var result = await apiClient.SelectCheckoutPaymentMethodAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    new StorefrontCheckoutPaymentMethodRequest { PaymentMethodKey = request.Key },
                    cancellationToken);
                return await ToLocalCheckoutStateResultAsync(result, displayContextProvider, priceFormatter, cancellationToken);
            });
            app.MapPost("/api/checkout/review", async (
                StorefrontBrowserCheckoutReviewRequest request,
                IStorefrontCheckoutClient apiClient,
                IStorefrontCartClient cartClient,
                IStorefrontDisplayContextProvider displayContextProvider,
                IStorefrontPriceFormatter priceFormatter,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartClient, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }
            
                var result = await apiClient.ReviewCheckoutAsync(
                    guard.CartToken!,
                    request.CheckoutSessionId,
                    new StorefrontCheckoutReviewRequest
                    {
                        TermsAccepted = request.TermsAccepted,
                        TermsVersion = request.TermsVersion,
                    },
                    cancellationToken);
                if (!result.Success || result.Data is null)
                {
                    return Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
                }
            
                var displayContext = await displayContextProvider.GetAsync(cancellationToken);
                return Results.Ok(ToBrowserCheckoutReviewState(result.Data, displayContext, priceFormatter));
            });
            app.MapPost("/api/checkout/place-order", async (
                StorefrontBrowserCheckoutPlaceOrderRequest request,
                IStorefrontCheckoutClient apiClient,
                IStorefrontCartClient cartClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var guard = await ValidateLocalCheckoutCommandAsync(httpContext, antiforgery, cartClient, request.CheckoutSessionId, request.ExpectedCartVersion, cancellationToken);
                if (guard.Failure is not null)
                {
                    return guard.Failure;
                }
            
                if (request.ExpectedCheckoutVersion < 1)
                {
                    return Results.BadRequest(new StorefrontLocalApiErrorResponse("Review checkout before placing the order."));
                }
            
                var result = await apiClient.PlaceOrderAsync(
                    new StorefrontPlaceOrderRequest
                    {
                        CheckoutSessionId = request.CheckoutSessionId,
                        ExpectedCheckoutVersion = request.ExpectedCheckoutVersion,
                        ExpectedCartVersion = request.ExpectedCartVersion,
                        IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                            ? Guid.NewGuid().ToString("N")
                            : request.IdempotencyKey.Trim(),
                    },
                    guard.CartToken,
                    cancellationToken);
                if (!result.Success || result.Data is null)
                {
                    return Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
                }
            
                var nextActionUrl = result.Data.NextAction?.Url;
                if (string.Equals(result.Data.NextAction?.Type, "redirect", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(nextActionUrl))
                {
                    return Results.Ok(new StorefrontBrowserCheckoutPlaceOrderResult(
                        true,
                        "Continue payment.",
                        result.Data.Reference,
                        nextActionUrl));
                }
            
                if (string.IsNullOrWhiteSpace(result.Data.Reference))
                {
                    return Results.Json(new StorefrontLocalApiErrorResponse("Order confirmation is not available yet."), statusCode: StatusCodes.Status400BadRequest);
                }
            
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.Cart, new CookieOptions { Path = "/" });
                httpContext.Response.Cookies.Delete(StorefrontCookieNames.CartToken, new CookieOptions { Path = "/" });
                return Results.Ok(new StorefrontBrowserCheckoutPlaceOrderResult(
                    true,
                    "Order placed.",
                    result.Data.Reference,
                    StorefrontRoutes.Checkout + QueryString.Create("orderReference", result.Data.Reference)));
            });

            return app;
        }
    }
}

