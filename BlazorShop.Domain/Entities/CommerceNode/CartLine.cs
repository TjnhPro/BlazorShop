namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities;

    public sealed class CartLine
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CartSessionId { get; set; }

        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public string LineKey { get; set; } = string.Empty;

        public string? SelectedAttributesJson { get; set; }

        public string? PersonalizationHash { get; set; }

        public string? PersonalizationJson { get; set; }

        public Guid? ArtworkAssetId { get; set; }

        public int? ArtworkVersion { get; set; }

        public string? FulfillmentProviderKey { get; set; }

        public int Quantity { get; set; }

        public decimal? UnitPriceSnapshot { get; set; }

        public string? CurrencyCodeSnapshot { get; set; }

        public decimal? BaseUnitPriceSnapshot { get; set; }

        public string? BaseCurrencyCodeSnapshot { get; set; }

        public decimal? ExchangeRateSnapshot { get; set; }

        public string? ExchangeRateProviderKey { get; set; }

        public string? ExchangeRateSource { get; set; }

        public DateTimeOffset? ExchangeRateEffectiveAtUtc { get; set; }

        public DateTimeOffset? ExchangeRateExpiresAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CartSession? CartSession { get; set; }

        public Product? Product { get; set; }

        public ProductVariant? ProductVariant { get; set; }
    }
}
