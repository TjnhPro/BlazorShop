namespace BlazorShop.Storefront.Services
{
    public sealed record StorefrontProductFilterMetadataResponse(
        IReadOnlyList<int> PageSizes,
        IReadOnlyList<StorefrontProductSortOptionResponse> SortOptions,
        IReadOnlyList<StorefrontFilterFacetResponse> Facets,
        StorefrontPriceFacetResponse PriceRange,
        int MinimumSearchTermLength);

    public sealed record StorefrontFilterFacetResponse(
        string Key,
        string Label,
        string Type,
        int DisplayOrder,
        int? MaxChoices,
        int MinimumHitCount,
        IReadOnlyList<StorefrontFilterChoiceResponse> Choices);

    public sealed record StorefrontFilterChoiceResponse(
        string Value,
        string Label,
        int DisplayOrder,
        int? HitCount,
        bool Selected);

    public sealed record StorefrontPriceFacetResponse(
        decimal? MinPrice,
        decimal? MaxPrice,
        string? CurrencyCode,
        int DisplayOrder);

    public sealed record StorefrontProductSortOptionResponse(
        string Value,
        string Label,
        int DisplayOrder);

    public sealed record StorefrontSearchSuggestionResponse(
        string? SearchTerm,
        int MinimumSearchTermLength,
        int Limit,
        IReadOnlyList<StorefrontSearchSuggestionItemResponse> Items);

    public sealed record StorefrontSearchSuggestionItemResponse(
        Guid Id,
        string Slug,
        string Name,
        string? Sku,
        string? Image,
        Guid? PrimaryMediaPublicId,
        bool HasPrimaryMedia,
        decimal Price,
        decimal? DisplayPrice,
        string? DisplayCurrencyCode,
        string? CategoryName,
        string? CategorySlug,
        bool InStock,
        string Url);

    public sealed class StorefrontProductSelectionPreviewRequest
    {
        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<StorefrontSelectedAttribute>? SelectedAttributes { get; set; }

        public int Quantity { get; set; } = 1;

        public string? CurrencyCode { get; set; }
    }

    public sealed record StorefrontProductSelectionPreviewResponse(
        Guid ProductId,
        Guid? ProductVariantId,
        bool IsValid,
        bool IsAvailable,
        bool CanAddToCart,
        IReadOnlyList<string> ValidationMessages,
        IReadOnlyList<StorefrontProductSelectionAttribute> SelectedAttributes,
        string? AttributeSignature,
        string? Sku,
        string? DisplayName,
        decimal UnitPrice,
        decimal? ComparePrice,
        string CurrencyCode,
        int StockQuantity,
        int MinQuantity,
        int MaxQuantity,
        string? PrimaryImageUrl);

    public sealed record StorefrontProductSelectionAttribute(string Name, string Value);
}
