namespace BlazorShop.Web.SharedV2.Models.Product
{
    using System.ComponentModel.DataAnnotations;

    public class ProductBase
    {
        [Required(ErrorMessage = "The Name field is required.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "The Description field is required.")]
        public string? Description { get; set; }

        [MaxLength(64)]
        public string? Sku { get; set; }

        [MaxLength(32)]
        public string? Gtin { get; set; }

        [MaxLength(64)]
        public string? Barcode { get; set; }

        [MaxLength(128)]
        public string? ManufacturerPartNumber { get; set; }

        [MaxLength(32)]
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

        [Required(ErrorMessage = "The Image field is required.")]
        public string? Image { get; set; }

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

        [Required(ErrorMessage = "The Quantity field is required.")]
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

        public int DisplayOrder { get; set; }

        public bool InStock { get; set; }

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        [Required(ErrorMessage = "The Category field is required.")]
        public Guid? CategoryId { get; set; }
    }
}
