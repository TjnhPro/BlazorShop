namespace BlazorShop.Web.SharedV2.Models.Product
{
    using System.ComponentModel.DataAnnotations;

    public sealed class GetCatalogProduct
    {
        [Required]
        public Guid Id { get; set; }

        public string? Slug { get; set; }

        [Required(ErrorMessage = "The Name field is required.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "The Description field is required.")]
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

        [Required(ErrorMessage = "The Price field is required.")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [DataType(DataType.Currency)]
        public decimal? ComparePrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal? DisplayPrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal? DisplayComparePrice { get; set; }

        public string? DisplayCurrencyCode { get; set; }

        [Required(ErrorMessage = "The Image field is required.")]
        public string? Image { get; set; }

        [Required]
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

        public string? DeliveryEstimateText { get; set; }

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        [Required(ErrorMessage = "The Category field is required.")]
        public Guid CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public string? CategorySlug { get; set; }

        public bool HasVariants { get; set; }

        public bool IsNew => DateTime.UtcNow.Subtract(this.CreatedOn).TotalDays <= 7;
    }
}
