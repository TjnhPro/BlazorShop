namespace BlazorShop.Domain.Contracts
{
    public sealed class CatalogProductReadModel
    {
        public Guid Id { get; init; }

        public string? Slug { get; init; }

        public string? Name { get; init; }

        public string? Description { get; init; }

        public string? Sku { get; init; }

        public string? Gtin { get; init; }

        public string? Barcode { get; init; }

        public string? ManufacturerPartNumber { get; init; }

        public string? Condition { get; init; }

        public decimal? Weight { get; init; }

        public decimal? Length { get; init; }

        public decimal? Width { get; init; }

        public decimal? Height { get; init; }

        public string? ShortDescription { get; init; }

        public decimal Price { get; init; }

        public decimal? ComparePrice { get; init; }

        public string? Image { get; init; }

        public Guid? PrimaryMediaPublicId { get; init; }

        public bool HasPrimaryMedia { get; init; }

        public DateTime CreatedOn { get; init; }

        public DateTime UpdatedAt { get; init; }

        public int DisplayOrder { get; init; }

        public bool InStock { get; init; }

        public bool IsPublished { get; init; }

        public DateTime? PublishedOn { get; init; }

        public DateTime? AvailableStartUtc { get; init; }

        public DateTime? AvailableEndUtc { get; init; }

        public Guid? CategoryId { get; init; }

        public string? CategoryName { get; init; }

        public string? CategorySlug { get; init; }

        public bool HasVariants { get; init; }

        public string ProductType { get; init; } = string.Empty;

        public Guid? VariationTemplateId { get; init; }
    }
}
