namespace BlazorShop.Web.SharedV2.Models.Product
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Web.SharedV2.Models.Category;

    public class GetProduct : ProductBase
    {
        [Required]
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

        public bool IsNew => DateTime.UtcNow.Subtract(this.CreatedOn).TotalDays <= 7;

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
}
