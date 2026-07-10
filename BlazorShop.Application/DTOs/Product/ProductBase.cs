namespace BlazorShop.Application.DTOs.Product
{
    using System.ComponentModel.DataAnnotations;

    public class ProductBase
    {
        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Description { get; set; }

        [MaxLength(64)]
        public string? Sku { get; set; }

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

        [Required]
        public Guid CategoryId { get; set; }
    }
}
