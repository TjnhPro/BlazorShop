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
    [Route("api/storefront/stores/{storeKey}/customer/addresses")]
    [Authorize]
    public sealed class StorefrontScopedCustomerAddressesController : StorefrontApiControllerBase
    {
        private readonly IStorefrontCustomerAddressService addressService;
        private readonly ICommerceStoreContext storeContext;

        public StorefrontScopedCustomerAddressesController(
            IStorefrontCustomerAddressService addressService,
            ICommerceStoreContext storeContext)
        {
            this.addressService = addressService;
            this.storeContext = storeContext;
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var context = await this.CreateAddressContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            var result = await this.addressService.ListAsync(context.Context!, cancellationToken);
            return this.FromServiceResponse(
                result,
                addresses => addresses?.Select(address => address.ToStorefrontContract()).ToArray());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StorefrontCustomerAddressRequest request, CancellationToken cancellationToken)
        {
            var context = await this.CreateAddressContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            var result = await this.addressService.CreateAsync(context.Context!, request.ToApplicationRequest(), cancellationToken);
            return this.FromServiceResponse(result, address => address?.ToStorefrontContract());
        }

        [HttpPut("{addressId:guid}")]
        public async Task<IActionResult> Update(Guid addressId, [FromBody] StorefrontCustomerAddressRequest request, CancellationToken cancellationToken)
        {
            var context = await this.CreateAddressContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            var result = await this.addressService.UpdateAsync(context.Context!, addressId, request.ToApplicationRequest(), cancellationToken);
            return this.FromServiceResponse(result, address => address?.ToStorefrontContract());
        }

        [HttpDelete("{addressId:guid}")]
        public async Task<IActionResult> Delete(Guid addressId, CancellationToken cancellationToken)
        {
            var context = await this.CreateAddressContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            var result = await this.addressService.DeleteAsync(context.Context!, addressId, cancellationToken);
            if (result.Success)
            {
                return this.FromServiceResponse(result);
            }

            return result.Payload is ServiceResponseType responseType
                ? this.Failure<object>(responseType, result.Message ?? "Customer address could not be deleted.")
                : this.FromServiceResponse(result);
        }

        [HttpPost("{addressId:guid}/default-shipping")]
        public async Task<IActionResult> SetDefaultShipping(Guid addressId, CancellationToken cancellationToken)
        {
            var context = await this.CreateAddressContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            var result = await this.addressService.SetDefaultShippingAsync(context.Context!, addressId, cancellationToken);
            return this.FromServiceResponse(result, address => address?.ToStorefrontContract());
        }

        [HttpPost("{addressId:guid}/default-billing")]
        public async Task<IActionResult> SetDefaultBilling(Guid addressId, CancellationToken cancellationToken)
        {
            var context = await this.CreateAddressContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            var result = await this.addressService.SetDefaultBillingAsync(context.Context!, addressId, cancellationToken);
            return this.FromServiceResponse(result, address => address?.ToStorefrontContract());
        }

        private async Task<(StorefrontCustomerAddressContext? Context, IActionResult? Result)> CreateAddressContextAsync(
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return (null, this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved."));
            }

            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (null, this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found."));
            }

            var email = this.User.FindFirst(ClaimTypes.Email)?.Value ?? this.User.FindFirst("email")?.Value;
            var fullName = this.User.FindFirst(ClaimTypes.Name)?.Value ?? this.User.Identity?.Name;
            return (new StorefrontCustomerAddressContext(storeId.Value, userId, email, fullName), null);
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success ? result.Payload : null;
        }
    }

}
