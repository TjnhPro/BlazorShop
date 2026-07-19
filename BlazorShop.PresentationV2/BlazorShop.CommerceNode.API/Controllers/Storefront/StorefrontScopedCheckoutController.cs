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
    [Route("api/storefront/stores/{storeKey}/checkout")]
    [Authorize]
    public sealed class StorefrontScopedCheckoutController : StorefrontApiControllerBase
    {
        private const string CartTokenHeaderName = "X-Cart-Token";

        private readonly IStorefrontCheckoutService checkoutService;

        public StorefrontScopedCheckoutController(
            IStorefrontCheckoutService checkoutService,
            ICommerceStoreContext storeContext)
            : base(storeContext)
        {
            this.checkoutService = checkoutService;
        }

        [HttpPost("start")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> Start(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCheckoutStartRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.StartAsync(
                new ApplicationStorefrontCheckoutStartRequest(storeId.Value, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutSessionResult session
                    ? session.ToStorefrontContract()
                    : null);
        }

        [HttpGet("{checkoutSessionId:guid}")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> Load(
            Guid checkoutSessionId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.LoadAsync(
                new ApplicationStorefrontCheckoutSessionRequest(storeId.Value, checkoutSessionId, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutSessionResult session
                    ? session.ToStorefrontContract()
                    : null);
        }

        [HttpPost("{checkoutSessionId:guid}/cancel")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> Cancel(
            Guid checkoutSessionId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.CancelAsync(
                new ApplicationStorefrontCheckoutSessionRequest(storeId.Value, checkoutSessionId, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutSessionResult session
                    ? session.ToStorefrontContract()
                    : null);
        }

        [HttpPost("{checkoutSessionId:guid}/addresses")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> UpdateAddresses(
            Guid checkoutSessionId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.UpdateAddressesAsync(
                request.ToApplicationRequest(storeId.Value, checkoutSessionId, cartToken, this.GetCurrentCustomerId()),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutSessionResult session
                    ? session.ToStorefrontContract()
                    : null);
        }

        [HttpPost("{checkoutSessionId:guid}/shipping-method")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> SelectShippingMethod(
            Guid checkoutSessionId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCheckoutShippingMethodRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.SelectShippingMethodAsync(
                request.ToApplicationRequest(storeId.Value, checkoutSessionId, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutSessionResult session
                    ? session.ToStorefrontContract()
                    : null);
        }

        [HttpPost("{checkoutSessionId:guid}/payment-method")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> SelectPaymentMethod(
            Guid checkoutSessionId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCheckoutPaymentMethodRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.SelectPaymentMethodAsync(
                request.ToApplicationRequest(storeId.Value, checkoutSessionId, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutSessionResult session
                    ? session.ToStorefrontContract()
                    : null);
        }

        [HttpPost("{checkoutSessionId:guid}/review")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> Review(
            Guid checkoutSessionId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCheckoutReviewRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.ReviewAsync(
                request.ToApplicationRequest(storeId.Value, checkoutSessionId, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutReviewResult review
                    ? review.ToStorefrontContract()
                    : null);
        }

        [HttpPost("preview")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> Preview(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.PreviewAsync(
                request.ToApplicationRequest(storeId.Value, cartToken, this.GetCurrentCustomerId()),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontCheckoutPreviewResult preview
                    ? preview.ToStorefrontContract()
                    : null);
        }

        [HttpPost("place-order")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> PlaceOrder(
            [FromBody] StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.checkoutService.PlaceOrderAsync(
                request.ToApplicationRequest(storeId.Value),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is ApplicationStorefrontPlaceOrderResult order
                    ? order.ToStorefrontContract()
                    : null);
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

}
