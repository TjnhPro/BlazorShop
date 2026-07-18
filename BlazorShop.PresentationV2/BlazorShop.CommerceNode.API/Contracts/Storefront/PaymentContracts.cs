namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed record StorefrontPaymentAttemptResponse(
        Guid Id,
        Guid CheckoutSessionId,
        Guid? OrderId,
        string PaymentMethodKey,
        string ProviderKey,
        string State,
        decimal Amount,
        string CurrencyCode,
        string? ProviderReference,
        string? ProviderSessionId,
        StorefrontPaymentNextActionResponse? NextAction,
        string? FailureCode,
        string? FailureMessage,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record StorefrontPaymentNextActionResponse(
        string Type,
        string? Url);

    public sealed class StorefrontPaymentCallbackRequest
    {
        public Guid? PaymentAttemptId { get; set; }

        [MaxLength(256)]
        public string? ProviderEventId { get; set; }

        [Required]
        [MaxLength(128)]
        public string EventType { get; set; } = "provider.callback";

        [MaxLength(32)]
        public string? State { get; set; }

        [MaxLength(256)]
        public string? ProviderReference { get; set; }

        [MaxLength(256)]
        public string? ProviderSessionId { get; set; }

        [MaxLength(128)]
        public string? FailureCode { get; set; }

        [MaxLength(512)]
        public string? FailureMessage { get; set; }

        [Required]
        public string PayloadJson { get; set; } = "{}";
    }

    public sealed class StorefrontPaymentWebhookRequest
    {
        public Guid? PaymentAttemptId { get; set; }

        [MaxLength(256)]
        public string? EventId { get; set; }

        [Required]
        [MaxLength(128)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? State { get; set; }

        [MaxLength(256)]
        public string? ProviderReference { get; set; }

        [MaxLength(256)]
        public string? ProviderSessionId { get; set; }

        [Required]
        public string PayloadJson { get; set; } = "{}";
    }

    public sealed record StorefrontPaymentWebhookAcceptedResponse(
        string ProviderKey,
        string? EventId,
        bool Duplicate,
        string PayloadHash,
        DateTimeOffset AcceptedAtUtc);

    public sealed record StorefrontPaymentMethodResponse(
        Guid Id,
        string Key,
        string Name,
        string? Description,
        string? ShortDisplayText,
        string? IconUrl,
        IReadOnlyList<string> SupportedCurrencyCodes,
        IReadOnlyList<string> SupportedCountryCodes);
}
