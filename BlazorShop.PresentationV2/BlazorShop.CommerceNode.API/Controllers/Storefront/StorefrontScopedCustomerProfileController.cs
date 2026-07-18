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
    [Route("api/storefront/stores/{storeKey}/customer/profile")]
    [Authorize]
    public sealed class StorefrontScopedCustomerProfileController : StorefrontApiControllerBase
    {
        private readonly IStorefrontCustomerService customerService;
        private readonly IAuthenticationService authenticationService;
        private readonly ICommerceStoreContext storeContext;

        public StorefrontScopedCustomerProfileController(
            IStorefrontCustomerService customerService,
            IAuthenticationService authenticationService,
            ICommerceStoreContext storeContext)
        {
            this.customerService = customerService;
            this.authenticationService = authenticationService;
            this.storeContext = storeContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var context = await this.CreateProfileContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            var result = await this.customerService.GetOrCreateAuthenticatedProfileAsync(
                new StorefrontAuthenticatedCustomerProfileRequest(
                    context.StoreId,
                    context.AppUserId!,
                    context.Email!,
                    context.FullName),
                cancellationToken);

            return this.FromServiceResponse(result, profile => profile?.ToStorefrontContract());
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] BlazorShop.CommerceNode.API.Contracts.Storefront.StorefrontCustomerProfileUpdateRequest request,
            CancellationToken cancellationToken)
        {
            var context = await this.CreateProfileContextAsync(cancellationToken);
            if (context.Result is not null)
            {
                return context.Result;
            }

            if (!string.Equals(NormalizeEmail(request.Email), NormalizeEmail(context.Email), StringComparison.Ordinal))
            {
                return this.Error(
                    StatusCodes.Status400BadRequest,
                    "profile.email_change_unsupported",
                    "Email change requires a confirmation flow and is not supported by this endpoint.");
            }

            var identityResult = await this.authenticationService.UpdateProfile(
                context.AppUserId!,
                new UpdateProfile
                {
                    FullName = request.FullName,
                    Email = context.Email!,
                    PhoneNumber = request.PhoneNumber,
                });
            if (!identityResult.Success)
            {
                return this.FromServiceResponse(identityResult);
            }

            var result = await this.customerService.UpdateAuthenticatedProfileAsync(
                request.ToApplicationRequest(context.StoreId, context.AppUserId!),
                cancellationToken);

            return this.FromServiceResponse(result, profile => profile?.ToStorefrontContract());
        }

        private async Task<(Guid StoreId, string? AppUserId, string? Email, string? FullName, IActionResult? Result)> CreateProfileContextAsync(
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return (Guid.Empty, null, null, null, this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved."));
            }

            var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (Guid.Empty, null, null, null, this.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Customer identity was not found."));
            }

            var email = this.User.FindFirst(ClaimTypes.Email)?.Value ?? this.User.FindFirst("email")?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return (Guid.Empty, null, null, null, this.Error(StatusCodes.Status400BadRequest, "profile.email_missing", "Customer email claim is required."));
            }

            var fullName = this.User.FindFirst(ClaimTypes.Name)?.Value ?? this.User.Identity?.Name;
            return (storeResult.Payload, userId, email, fullName, null);
        }

        private static string? NormalizeEmail(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }
    }

}
