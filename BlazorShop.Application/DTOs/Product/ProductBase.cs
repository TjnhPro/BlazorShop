namespace BlazorShop.Application.DTOs.Product
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Domain.Constants;

    public class ProductBase
    {
        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        [MaxLength(64)]
        public string? Sku { get; set; }

        [MaxLength(ProductIdentityConstraints.GtinMaxLength)]
        public string? Gtin { get; set; }

        [MaxLength(ProductIdentityConstraints.BarcodeMaxLength)]
        public string? Barcode { get; set; }

        [MaxLength(ProductIdentityConstraints.ManufacturerPartNumberMaxLength)]
        public string? ManufacturerPartNumber { get; set; }

        [MaxLength(ProductIdentityConstraints.ConditionMaxLength)]
        public string? Condition { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Weight { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Length { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Width { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Height { get; set; }

        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [DataType(DataType.Currency)]
        public decimal? ComparePrice { get; set; }

        [Required]
        public string? Image { get; set; }

        [Required]
        public int Quantity { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; } = true;

        public DateTime? PublishedOn { get; set; } = DateTime.UtcNow;

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        public string ProductType { get; set; } = ProductTypes.Simple;

        public Guid? VariationTemplateId { get; set; }

        public Guid? CategoryId { get; set; }
    }
}
