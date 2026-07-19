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
    [Route("api/storefront/stores/{storeKey}/consent")]
    public sealed class StorefrontScopedConsentController : StorefrontApiControllerBase
    {
        private const string ConsentVisitorHeaderName = "X-Consent-Visitor";

        private readonly IStorefrontConsentService consentService;

        public StorefrontScopedConsentController(
            ICommerceStoreContext storeContext,
            IStorefrontConsentService consentService)
            : base(storeContext)
        {
            this.consentService = consentService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> Current(
            [FromHeader(Name = ConsentVisitorHeaderName)] string? visitorKey,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.consentService.GetCurrentAsync(storeId.Value, visitorKey, cancellationToken);
            return this.FromServiceResponse(result, snapshot => snapshot?.ToStorefrontContract());
        }

        [HttpPost]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Newsletter)]
        public async Task<IActionResult> Save(
            [FromHeader(Name = ConsentVisitorHeaderName)] string visitorKey,
            [FromBody] BlazorShop.CommerceNode.API.Contracts.Storefront.StorefrontConsentSaveRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.consentService.SaveAsync(
                storeId.Value,
                visitorKey,
                new BlazorShop.Application.CommerceNode.Consent.StorefrontConsentSaveRequest(
                    request.Preferences,
                    request.Analytics,
                    request.Marketing),
                cancellationToken);
            return this.FromServiceResponse(result, snapshot => snapshot?.ToStorefrontContract());
        }

        [HttpPost("revoke")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Newsletter)]
        public async Task<IActionResult> Revoke(
            [FromHeader(Name = ConsentVisitorHeaderName)] string visitorKey,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.consentService.RevokeAsync(storeId.Value, visitorKey, cancellationToken);
            return this.FromServiceResponse(result, snapshot => snapshot?.ToStorefrontContract());
        }

    }

}
