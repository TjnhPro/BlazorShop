namespace BlazorShop.CommerceNode.API.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;

    using BlazorShop.Application.Common.Results;
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
    [Route("api/storefront/stores/{storeKey}/configuration")]
    public sealed class StorefrontScopedConfigurationController : StorefrontApiControllerBase
    {
        private readonly ICommerceStoreContext storeContext;
        private readonly IPaymentMethodService paymentMethodService;
        private readonly IStoreCurrencyService currencyService;
        private readonly IStoreSeoSettingsService seoSettingsService;
        private readonly IStoreFeatureStateService featureStateService;
        private readonly IStorefrontPublicConfigurationCache publicConfigurationCache;
        private readonly IStoreSecurityPrivacySettingsService securityPrivacySettingsService;

        public StorefrontScopedConfigurationController(
            ICommerceStoreContext storeContext,
            IPaymentMethodService paymentMethodService,
            IStoreCurrencyService currencyService,
            IStoreSeoSettingsService seoSettingsService,
            IStoreFeatureStateService featureStateService,
            IStorefrontPublicConfigurationCache publicConfigurationCache,
            IStoreSecurityPrivacySettingsService securityPrivacySettingsService)
        {
            this.storeContext = storeContext;
            this.paymentMethodService = paymentMethodService;
            this.currencyService = currencyService;
            this.seoSettingsService = seoSettingsService;
            this.featureStateService = featureStateService;
            this.publicConfigurationCache = publicConfigurationCache;
            this.securityPrivacySettingsService = securityPrivacySettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Value is null)
            {
                return this.ToActionResult(storeResult);
            }

            if (this.publicConfigurationCache.TryGet<StorefrontPublicConfigurationResponse>(
                storeResult.Value.StoreKey,
                out var cachedConfiguration) && cachedConfiguration is not null)
            {
                return this.Success(cachedConfiguration, "Storefront configuration loaded.");
            }

            var storeIdResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeIdResult.Success)
            {
                return this.ToActionResult(storeIdResult);
            }

            var paymentMethods = (await this.paymentMethodService.GetPaymentMethodsAsync())
                .Select(method => method.ToStorefrontContract())
                .ToArray();
            var supportedCurrencyCodes = await this.currencyService.ResolveSupportedCurrencyCodesAsync(
                storeIdResult.Value,
                cancellationToken);
            var seoDefaults = await this.seoSettingsService.ResolveAsync(cancellationToken);
            var featureStates = await this.featureStateService.ResolveAsync(storeIdResult.Value, cancellationToken);
            var securityPrivacySettings = await this.securityPrivacySettingsService.ResolveCurrentAsync(cancellationToken);
            var configuration = storeResult.Value.ToPublicConfigurationContract(
                paymentMethods,
                seoDefaults,
                featureStates,
                securityPrivacySettings.Consent,
                securityPrivacySettings.Captcha,
                supportedCurrencyCodes);

            this.publicConfigurationCache.Set(storeResult.Value.StoreKey, configuration);

            return this.Success(
                configuration,
                "Storefront configuration loaded.");
        }

        private IActionResult ToActionResult<TPayload>(ApplicationResult<TPayload> result)
        {
            if (!result.Success)
            {
                return this.StatusCode(
                    ToStatusCode(result.Error?.Kind),
                    new CommerceNodeApiErrorResponse(
                        false,
                        ToErrorCode(result.Error?.Kind),
                        NormalizeMessage(result.Message),
                        this.HttpContext.TraceIdentifier));
            }

            return this.StatusCode(
                StatusCodes.Status500InternalServerError,
                new CommerceNodeApiErrorResponse(
                    false,
                    "store.unavailable",
                    "Storefront store could not be resolved.",
                    this.HttpContext.TraceIdentifier));
        }

        private static int ToStatusCode(ApplicationErrorKind? failure)
        {
            return failure switch
            {
                ApplicationErrorKind.Validation => StatusCodes.Status400BadRequest,
                ApplicationErrorKind.NotFound => StatusCodes.Status404NotFound,
                ApplicationErrorKind.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            };
        }

        private static string ToErrorCode(ApplicationErrorKind? failure)
        {
            return failure switch
            {
                ApplicationErrorKind.Validation => "store.validation_error",
                ApplicationErrorKind.NotFound => "store.not_found",
                ApplicationErrorKind.Conflict => "store.conflict",
                _ => "store.unavailable",
            };
        }

        private static string NormalizeMessage(string? message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? "Storefront store could not be resolved."
                : message;
        }
    }

}
