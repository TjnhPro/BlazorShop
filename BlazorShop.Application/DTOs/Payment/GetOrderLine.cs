namespace BlazorShop.Application.DTOs.Payment
{
    using BlazorShop.Application.DTOs.Product.ProductVariant;

    public class GetOrderLine
    {
        public Guid ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? Sku { get; set; }

        public string? Image { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<ProductVariantAttributeDto> VariantAttributes { get; set; } = Array.Empty<ProductVariantAttributeDto>();

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;
    }
}
