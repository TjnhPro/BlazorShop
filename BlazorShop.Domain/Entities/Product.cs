namespace BlazorShop.Domain.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;

    public class Product
    {
        [Key]
        public Guid Id { get; set; }

        public string? Name { get; set; }

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

        [Column(TypeName = "decimal(18,3)")]
        public decimal? Weight { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? Length { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? Height { get; set; }

        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ComparePrice { get; set; }

        public string? Image { get; set; }

        public int Quantity { get; set; }

        public int MinOrderQuantity { get; set; } = 1;

        public int? MaxOrderQuantity { get; set; }

        public int QuantityStep { get; set; } = 1;

        public bool PurchasingDisabled { get; set; }

        [MaxLength(ProductPurchaseConstraints.PurchasingDisabledReasonMaxLength)]
        public string? PurchasingDisabledReason { get; set; }

        public bool ManageStock { get; set; } = true;

        public bool HideWhenOutOfStock { get; set; }

        public bool ShippingRequired { get; set; } = true;

        public bool FreeShipping { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ShippingSurcharge { get; set; }

        [MaxLength(ProductPurchaseConstraints.DeliveryEstimateTextMaxLength)]
        public string? DeliveryEstimateText { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ArchivedAt { get; set; }

        public int DisplayOrder { get; set; }

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

        public DateTime? PublishedOn { get; set; } = DateTime.UtcNow;

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        public Guid? StoreId { get; set; }

        public string ProductType { get; set; } = ProductTypes.Simple;

        public Guid? VariationTemplateId { get; set; }

        public VariationTemplate? VariationTemplate { get; set; }

        public Guid? CategoryId { get; set; }

        public Category? Category { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
