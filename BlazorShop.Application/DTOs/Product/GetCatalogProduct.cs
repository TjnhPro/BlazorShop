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

        public string? Gtin { get; set; }

        public string? Barcode { get; set; }

        public string? ManufacturerPartNumber { get; set; }

        public string? Condition { get; set; }

        public decimal? Weight { get; set; }

        public decimal? Length { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

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

        public int MinOrderQuantity { get; set; } = 1;

        public int? MaxOrderQuantity { get; set; }

        public int QuantityStep { get; set; } = 1;

        public bool PurchasingDisabled { get; set; }

        public string? PurchasingDisabledReason { get; set; }

        public bool ManageStock { get; set; } = true;

        public bool HideWhenOutOfStock { get; set; }

        public bool ShippingRequired { get; set; } = true;

        public bool FreeShipping { get; set; }

        public string? DeliveryEstimateText { get; set; }

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
