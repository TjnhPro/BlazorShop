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
    [Route("api/storefront/stores/{storeKey}/contact")]
    public sealed class StorefrontScopedContactController : StorefrontApiControllerBase
    {
        private readonly IStorefrontContactMessageService contactMessageService;
        private readonly ICaptchaVerifier captchaVerifier;
        private readonly IStoreSecurityPrivacySettingsService securityPrivacySettingsService;

        public StorefrontScopedContactController(
            IStorefrontContactMessageService contactMessageService,
            ICaptchaVerifier captchaVerifier,
            IStoreSecurityPrivacySettingsService securityPrivacySettingsService)
        {
            this.contactMessageService = contactMessageService;
            this.captchaVerifier = captchaVerifier;
            this.securityPrivacySettingsService = securityPrivacySettingsService;
        }

        [HttpPost]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Newsletter)]
        public async Task<IActionResult> Submit(
            [FromBody] StorefrontContactRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return this.Failure<object>(ServiceResponseType.ValidationError, "Contact request is required.");
            }

            var runtimeSettings = await this.securityPrivacySettingsService.ResolveCurrentAsync(cancellationToken);
            if (runtimeSettings.Captcha.Enabled && runtimeSettings.Captcha.Targets.Contact)
            {
                if (string.IsNullOrWhiteSpace(request.CaptchaToken))
                {
                    return this.Error(StatusCodes.Status400BadRequest, "captcha.required", "Security verification is required.");
                }

                var captcha = await this.captchaVerifier.VerifyAsync(
                    new CaptchaVerificationRequest(
                        CaptchaTargetNames.Contact,
                        request.CaptchaToken,
                        this.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        this.Request.Headers.UserAgent.ToString()),
                    cancellationToken);
                if (!captcha.Success)
                {
                    return this.Error(StatusCodes.Status400BadRequest, "captcha.failed", "Security verification failed.");
                }
            }

            var result = await this.contactMessageService.SendAsync(
                new StorefrontContactMessageRequest(
                    request.Name,
                    request.Email,
                    request.Subject,
                    request.Message),
                cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is null ? null : new StorefrontContactResponse(payload.Accepted, payload.Message));
        }
    }

}
