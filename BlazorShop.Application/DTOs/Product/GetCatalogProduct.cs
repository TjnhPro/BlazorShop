namespace BlazorShop.Application.DTOs.Product
{
    using System.ComponentModel.DataAnnotations;

    public sealed class GetCatalogProduct
    {
        [Required]
        public Guid Id { get; set; }

        public string? Slug { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        public string? Sku { get; set; }

        public string? ShortDescription { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [DataType(DataType.Currency)]
        public decimal? ComparePrice { get; set; }

        [Required]
        public string? Image { get; set; }

        public Guid? PrimaryMediaPublicId { get; set; }

        public bool HasPrimaryMedia { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int DisplayOrder { get; set; }

        public bool InStock { get; set; }

        public bool IsPublished { get; set; }

        public DateTime? PublishedOn { get; set; }

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        public Guid? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? CategorySlug { get; set; }

        public bool HasVariants { get; set; }

        public string ProductType { get; set; } = string.Empty;

        public Guid? VariationTemplateId { get; set; }
    }
}
