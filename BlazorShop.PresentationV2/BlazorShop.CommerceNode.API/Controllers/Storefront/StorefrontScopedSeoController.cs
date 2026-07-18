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
    [Route("api/storefront/stores/{storeKey}/seo")]
    public sealed class StorefrontScopedSeoController : StorefrontApiControllerBase
    {
        private readonly ISeoRedirectResolutionService seoRedirectResolutionService;
        private readonly ISeoUrlResolver seoUrlResolver;
        private readonly IStoreSeoSettingsService seoSettingsService;

        public StorefrontScopedSeoController(
            ISeoRedirectResolutionService seoRedirectResolutionService,
            ISeoUrlResolver seoUrlResolver,
            IStoreSeoSettingsService seoSettingsService)
        {
            this.seoRedirectResolutionService = seoRedirectResolutionService;
            this.seoUrlResolver = seoUrlResolver;
            this.seoSettingsService = seoSettingsService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await this.seoSettingsService.ResolveAsync();
            return this.Success(settings, "SEO settings loaded.");
        }

        [HttpGet("redirects/resolve")]
        public async Task<IActionResult> ResolveRedirect([FromQuery] string path, CancellationToken cancellationToken)
        {
            var redirect = await this.seoRedirectResolutionService.ResolvePublicPathAsync(path);
            redirect ??= ToRedirectResolution(await this.seoUrlResolver.ResolvePublicPathAsync(path, cancellationToken));

            return redirect is null
                ? this.Failure<SeoRedirectResolutionDto>(ServiceResponseType.NotFound, "SEO redirect was not found.")
                : this.Success(redirect, "SEO redirect resolved.");
        }

        private static SeoRedirectResolutionDto? ToRedirectResolution(SeoUrlResolutionDto resolution)
        {
            return resolution.RequiresRedirect && !string.IsNullOrWhiteSpace(resolution.CanonicalPath)
                ? new SeoRedirectResolutionDto
                {
                    NewPath = resolution.CanonicalPath,
                    StatusCode = resolution.HttpStatusCode,
                }
                : null;
        }
    }

}
