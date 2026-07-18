namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed class StorefrontCartItemRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public sealed class StorefrontCreateCartSessionRequest
    {
        [MaxLength(512)]
        public string? CartToken { get; set; }
    }

    public sealed class StorefrontCartLineCreateRequest
    {
        [Required]
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [MaxLength(128)]
        public string? PersonalizationHash { get; set; }

        [MaxLength(8192)]
        public string? PersonalizationJson { get; set; }

        public Guid? ArtworkAssetId { get; set; }

        [Range(1, int.MaxValue)]
        public int? ArtworkVersion { get; set; }

        [MaxLength(64)]
        public string? FulfillmentProviderKey { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [StringLength(3, MinimumLength = 3)]
        public string? CurrencyCode { get; set; }
    }

    public sealed class StorefrontCartLineUpdateRequest
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public sealed class StorefrontCartValidateRequest
    {
        [Range(1, int.MaxValue)]
        public int? ExpectedVersion { get; set; }
    }

    public sealed class StorefrontCartRecalculateRequest
    {
        [Range(1, int.MaxValue)]
        public int? ExpectedVersion { get; set; }
    }

    public sealed record StorefrontCartSessionResponse(
        Guid CartId,
        string CartToken,
        string State,
        int Version,
        DateTimeOffset ExpiresAtUtc);

    public sealed record StorefrontCartResponse(
        Guid CartId,
        string State,
        int Version,
        DateTimeOffset LastActivityAtUtc,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<StorefrontCartLineResponse> Lines,
        string CurrencyCode = "USD",
        int SummaryCount = 0,
        decimal Subtotal = 0m,
        decimal DiscountTotal = 0m,
        decimal ShippingEstimate = 0m,
        decimal TaxEstimate = 0m,
        decimal GrandTotal = 0m,
        bool CheckoutAllowed = true,
        IReadOnlyList<StorefrontCartWarningResponse>? Warnings = null,
        IReadOnlyList<StorefrontCartAdjustmentResponse>? Adjustments = null);

    public sealed record StorefrontCartLineResponse(
        Guid LineId,
        Guid ProductId,
        Guid? ProductVariantId,
        string? SelectedAttributesJson,
        string? PersonalizationHash,
        string? PersonalizationJson,
        Guid? ArtworkAssetId,
        int? ArtworkVersion,
        string? FulfillmentProviderKey,
        int Quantity,
        decimal? UnitPriceSnapshot,
        string? CurrencyCodeSnapshot,
        string? DisplayName = null,
        string? ProductSlug = null,
        string? ProductUrl = null,
        string? ImageUrl = null,
        IReadOnlyList<StorefrontCartSelectedAttributeResponse>? SelectedAttributes = null,
        decimal? UnitPrice = null,
        decimal? LineSubtotal = null,
        decimal? LineTotal = null,
        int QuantityMinimum = 1,
        int? QuantityMaximum = null,
        int QuantityStep = 1,
        IReadOnlyList<int>? AllowedQuantities = null,
        bool Purchasable = true,
        IReadOnlyList<StorefrontCartWarningResponse>? Warnings = null);

    public sealed record StorefrontCartSelectedAttributeResponse(
        string Name,
        string Value);

    public sealed record StorefrontCartWarningResponse(
        string Code,
        string Message,
        Guid? LineId,
        Guid? ProductId);

    public sealed record StorefrontCartAdjustmentResponse(
        string Code,
        string Label,
        decimal Amount,
        string CurrencyCode);

    public sealed record StorefrontCartValidationResponse(
        Guid CartId,
        int Version,
        bool IsValid,
        decimal TotalAmount,
        string CurrencyCode,
        IReadOnlyList<StorefrontCartValidationIssueResponse> Issues);

    public sealed record StorefrontCartValidationIssueResponse(
        Guid? LineId,
        Guid? ProductId,
        string Code,
        string Message);
}
