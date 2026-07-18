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
    [Route("api/storefront/stores/{storeKey}/cart")]
    [Authorize]
    public sealed class StorefrontScopedCartController : StorefrontApiControllerBase
    {
        private const string CartTokenHeaderName = "X-Cart-Token";

        private readonly IStorefrontCartService storefrontCartService;
        private readonly ICommerceStoreContext storeContext;

        public StorefrontScopedCartController(
            IStorefrontCartService storefrontCartService,
            ICommerceStoreContext storeContext)
        {
            this.storefrontCartService = storefrontCartService;
            this.storeContext = storeContext;
        }

        [HttpPost("session")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> CreateSession(
            [FromBody] StorefrontCreateCartSessionRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.CreateOrResumeAsync(
                new StorefrontCartCreateOrResumeRequest(storeId.Value, request.CartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToSessionContract(request.CartToken));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.GetAsync(storeId.Value, cartToken, cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPost("lines")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> AddLine(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCartLineCreateRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.AddLineAsync(
                request.ToApplicationRequest(storeId.Value, cartToken),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPut("lines/{lineId:guid}")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> UpdateLine(
            Guid lineId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCartLineUpdateRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.UpdateLineAsync(
                request.ToApplicationRequest(storeId.Value, cartToken, lineId),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpDelete("lines/{lineId:guid}")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> RemoveLine(
            Guid lineId,
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.RemoveLineAsync(
                storeId.Value,
                cartToken,
                lineId,
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpDelete]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> Clear(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.ClearAsync(
                storeId.Value,
                cartToken,
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPost("validate")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> Validate(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] StorefrontCartValidateRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.ValidateAsync(storeId.Value, cartToken, cancellationToken);
            if (result.Success
                && result.Payload is not null
                && request.ExpectedVersion.HasValue
                && request.ExpectedVersion.Value != result.Payload.Version)
            {
                return this.Error(StatusCodes.Status409Conflict, "cart.version_stale", "Cart version is stale.");
            }

            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPost("recalculate")]
        [AllowAnonymous]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> Recalculate(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            [FromBody] Contracts.Storefront.StorefrontCartRecalculateRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.storefrontCartService.RecalculateAsync(
                new BlazorShop.Application.CommerceNode.Carts.StorefrontCartRecalculateRequest(
                    storeId.Value,
                    cartToken,
                    request.ExpectedVersion),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        [HttpPost("merge-current-customer")]
        [Authorize]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Cart)]
        public async Task<IActionResult> MergeCurrentCustomer(
            [FromHeader(Name = CartTokenHeaderName)] string cartToken,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var appUserId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(appUserId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.storefrontCartService.AttachOrMergeCurrentCustomerAsync(
                new StorefrontCartAttachCurrentCustomerRequest(
                    storeId.Value,
                    cartToken,
                    appUserId),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload?.ToStorefrontContract());
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success ? result.Payload : null;
        }
    }

}
