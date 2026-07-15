namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreCurrency
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public bool IsDefaultDisplayCurrency { get; set; }

        public int DisplayOrder { get; set; }

        public string? CultureName { get; set; }

        public string? Symbol { get; set; }

        public int DecimalDigits { get; set; } = 2;

        public string UnitPriceRoundingMode { get; set; } = "halfAwayFromZero";

        public decimal UnitPriceRoundingIncrement { get; set; } = 0.01m;

        public string LineTotalRoundingMode { get; set; } = "halfAwayFromZero";

        public decimal LineTotalRoundingIncrement { get; set; } = 0.01m;

        public string OrderTotalRoundingMode { get; set; } = "halfAwayFromZero";

        public decimal OrderTotalRoundingIncrement { get; set; } = 0.01m;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }
    }
}
