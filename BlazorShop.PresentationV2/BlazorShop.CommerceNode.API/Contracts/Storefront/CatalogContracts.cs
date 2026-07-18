namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed class StorefrontProductCatalogQuery
    {
        [Range(1, int.MaxValue)]
        public int PageNumber { get; init; } = 1;

        [Range(1, StorefrontContractValidation.MaxPageSize)]
        public int PageSize { get; init; } = StorefrontContractValidation.DefaultPageSize;

        public Guid? CategoryId { get; init; }

        [MaxLength(256)]
        public string? CategorySlug { get; init; }

        public bool IncludeSubcategories { get; init; }

        [MaxLength(256)]
        public string? SearchTerm { get; init; }

        [Range(0, double.MaxValue)]
        public decimal? MinPrice { get; init; }

        [Range(0, double.MaxValue)]
        public decimal? MaxPrice { get; init; }

        public bool? InStock { get; init; }

        [RegularExpression(StorefrontContractValidation.SortByPattern)]
        public string SortBy { get; init; } = StorefrontProductCatalogSortValues.Newest;

        public DateTime? CreatedAfterUtc { get; init; }

        [StringLength(3, MinimumLength = 3)]
        public string? CurrencyCode { get; init; }
    }

    public sealed class StorefrontProductFilterMetadataQuery
    {
        [MaxLength(256)]
        public string? CategorySlug { get; init; }

        [MaxLength(256)]
        public string? SearchTerm { get; init; }

        [StringLength(3, MinimumLength = 3)]
        public string? CurrencyCode { get; init; }
    }

    public sealed class StorefrontSearchSuggestionQuery
    {
        [MaxLength(256)]
        public string? SearchTerm { get; init; }

        [MaxLength(256)]
        public string? CategorySlug { get; init; }

        [Range(1, CatalogSearchPolicy.SuggestionMaxLimit)]
        public int? Limit { get; init; }

        [StringLength(3, MinimumLength = 3)]
        public string? CurrencyCode { get; init; }
    }

    public sealed record StorefrontProductFilterMetadataResponse(
        [property: Required]
        IReadOnlyList<int> PageSizes,
        [property: Required]
        IReadOnlyList<StorefrontProductSortOptionResponse> SortOptions,
        [property: Required]
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
        [property: Required]
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
        [property: Required]
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

    public static class StorefrontProductCatalogSortValues
    {
        public const string Newest = "newest";
        public const string Oldest = "oldest";
        public const string PriceLowToHigh = "priceLowToHigh";
        public const string PriceHighToLow = "priceHighToLow";
        public const string NameAscending = "nameAscending";
        public const string NameDescending = "nameDescending";
        public const string DisplayOrder = "displayOrder";
        public const string Updated = "updated";

        public static IReadOnlyList<string> All { get; } =
        [
            Newest,
            Oldest,
            PriceLowToHigh,
            PriceHighToLow,
            NameAscending,
            NameDescending,
            DisplayOrder,
            Updated,
        ];
    }

    public sealed record StorefrontCategoryResponse(
        Guid Id,
        Guid? ParentCategoryId,
        string? Name,
        string? Description,
        string? Slug,
        string? Image,
        int DisplayOrder,
        DateTime? UpdatedAt = null,
        string? MetaTitle = null,
        string? MetaDescription = null,
        string? CanonicalUrl = null,
        string? OgTitle = null,
        string? OgDescription = null,
        string? OgImage = null,
        string? SeoContent = null,
        bool RobotsIndex = true,
        bool RobotsFollow = true);

    public sealed record StorefrontCategoryTreeNodeResponse(
        Guid Id,
        Guid? ParentCategoryId,
        string? Name,
        string? Slug,
        string? Image,
        int DisplayOrder,
        IReadOnlyList<StorefrontCategoryTreeNodeResponse> Children);

    public sealed record StorefrontCategoryPageResponse(
        StorefrontCategoryResponse Category,
        [property: Required]
        IReadOnlyList<StorefrontCategoryBreadcrumbItemResponse> Breadcrumbs,
        [property: Required]
        IReadOnlyList<StorefrontCatalogProductResponse> Products,
        int DirectProductCount,
        int DescendantProductCount);

    public sealed record StorefrontCategoryBreadcrumbItemResponse(
        Guid Id,
        string? Name,
        string? Slug);

    public sealed record StorefrontCatalogProductResponse(
        Guid Id,
        string? Slug,
        string? Name,
        string? Description,
        string? Sku,
        string? ShortDescription,
        decimal Price,
        decimal? ComparePrice,
        string? Image,
        Guid? PrimaryMediaPublicId,
        bool HasPrimaryMedia,
        int Quantity,
        DateTime CreatedOn,
        DateTime UpdatedAt,
        int DisplayOrder,
        bool InStock,
        bool Purchasable,
        IReadOnlyList<string> PurchaseBlockReasons,
        string StockStatus,
        int? AvailableQuantity,
        int MinOrderQuantity,
        int? MaxOrderQuantity,
        int QuantityStep,
        bool ManageStock,
        bool ShippingRequired,
        bool FreeShipping,
        string? DeliveryEstimateText,
        DateTime? PublishedOn,
        Guid? CategoryId,
        string? CategoryName,
        string? CategorySlug,
        bool HasVariants,
        string ProductType,
        Guid? VariationTemplateId,
        decimal? DisplayPrice = null,
        decimal? DisplayComparePrice = null,
        string? DisplayCurrencyCode = null);

    public sealed record StorefrontProductResponse(
        Guid Id,
        string? Slug,
        string? Name,
        string? Description,
        string? Sku,
        string? ShortDescription,
        string? FullDescription,
        decimal Price,
        decimal? ComparePrice,
        decimal? Weight,
        decimal? Length,
        decimal? Width,
        decimal? Height,
        string? Image,
        int Quantity,
        bool Purchasable,
        IReadOnlyList<string> PurchaseBlockReasons,
        string StockStatus,
        int? AvailableQuantity,
        int MinOrderQuantity,
        int? MaxOrderQuantity,
        int QuantityStep,
        bool ManageStock,
        bool ShippingRequired,
        bool FreeShipping,
        string? DeliveryEstimateText,
        int DisplayOrder,
        bool InStock,
        DateTime? PublishedOn,
        string ProductType,
        Guid? VariationTemplateId,
        Guid? CategoryId,
        string? MetaTitle,
        string? MetaDescription,
        string? CanonicalUrl,
        string? OgTitle,
        string? OgDescription,
        string? OgImage,
        string? SeoContent,
        bool RobotsIndex,
        bool RobotsFollow,
        StorefrontCategoryResponse? Category,
        StorefrontVariationTemplateDto? VariationTemplate,
        DateTime CreatedOn,
        DateTime UpdatedAt,
        IReadOnlyList<StorefrontProductVariantResponse> Variants,
        decimal? DisplayPrice = null,
        decimal? DisplayComparePrice = null,
        string? DisplayCurrencyCode = null);

    public sealed record StorefrontProductVariantResponse(
        Guid Id,
        Guid ProductId,
        string? Sku,
        IReadOnlyList<StorefrontProductVariantAttributeResponse> Attributes,
        string? AttributeSignature,
        string? DisplayName,
        int SizeScale,
        string SizeValue,
        decimal? Price,
        decimal EffectivePrice,
        int Stock,
        bool IsActive,
        bool Purchasable,
        IReadOnlyList<string> PurchaseBlockReasons,
        string StockStatus,
        int? AvailableQuantity,
        string? Color,
        bool IsDefault,
        decimal? DisplayPrice = null,
        string? DisplayCurrencyCode = null);

    public sealed record StorefrontProductVariantAttributeResponse(
        string Name,
        string Value);

    public sealed class StorefrontProductSelectionPreviewRequest
    {
        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [StringLength(3, MinimumLength = 3)]
        public string? CurrencyCode { get; set; }
    }

    public sealed record StorefrontProductSelectionPreviewResponse(
        Guid ProductId,
        Guid? ProductVariantId,
        bool IsValid,
        bool IsAvailable,
        bool CanAddToCart,
        IReadOnlyList<string> ValidationMessages,
        IReadOnlyList<StorefrontProductVariantAttributeResponse> SelectedAttributes,
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

    public sealed record StorefrontProductRecommendationResponse(
        Guid Id,
        string? Name,
        string? Image,
        decimal Price,
        string? CategoryName);
}
