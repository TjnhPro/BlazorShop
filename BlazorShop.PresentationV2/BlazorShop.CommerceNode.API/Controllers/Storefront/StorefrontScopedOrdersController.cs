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
    [Route("api/storefront/stores/{storeKey}/orders")]
    [Authorize]
    public sealed class StorefrontScopedOrdersController : StorefrontApiControllerBase
    {
        private readonly IStorefrontGuestOrderService guestOrderService;
        private readonly IStorefrontCustomerOrderService customerOrderService;

        public StorefrontScopedOrdersController(
            IStorefrontGuestOrderService guestOrderService,
            IStorefrontCustomerOrderService customerOrderService)
        {
            this.guestOrderService = guestOrderService;
            this.customerOrderService = customerOrderService;
        }

        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUserOrders(
            [FromQuery] StorefrontCustomerOrderListQuery query,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.customerOrderService.ListAsync(
                new StorefrontCustomerOrderQuery(userId, query.PageNumber, query.PageSize),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                page => page!.ToStorefrontContract(item => item.ToCustomerOrderListItemContract()));
        }

        [HttpGet("current-user/{orderReference}")]
        public async Task<IActionResult> GetCurrentUserOrder(
            string orderReference,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.customerOrderService.GetAsync(
                new StorefrontCustomerOrderLookupRequest(userId, orderReference),
                cancellationToken);
            return this.FromServiceResponse(result, order => order?.ToCustomerOrderDetailContract(receiptMode: false));
        }

        [HttpGet("current-user/{orderReference}/receipt")]
        public async Task<IActionResult> GetCurrentUserOrderReceipt(
            string orderReference,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentCustomerId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found.");
            }

            var result = await this.customerOrderService.GetReceiptAsync(
                new StorefrontCustomerOrderLookupRequest(userId, orderReference),
                cancellationToken);
            return this.FromServiceResponse(result, order => order?.ToCustomerOrderDetailContract(receiptMode: true));
        }

        [AllowAnonymous]
        [HttpPost("guest-lookup")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> GetGuestOrder(
            [FromBody] Contracts.Storefront.StorefrontGuestOrderLookupRequest request,
            CancellationToken cancellationToken)
        {
            var result = await this.guestOrderService.GetAsync(request.ToApplicationRequest(), cancellationToken);
            return this.FromServiceResponse(result, order => order!.ToCustomerOrderDetailContract(receiptMode: true));
        }

        private string? GetCurrentCustomerId()
        {
            return this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }

}
