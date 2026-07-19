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
    [Route("api/storefront/stores/{storeKey}/payments")]
    public sealed class StorefrontScopedPaymentsController : StorefrontApiControllerBase
    {
        private readonly IPaymentAttemptService paymentAttemptService;
        private readonly IPaymentWebhookSignatureVerifier paymentWebhookSignatureVerifier;
        private readonly IStorefrontPaymentProviderResolver paymentProviderResolver;
        private readonly IPaymentMethodService paymentMethodService;

        public StorefrontScopedPaymentsController(
            ICommerceStoreContext storeContext,
            IPaymentAttemptService paymentAttemptService,
            IPaymentWebhookSignatureVerifier paymentWebhookSignatureVerifier,
            IStorefrontPaymentProviderResolver paymentProviderResolver,
            IPaymentMethodService paymentMethodService)
            : base(storeContext)
        {
            this.paymentAttemptService = paymentAttemptService;
            this.paymentWebhookSignatureVerifier = paymentWebhookSignatureVerifier;
            this.paymentProviderResolver = paymentProviderResolver;
            this.paymentMethodService = paymentMethodService;
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            var paymentMethods = (await this.paymentMethodService.GetPaymentMethodsAsync()).ToArray();
            return paymentMethods.Length == 0
                ? this.Failure<IReadOnlyList<StorefrontPaymentMethodResponse>>(
                    ServiceResponseType.NotFound,
                    "No payment methods are currently available.",
                    [])
                : this.Success(
                    paymentMethods.Select(method => method.ToStorefrontContract()).ToArray(),
                    "Payment methods loaded.");
        }

        [HttpGet("attempts/{attemptId:guid}")]
        public async Task<IActionResult> GetAttempt(Guid attemptId, CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var result = await this.paymentAttemptService.GetAsync(storeId.Value, attemptId, cancellationToken);
            return this.FromServiceResponse(
                result,
                payload => payload is PaymentAttemptDto attempt
                    ? attempt.ToStorefrontContract()
                    : null);
        }

        [HttpPost("provider-callback/{providerKey}")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> HandleProviderCallback(
            string providerKey,
            [FromBody] StorefrontPaymentCallbackRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var eventResult = await this.paymentAttemptService.RecordProviderEventAsync(
                new RecordPaymentProviderEventRequest(
                    storeId.Value,
                    request.PaymentAttemptId,
                    providerKey,
                    request.ProviderEventId,
                    request.EventType,
                    request.PayloadJson,
                    request.ProviderReference,
                    request.ProviderSessionId,
                    ProcessedAtUtc: DateTimeOffset.UtcNow),
                cancellationToken);
            if (!eventResult.Success || eventResult.Payload is null)
            {
                return this.FromServiceResponse(eventResult);
            }

            var operationResult = await this.ApplyProviderCallbackOperationAsync(
                storeId.Value,
                providerKey,
                request,
                cancellationToken);
            if (operationResult is not null)
            {
                return operationResult;
            }

            return this.Success(
                new StorefrontPaymentWebhookAcceptedResponse(
                    providerKey,
                    request.ProviderEventId,
                    eventResult.Payload.IsDuplicate,
                    eventResult.Payload.PayloadHash,
                    eventResult.Payload.CreatedAtUtc),
                "Payment provider callback accepted.");
        }

        [HttpPost("webhooks/{providerKey}")]
        [EnableRateLimiting(StorefrontRateLimitPolicyNames.Checkout)]
        public async Task<IActionResult> HandleWebhook(
            string providerKey,
            [FromHeader(Name = "X-Provider-Signature")] string? providerSignature,
            [FromBody] StorefrontPaymentWebhookRequest request,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return this.Error(StatusCodes.Status404NotFound, "store.not_found", "Storefront store could not be resolved.");
            }

            var signatureResult = await this.paymentWebhookSignatureVerifier.VerifyAsync(
                providerKey,
                request.PayloadJson,
                providerSignature,
                cancellationToken);
            if (!signatureResult.Success)
            {
                return this.FromServiceResponse(signatureResult);
            }

            var eventResult = await this.paymentAttemptService.RecordProviderEventAsync(
                new RecordPaymentProviderEventRequest(
                    storeId.Value,
                    request.PaymentAttemptId,
                    providerKey,
                    request.EventId,
                    request.EventType,
                    request.PayloadJson,
                    request.ProviderReference,
                    request.ProviderSessionId,
                    ProcessedAtUtc: DateTimeOffset.UtcNow),
                cancellationToken);
            if (!eventResult.Success || eventResult.Payload is null)
            {
                return this.FromServiceResponse(eventResult);
            }

            var operationResult = await this.ApplyProviderWebhookOperationAsync(
                storeId.Value,
                providerKey,
                providerSignature,
                request,
                cancellationToken);
            if (operationResult is not null)
            {
                return operationResult;
            }

            return this.Success(
                new StorefrontPaymentWebhookAcceptedResponse(
                    providerKey,
                    request.EventId,
                    eventResult.Payload.IsDuplicate,
                    eventResult.Payload.PayloadHash,
                    eventResult.Payload.CreatedAtUtc),
                "Payment webhook accepted.");
        }

        private async Task<IActionResult?> ApplyProviderCallbackOperationAsync(
            Guid storeId,
            string providerKey,
            StorefrontPaymentCallbackRequest request,
            CancellationToken cancellationToken)
        {
            ServiceResponse<PaymentProviderOperationResult>? providerResult = null;
            try
            {
                var provider = this.paymentProviderResolver.Resolve(providerKey);
                var operationRequest = new PaymentProviderOperationRequest(
                    storeId,
                    CheckoutSessionId: Guid.Empty,
                    request.PaymentAttemptId ?? Guid.Empty,
                    PaymentMethodKey: providerKey,
                    ProviderKey: providerKey,
                    Amount: 0m,
                    CurrencyCode: "USD",
                    IdempotencyKey: request.ProviderEventId ?? Guid.NewGuid().ToString("N"),
                    ProviderReference: request.ProviderReference,
                    ProviderSessionId: request.ProviderSessionId,
                    PayloadJson: request.PayloadJson);

                providerResult = request.EventType.Contains("cancel", StringComparison.OrdinalIgnoreCase)
                    ? await provider.HandleCancelAsync(operationRequest, cancellationToken)
                    : await provider.HandleReturnAsync(operationRequest, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            return await this.ApplyProviderOperationTransitionAsync(storeId, request.PaymentAttemptId, request.PayloadJson, providerResult, cancellationToken);
        }

        private async Task<IActionResult?> ApplyProviderWebhookOperationAsync(
            Guid storeId,
            string providerKey,
            string? providerSignature,
            StorefrontPaymentWebhookRequest request,
            CancellationToken cancellationToken)
        {
            ServiceResponse<PaymentProviderOperationResult>? providerResult = null;
            try
            {
                var provider = this.paymentProviderResolver.Resolve(providerKey);
                providerResult = await provider.HandleWebhookAsync(
                    new PaymentProviderOperationRequest(
                        storeId,
                        CheckoutSessionId: Guid.Empty,
                        request.PaymentAttemptId ?? Guid.Empty,
                        PaymentMethodKey: providerKey,
                        ProviderKey: providerKey,
                        Amount: 0m,
                        CurrencyCode: "USD",
                        IdempotencyKey: request.EventId ?? Guid.NewGuid().ToString("N"),
                        ProviderReference: request.ProviderReference,
                        ProviderSessionId: request.ProviderSessionId,
                        PayloadJson: request.PayloadJson,
                        ProviderSignature: providerSignature),
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            return await this.ApplyProviderOperationTransitionAsync(storeId, request.PaymentAttemptId, request.PayloadJson, providerResult, cancellationToken);
        }

        private async Task<IActionResult?> ApplyProviderOperationTransitionAsync(
            Guid storeId,
            Guid? paymentAttemptId,
            string payloadJson,
            ServiceResponse<PaymentProviderOperationResult>? providerResult,
            CancellationToken cancellationToken)
        {
            if (providerResult is null)
            {
                return null;
            }

            if (!providerResult.Success)
            {
                return string.Equals(providerResult.Payload?.SafeFailureCode, "payment.operation_not_supported", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : this.FromServiceResponse(providerResult);
            }

            if (!paymentAttemptId.HasValue || string.IsNullOrWhiteSpace(providerResult.Payload?.RecommendedState))
            {
                return null;
            }

            var transition = await this.paymentAttemptService.TransitionAsync(
                new TransitionPaymentAttemptRequest(
                    storeId,
                    paymentAttemptId.Value,
                    providerResult.Payload.RecommendedState!,
                    providerResult.Payload.ProviderReference,
                    providerResult.Payload.ProviderSessionId,
                    FailureCode: providerResult.Payload.SafeFailureCode,
                    FailureMessage: providerResult.Payload.SafeFailureMessage,
                    MetadataJson: providerResult.Payload.MetadataJson ?? payloadJson),
                cancellationToken);

            return transition.Success ? null : this.FromServiceResponse(transition);
        }

    }

}
