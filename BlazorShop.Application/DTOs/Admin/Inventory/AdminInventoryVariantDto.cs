namespace BlazorShop.Application.DTOs.Admin.Inventory
{
    using BlazorShop.Application.DTOs.Product.ProductVariant;

    public class AdminInventoryVariantDto
    {
        public Guid VariantId { get; set; }

        public Guid ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? Sku { get; set; }

        public string? DisplayName { get; set; }

        public IReadOnlyList<ProductVariantAttributeDto> Attributes { get; set; } = Array.Empty<ProductVariantAttributeDto>();

        public string SizeScale { get; set; } = string.Empty;

        public string SizeValue { get; set; } = string.Empty;

        public string? Color { get; set; }

        public decimal EffectivePrice { get; set; }

        public int Stock { get; set; }

        public bool IsLowStock { get; set; }

        public bool IsOutOfStock { get; set; }
    }
}
