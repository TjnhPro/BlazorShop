namespace BlazorShop.Domain.Entities.Payment
{
    public class OrderLine
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrderId { get; set; }

        public Guid ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? Sku { get; set; }

        public string? Image { get; set; }

        public Guid? ProductVariantId { get; set; }

        public string? VariantAttributesJson { get; set; }

        public string? PersonalizationHash { get; set; }

        public string? PersonalizationJson { get; set; }

        public Guid? ArtworkAssetId { get; set; }

        public int? ArtworkVersion { get; set; }

        public string? FulfillmentProviderKey { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public Order? Order { get; set; }
    }
}
