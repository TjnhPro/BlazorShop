namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public sealed class StorefrontCheckoutPreviewRequest
    {
        public int ExpectedCartVersion { get; set; }

        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string PaymentMethodKey { get; set; } = string.Empty;

        public Guid? ShippingAddressId { get; set; }

        public Guid? BillingAddressId { get; set; }

        public bool UseShippingAddressAsBillingAddress { get; set; } = true;

        public StorefrontCheckoutPreviewShippingAddress? ShippingAddress { get; set; } = new();
    }

    public sealed class StorefrontCheckoutStartRequest
    {
    }

    public sealed class StorefrontCheckoutPreviewShippingAddress
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string Address1 { get; set; } = string.Empty;

        public string? Address2 { get; set; }

        public string City { get; set; } = string.Empty;

        public string? State { get; set; }

        public string PostalCode { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;
    }

    public sealed record StorefrontCheckoutPreviewResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CartVersion,
        string State,
        bool IsValid,
        string NextAction,
        string CustomerEmail,
        string CustomerName,
        string PaymentMethodKey,
        decimal Subtotal,
        decimal ShippingTotal,
        decimal TaxTotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        string CurrencyCode,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCheckoutLineSummaryResponse> Lines,
        IReadOnlyList<StorefrontCheckoutValidationIssueResponse> Issues);

    public sealed record StorefrontCheckoutLineSummaryResponse(
        Guid LineId,
        Guid ProductId,
        Guid? ProductVariantId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        string CurrencyCode);

    public sealed record StorefrontCheckoutValidationIssueResponse(
        string Code,
        string Message,
        string? Field,
        Guid? LineId,
        Guid? ProductId);

    public sealed class StorefrontCheckoutAddressStepRequest
    {
        public Guid? BillingAddressId { get; set; }

        public Guid? ShippingAddressId { get; set; }

        public bool UseBillingAddressAsShippingAddress { get; set; }

        public StorefrontCheckoutPreviewShippingAddress? BillingAddress { get; set; }

        public StorefrontCheckoutPreviewShippingAddress? ShippingAddress { get; set; }
    }

    public sealed class StorefrontCheckoutShippingMethodRequest
    {
        public string ShippingOptionKey { get; set; } = string.Empty;
    }

    public sealed class StorefrontCheckoutPaymentMethodRequest
    {
        public string PaymentMethodKey { get; set; } = string.Empty;
    }

    public sealed class StorefrontCheckoutReviewRequest
    {
        public bool TermsAccepted { get; set; }

        public string? TermsVersion { get; set; }
    }

    public sealed record StorefrontCheckoutShippingOptionResponse(
        string Key,
        string DisplayName,
        string? Description,
        decimal Price,
        string CurrencyCode,
        string? DeliveryEstimateText,
        bool Selected);

    public sealed record StorefrontCheckoutPaymentMethodOptionResponse(
        string Key,
        string DisplayName,
        string? Description,
        string? ShortDisplayText,
        string? IconUrl,
        string ProviderKey,
        string NextActionKind,
        bool Selected);

    public sealed record StorefrontCheckoutSessionResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CheckoutVersion,
        int CartVersion,
        int LastValidatedCartVersion,
        string State,
        string CurrentStep,
        IReadOnlyList<string> CompletedSteps,
        bool IsActive,
        string NextAction,
        string CustomerEmail,
        string CustomerName,
        string PaymentMethodKey,
        decimal Subtotal,
        decimal ShippingTotal,
        decimal TaxTotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        string CurrencyCode,
        DateTimeOffset ExpiresAtUtc,
        bool ShippingRequired,
        StorefrontCheckoutShippingOptionResponse? SelectedShippingOption,
        IReadOnlyList<StorefrontCheckoutShippingOptionResponse> ShippingOptions,
        StorefrontCheckoutPaymentMethodOptionResponse? SelectedPaymentMethod,
        IReadOnlyList<StorefrontCheckoutPaymentMethodOptionResponse> PaymentMethods,
        IReadOnlyList<StorefrontCheckoutLineSummaryResponse> Lines,
        IReadOnlyList<StorefrontCheckoutValidationIssueResponse> Issues);

    public sealed record StorefrontCheckoutReviewResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CheckoutVersion,
        int CartVersion,
        int LastValidatedCartVersion,
        string State,
        string CurrentStep,
        IReadOnlyList<string> CompletedSteps,
        bool IsActive,
        string NextAction,
        string CustomerEmail,
        string CustomerName,
        StorefrontCheckoutPreviewShippingAddress? BillingAddress,
        StorefrontCheckoutPreviewShippingAddress? ShippingAddress,
        StorefrontCheckoutShippingOptionResponse? SelectedShippingOption,
        StorefrontCheckoutPaymentMethodOptionResponse? SelectedPaymentMethod,
        IReadOnlyList<StorefrontCheckoutLineSummaryResponse> Lines,
        decimal Subtotal,
        decimal ShippingTotal,
        decimal TaxTotal,
        decimal DiscountTotal,
        decimal GrandTotal,
        string CurrencyCode,
        bool TermsRequired,
        bool TermsAccepted,
        string? TermsVersion,
        DateTimeOffset? TermsAcceptedAtUtc,
        bool PlaceOrderAllowed,
        string NextRequiredStep,
        IReadOnlyList<StorefrontCheckoutValidationIssueResponse> Issues);

    public sealed class StorefrontPlaceOrderRequest
    {
        public Guid CheckoutSessionId { get; set; }

        public int ExpectedCheckoutVersion { get; set; }

        public int ExpectedCartVersion { get; set; }

        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public sealed record StorefrontPlaceOrderResponse(
        Guid CheckoutSessionId,
        Guid PaymentAttemptId,
        Guid? OrderId,
        string? Reference,
        string? OrderStatus,
        string PaymentStatus,
        string PaymentMethodKey,
        decimal TotalAmount,
        string CurrencyCode,
        string IdempotencyKey,
        DateTime CreatedOn,
        string? GuestAccessToken,
        StorefrontPaymentNextActionResponse? NextAction);
}
