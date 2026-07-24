namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    public sealed class StorefrontLocalCartLineRequest
    {
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public string? CurrencyCode { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public sealed class StorefrontLocalProductSelectionPreviewRequest
    {
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        public int Quantity { get; set; } = 1;

        public string? CurrencyCode { get; set; }
    }

    public sealed record StorefrontLocalProductSelectionPreviewResponse(
        Guid ProductId,
        Guid? ProductVariantId,
        bool IsValid,
        bool IsAvailable,
        bool CanAddToCart,
        IReadOnlyList<string> ValidationMessages,
        IReadOnlyList<SelectedAttributeDto> SelectedAttributes,
        string? AttributeSignature,
        string? Sku,
        string? DisplayName,
        decimal UnitPrice,
        decimal? ComparePrice,
        string CurrencyCode,
        string FormattedUnitPrice,
        string? FormattedComparePrice,
        int StockQuantity,
        int MinQuantity,
        int MaxQuantity,
        string? PrimaryImageUrl);

    public sealed class StorefrontLocalCartQuantityRequest
    {
        public int Quantity { get; set; }
    }
}
