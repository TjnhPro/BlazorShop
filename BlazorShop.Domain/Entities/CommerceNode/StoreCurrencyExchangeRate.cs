namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreCurrencyExchangeRate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string BaseCurrencyCode { get; set; } = string.Empty;

        public string TargetCurrencyCode { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        public string ProviderKey { get; set; } = "manual";

        public string? Source { get; set; }

        public DateTimeOffset EffectiveAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ExpiresAt { get; set; }

        public bool IsManual { get; set; } = true;

        public bool IsEnabled { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }
    }
}
