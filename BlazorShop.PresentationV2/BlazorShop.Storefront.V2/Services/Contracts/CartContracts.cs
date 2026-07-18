namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models.Discovery;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Web.SharedV2.Models.Category;
    using BlazorShop.Web.SharedV2.Models.Pages;
    using BlazorShop.Web.SharedV2.Models.Product;
    using BlazorShop.Web.SharedV2.Models.Seo;

    using Microsoft.Extensions.Options;

    using GetCategoryTreeNode = BlazorShop.Application.DTOs.Category.GetCategoryTreeNode;

    public sealed class StorefrontCreateCartSessionRequest
    {
        public string? CartToken { get; set; }
    }

    public sealed class StorefrontCartLineCreateRequest
    {
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public string? CurrencyCode { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public sealed class StorefrontCartLineUpdateRequest
    {
        public int Quantity { get; set; }
    }

    public sealed class StorefrontCartRecalculateRequest
    {
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
}
