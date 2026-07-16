namespace BlazorShop.Web.SharedV2.Models.Product
{
    public class GetProductVariant
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string? Sku { get; set; }

        public IReadOnlyList<ProductVariantAttributeDto> Attributes { get; set; } = Array.Empty<ProductVariantAttributeDto>();

        public string? AttributeSignature { get; set; }

        public string? DisplayName { get; set; }

        public int SizeScale { get; set; }

        public string SizeValue { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        public decimal EffectivePrice { get; set; }

        public decimal? DisplayPrice { get; set; }

        public string? DisplayCurrencyCode { get; set; }

        public int Stock { get; set; }

        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; }
    }
}
