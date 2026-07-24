namespace BlazorShop.Storefront.Models
{
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public sealed class ProductCatalogQuery
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 24;

        public Guid? CategoryId { get; set; }

        public string? CategorySlug { get; set; }

        public bool IncludeSubcategories { get; set; }

        public string? SearchTerm { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public bool? InStock { get; set; }

        public ProductCatalogSortBy SortBy { get; set; } = ProductCatalogSortBy.Newest;

        public DateTime? CreatedAfterUtc { get; set; }
    }

    public enum ProductCatalogSortBy
    {
        Newest = 0,
        Oldest = 1,
        PriceLowToHigh = 2,
        PriceHighToLow = 3,
        NameAscending = 4,
        NameDescending = 5,
        DisplayOrder = 6,
        Updated = 7,
    }

    public static class ProductCatalogSortByExtensions
    {
        public static string ToApiValue(this ProductCatalogSortBy sortBy)
        {
            return sortBy switch
            {
                ProductCatalogSortBy.Oldest => "oldest",
                ProductCatalogSortBy.PriceLowToHigh => "priceLowToHigh",
                ProductCatalogSortBy.PriceHighToLow => "priceHighToLow",
                ProductCatalogSortBy.NameAscending => "nameAscending",
                ProductCatalogSortBy.NameDescending => "nameDescending",
                ProductCatalogSortBy.DisplayOrder => "displayOrder",
                ProductCatalogSortBy.Updated => "updated",
                _ => "newest",
            };
        }

        public static bool TryParseApiValue(string? value, out ProductCatalogSortBy sortBy)
        {
            sortBy = value?.Trim() switch
            {
                "newest" => ProductCatalogSortBy.Newest,
                "oldest" => ProductCatalogSortBy.Oldest,
                "priceLowToHigh" => ProductCatalogSortBy.PriceLowToHigh,
                "priceHighToLow" => ProductCatalogSortBy.PriceHighToLow,
                "nameAscending" => ProductCatalogSortBy.NameAscending,
                "nameDescending" => ProductCatalogSortBy.NameDescending,
                "displayOrder" => ProductCatalogSortBy.DisplayOrder,
                "updated" => ProductCatalogSortBy.Updated,
                _ => default,
            };

            if (value is not null
                && Enum.TryParse<ProductCatalogSortBy>(value, ignoreCase: true, out var legacySortBy))
            {
                sortBy = legacySortBy;
                return true;
            }

            return value is "newest"
                or "oldest"
                or "priceLowToHigh"
                or "priceHighToLow"
                or "nameAscending"
                or "nameDescending"
                or "displayOrder"
                or "updated";
        }
    }

    public class CategoryBase
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class GetCategory : CategoryBase
    {
        public Guid Id { get; set; }

        public string? Slug { get; set; }

        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? CanonicalUrl { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public string? SeoContent { get; set; }

        public bool RobotsIndex { get; set; } = true;

        public bool RobotsFollow { get; set; } = true;

        public ICollection<GetProduct>? Products { get; set; }
    }

    public sealed class GetCategoryPage
    {
        public GetCategory Category { get; set; } = new();

        public IReadOnlyList<GetCategoryBreadcrumbItem> Breadcrumbs { get; set; } = Array.Empty<GetCategoryBreadcrumbItem>();

        public IReadOnlyList<GetCatalogProduct> Products { get; set; } = Array.Empty<GetCatalogProduct>();

        public int DirectProductCount { get; set; }

        public int DescendantProductCount { get; set; }
    }

    public sealed class GetCategoryBreadcrumbItem
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Slug { get; set; }
    }

    public sealed class GetCategoryTreeNode
    {
        public Guid Id { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public string? Name { get; set; }

        public string? Slug { get; set; }

        public string? Image { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public IReadOnlyList<GetCategoryTreeNode> Children { get; set; } = Array.Empty<GetCategoryTreeNode>();
    }

    public class ProductBase
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Sku { get; set; }

        public string? Gtin { get; set; }

        public string? Barcode { get; set; }

        public string? ManufacturerPartNumber { get; set; }

        public string? Condition { get; set; }

        public decimal? Weight { get; set; }

        public decimal? Length { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        public string? Image { get; set; }

        public decimal Price { get; set; }

        public decimal? ComparePrice { get; set; }

        public decimal? DisplayPrice { get; set; }

        public decimal? DisplayComparePrice { get; set; }

        public string? DisplayCurrencyCode { get; set; }

        public int Quantity { get; set; }

        public bool Purchasable { get; set; }

        public IReadOnlyList<string> PurchaseBlockReasons { get; set; } = Array.Empty<string>();

        public string StockStatus { get; set; } = string.Empty;

        public int? AvailableQuantity { get; set; }

        public int MinOrderQuantity { get; set; } = 1;

        public int? MaxOrderQuantity { get; set; }

        public int QuantityStep { get; set; } = 1;

        public bool ManageStock { get; set; } = true;

        public bool ShippingRequired { get; set; } = true;

        public bool FreeShipping { get; set; }

        public decimal? ShippingSurcharge { get; set; }

        public string? DeliveryEstimateText { get; set; }

        public int DisplayOrder { get; set; }

        public bool InStock { get; set; }

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        public Guid? CategoryId { get; set; }
    }

    public sealed class GetCatalogProduct
    {
        public Guid Id { get; set; }

        public string? Slug { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? Sku { get; set; }

        public string? Gtin { get; set; }

        public string? Barcode { get; set; }

        public string? ManufacturerPartNumber { get; set; }

        public string? Condition { get; set; }

        public decimal? Weight { get; set; }

        public decimal? Length { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public string? ShortDescription { get; set; }

        public decimal Price { get; set; }

        public decimal? ComparePrice { get; set; }

        public decimal? DisplayPrice { get; set; }

        public decimal? DisplayComparePrice { get; set; }

        public string? DisplayCurrencyCode { get; set; }

        public string? Image { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int DisplayOrder { get; set; }

        public bool InStock { get; set; }

        public int Quantity { get; set; }

        public bool Purchasable { get; set; }

        public IReadOnlyList<string> PurchaseBlockReasons { get; set; } = Array.Empty<string>();

        public string StockStatus { get; set; } = string.Empty;

        public int? AvailableQuantity { get; set; }

        public int MinOrderQuantity { get; set; } = 1;

        public int? MaxOrderQuantity { get; set; }

        public int QuantityStep { get; set; } = 1;

        public bool ManageStock { get; set; } = true;

        public bool ShippingRequired { get; set; } = true;

        public bool FreeShipping { get; set; }

        public decimal? ShippingSurcharge { get; set; }

        public string? DeliveryEstimateText { get; set; }

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        public Guid CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? CategorySlug { get; set; }

        public bool HasVariants { get; set; }

        public bool IsNew => DateTime.UtcNow.Subtract(CreatedOn).TotalDays <= 7;
    }

    public class GetProduct : ProductBase
    {
        public Guid Id { get; set; }

        public string? Slug { get; set; }

        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? CanonicalUrl { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public string? SeoContent { get; set; }

        public bool RobotsIndex { get; set; } = true;

        public bool RobotsFollow { get; set; } = true;

        public GetCategory? Category { get; set; }

        public StorefrontVariationTemplateDto? VariationTemplate { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsNew => DateTime.UtcNow.Subtract(CreatedOn).TotalDays <= 7;

        public IReadOnlyList<ProductGalleryImageDto> MediaGallery { get; set; } = Array.Empty<ProductGalleryImageDto>();

        public IEnumerable<GetProductVariant> Variants { get; set; } = Array.Empty<GetProductVariant>();
    }

    public sealed record ProductGalleryImageDto(
        Guid PublicId,
        string? ImageUrl,
        string? ThumbnailUrl,
        string? FullSizeUrl,
        string? AltText,
        int SortOrder,
        bool IsPrimary,
        int? Width,
        int? Height,
        int Version);

    public sealed record StorefrontVariationTemplateDto(
        string? Name,
        string? Slug,
        IReadOnlyList<StorefrontVariationOptionDto> Options);

    public sealed record StorefrontVariationOptionDto(
        string? Name,
        string? ControlType,
        bool IsRequired,
        IReadOnlyList<StorefrontVariationValueDto> Values);

    public sealed record StorefrontVariationValueDto(
        string? Value,
        string? ColorHex = null);

    public class GetProductVariant
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string? Sku { get; set; }

        public IReadOnlyList<ProductVariantAttributeDto> Attributes { get; set; } = Array.Empty<ProductVariantAttributeDto>();

        public string? AttributeSignature { get; set; }

        public string? DisplayName { get; set; }

        public int SizeScale { get; set; }

        public string SizeValue { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        public decimal EffectivePrice { get; set; }

        public decimal? DisplayPrice { get; set; }

        public string? DisplayCurrencyCode { get; set; }

        public int Stock { get; set; }

        public bool Purchasable { get; set; }

        public IReadOnlyList<string> PurchaseBlockReasons { get; set; } = Array.Empty<string>();

        public string StockStatus { get; set; } = string.Empty;

        public int? AvailableQuantity { get; set; }

        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; }
    }

    public sealed class ProductVariantAttributeDto
    {
        public string Name { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    public sealed class GetPublicCatalogSitemap
    {
        public IReadOnlyList<GetCategorySitemapEntry> Categories { get; set; } = Array.Empty<GetCategorySitemapEntry>();

        public IReadOnlyList<GetProductSitemapEntry> Products { get; set; } = Array.Empty<GetProductSitemapEntry>();

        public IReadOnlyList<GetPageSitemapEntry> Pages { get; set; } = Array.Empty<GetPageSitemapEntry>();
    }

    public sealed class GetCategorySitemapEntry
    {
        public string Slug { get; set; } = string.Empty;

        public DateTime? LastModifiedUtc { get; set; }
    }

    public sealed class GetProductSitemapEntry
    {
        public string Slug { get; set; } = string.Empty;

        public DateTime? LastModifiedUtc { get; set; }
    }

    public sealed class GetPageSitemapEntry
    {
        public string Slug { get; set; } = string.Empty;

        public DateTime? LastModifiedUtc { get; set; }
    }

    public sealed class GetStorefrontPage
    {
        public string Slug { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Intro { get; set; }

        public string BodyHtml { get; set; } = string.Empty;

        public StorefrontPageSeo Seo { get; set; } = new();

        public DateTimeOffset UpdatedAt { get; set; }

        public string? PageKey { get; set; }
    }

    public sealed class StorefrontPageSeo
    {
        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? CanonicalUrl { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public bool RobotsIndex { get; set; } = true;

        public bool RobotsFollow { get; set; } = true;
    }

    public sealed record StorefrontPageNavigationLinkDto(
        string PageKey,
        string Slug,
        string Title,
        string? NavigationLocation,
        int DisplayOrder);

    public static class StorefrontPageContentRules
    {
        public const string FooterCompany = "footer_company";
        public const string FooterSupport = "footer_support";
        public const string FooterLegal = "footer_legal";
        public const string Header = "header";

        public static readonly IReadOnlySet<string> PageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "about",
            "faq",
            "customer_service",
            "shipping_information",
            "payment_information",
            "terms_conditions",
            "privacy_policy",
            "return_refund_policy",
            "cookie_information",
            "home_content",
            "store_closed_content",
        };

        public static bool IsKnownPageKey(string value)
        {
            return PageKeys.Contains(value);
        }
    }

    public static class CatalogSearchPolicy
    {
        public const int MinimumSearchTermLength = 2;
        public const int SuggestionDefaultLimit = 6;
        public const int SuggestionMaxLimit = 10;

        public static IReadOnlyList<int> StorefrontPageSizes { get; } = [12, 24, 48];

        public static string? NormalizeSearchTerm(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public static bool IsSearchTermTooShort(string? normalizedSearchTerm)
        {
            return !string.IsNullOrEmpty(normalizedSearchTerm)
                && normalizedSearchTerm.Length < MinimumSearchTermLength;
        }
    }

    public static class StoreNavigationMenuNames
    {
        public const string Main = "main";
        public const string FooterCompany = "footer_company";
        public const string FooterSupport = "footer_support";
        public const string FooterLegal = "footer_legal";
        public const string Utility = "utility";
        public const string Mobile = "mobile";
    }

    public sealed record StoreNavigationPublicMenuDto(
        string SystemName,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<StoreNavigationPublicItemDto> Items);

    public sealed record StoreNavigationPublicItemDto(
        string Label,
        string? Href,
        string TargetType,
        string? TargetKey,
        bool OpensInNewTab,
        IReadOnlyList<StoreNavigationPublicItemDto> Children);

    public class SeoFieldsDto
    {
        public string? Slug { get; set; }

        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? CanonicalUrl { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public bool RobotsIndex { get; set; } = true;

        public bool RobotsFollow { get; set; } = true;

        public string? SeoContent { get; set; }

        public bool IsPublished { get; set; } = true;
    }

    public class SeoMetadataDto
    {
        public string? Title { get; set; }

        public string? MetaDescription { get; set; }

        public string? CanonicalUrl { get; set; }

        public string? OgTitle { get; set; }

        public string? OgDescription { get; set; }

        public string? OgImage { get; set; }

        public string? SiteName { get; set; }

        public bool RobotsIndex { get; set; } = true;

        public bool RobotsFollow { get; set; } = true;
    }

    public sealed class SeoRedirectResolutionDto
    {
        public string? NewPath { get; set; }

        public int StatusCode { get; set; }
    }

    public sealed class SeoSettingsDto
    {
        public Guid Id { get; set; }

        public string? SiteName { get; set; }

        public string? DefaultTitleSuffix { get; set; }

        public string? DefaultMetaDescription { get; set; }

        public string? DefaultOgImage { get; set; }

        public string? BaseCanonicalUrl { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyLogoUrl { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyAddress { get; set; }

        public string? FacebookUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? XUrl { get; set; }
    }

    public class GetSeoSettings
    {
        public Guid Id { get; set; }

        public string? SiteName { get; set; }

        public string? DefaultTitleSuffix { get; set; }

        public string? DefaultMetaDescription { get; set; }

        public string? DefaultOgImage { get; set; }

        public string? BaseCanonicalUrl { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyLogoUrl { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyAddress { get; set; }

        public string? FacebookUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? XUrl { get; set; }
    }
}
