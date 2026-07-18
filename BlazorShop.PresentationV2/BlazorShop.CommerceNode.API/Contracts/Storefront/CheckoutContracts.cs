namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed class StorefrontCheckoutPreviewRequest
    {
        [Range(1, int.MaxValue)]
        public int ExpectedCartVersion { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string PaymentMethodKey { get; set; } = string.Empty;

        public Guid? ShippingAddressId { get; set; }

        public Guid? BillingAddressId { get; set; }

        public bool UseShippingAddressAsBillingAddress { get; set; } = true;

        public StorefrontCheckoutShippingAddress? ShippingAddress { get; set; } = new();
    }

    public sealed record StorefrontCheckoutPreviewResponse(
        Guid CheckoutSessionId,
        Guid CartId,
        int CheckoutVersion,
        int CartVersion,
        int LastValidatedCartVersion,
        string State,
        string CurrentStep,
        IReadOnlyList<string> CompletedSteps,
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

    public sealed class StorefrontCheckoutStartRequest
    {
    }

    public sealed class StorefrontCheckoutAddressStepRequest
    {
        public Guid? BillingAddressId { get; set; }

        public Guid? ShippingAddressId { get; set; }

        public bool UseBillingAddressAsShippingAddress { get; set; }

        public StorefrontCheckoutShippingAddress? BillingAddress { get; set; }

        public StorefrontCheckoutShippingAddress? ShippingAddress { get; set; }
    }

    public sealed class StorefrontCheckoutShippingMethodRequest
    {
        [Required]
        [MaxLength(64)]
        public string ShippingOptionKey { get; set; } = string.Empty;
    }

    public sealed record StorefrontCheckoutShippingOptionResponse(
        string Key,
        string ProviderSystemName,
        string MethodCode,
        string DisplayName,
        string? Description,
        decimal Price,
        string CurrencyCode,
        string? DeliveryEstimateText,
        bool Selected);

    public sealed class StorefrontCheckoutPaymentMethodRequest
    {
        [Required]
        [MaxLength(64)]
        public string PaymentMethodKey { get; set; } = string.Empty;
    }

    public sealed class StorefrontCheckoutReviewRequest
    {
        public bool TermsAccepted { get; set; }

        [MaxLength(64)]
        public string? TermsVersion { get; set; }
    }

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
        StorefrontCheckoutShippingAddress? BillingAddress,
        StorefrontCheckoutShippingAddress? ShippingAddress,
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
        [Required]
        public Guid CheckoutSessionId { get; set; }

        [Range(1, int.MaxValue)]
        public int ExpectedCheckoutVersion { get; set; }

        [Range(1, int.MaxValue)]
        public int ExpectedCartVersion { get; set; }

        [Required]
        [MaxLength(128)]
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
}
