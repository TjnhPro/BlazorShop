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

        public int DisplayOrder { get; set; }

        [Required(ErrorMessage = "The Category field is required.")]
        public Guid? CategoryId { get; set; }
    }
}
