namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using ApplicationStorefrontCheckoutResult = BlazorShop.Application.DTOs.Payment.StorefrontCheckoutResult;
    using ApplicationStorefrontCheckoutPreviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutPreviewResult;
    using ApplicationStorefrontCheckoutReviewResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutReviewResult;
    using ApplicationStorefrontCheckoutSessionRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionRequest;
    using ApplicationStorefrontCheckoutSessionResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutSessionResult;
    using ApplicationStorefrontCheckoutStartRequest = BlazorShop.Application.CommerceNode.Checkout.StorefrontCheckoutStartRequest;
    using ApplicationStorefrontPlaceOrderResult = BlazorShop.Application.CommerceNode.Checkout.StorefrontPlaceOrderResult;
    using IStorefrontCheckoutService = BlazorShop.Application.CommerceNode.Checkout.IStorefrontCheckoutService;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Orders;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.CommerceNode.API.Configuration;
    using BlazorShop.CommerceNode.API.Contracts.Storefront;
    using BlazorShop.CommerceNode.API.Responses;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.Options;

    [ApiController]
    [Route("api/storefront/stores/{storeKey}/currency")]
    public sealed class StorefrontScopedCurrencyController : StorefrontApiControllerBase
    {
        private const string WorkingCurrencyCookieName = "blazorshop-working-currency";

        private readonly ICommerceStoreContext storeContext;
        private readonly IStorefrontWorkingCurrencyResolver workingCurrencyResolver;

        public StorefrontScopedCurrencyController(
            ICommerceStoreContext storeContext,
            IStorefrontWorkingCurrencyResolver workingCurrencyResolver)
        {
            this.storeContext = storeContext;
            this.workingCurrencyResolver = workingCurrencyResolver;
        }

        [HttpPost("preference")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Currency)]
        public async Task<IActionResult> SetPreference(
            [FromBody] StorefrontCurrencyPreferenceRequest request,
            CancellationToken cancellationToken)
        {
            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var resolution = await this.workingCurrencyResolver.ResolveAsync(
                storeIdResult.Payload,
                request.CurrencyCode,
                cancellationToken);

            if (resolution.RequestedCurrencySupported && resolution.CheckoutCurrencyEnabled)
            {
                this.Response.Cookies.Append(
                    WorkingCurrencyCookieName,
                    resolution.CurrencyCode,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = this.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        MaxAge = TimeSpan.FromDays(30),
                    });
            }
            else
            {
                this.Response.Cookies.Delete(WorkingCurrencyCookieName, new CookieOptions { Path = "/" });
            }

            return this.Success(resolution.ToStorefrontContract(), "Currency preference resolved.");
        }
    }

}
