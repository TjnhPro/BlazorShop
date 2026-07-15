namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities.Payment;

    public sealed class PaymentAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid CheckoutSessionId { get; set; }

        public Guid? OrderId { get; set; }

        public string PaymentMethodKey { get; set; } = string.Empty;

        public string ProviderKey { get; set; } = string.Empty;

        public string State { get; set; } = PaymentAttemptStates.Created;

        public decimal Amount { get; set; }

        public string CurrencyCode { get; set; } = "USD";

        public string? BaseCurrencyCode { get; set; }

        public decimal? BaseAmount { get; set; }

        public decimal? ExchangeRate { get; set; }

        public string? ExchangeRateProviderKey { get; set; }

        public string? ExchangeRateSource { get; set; }

        public DateTimeOffset? ExchangeRateEffectiveAtUtc { get; set; }

        public DateTimeOffset? ExchangeRateExpiresAtUtc { get; set; }

        public string IdempotencyKey { get; set; } = string.Empty;

        public string? ProviderReference { get; set; }

        public string? ProviderSessionId { get; set; }

        public string? NextActionType { get; set; }

        public string? NextActionUrl { get; set; }

        public string? FailureCode { get; set; }

        public string? FailureMessage { get; set; }

        public string? MetadataJson { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(30);

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }

        public CheckoutSession? CheckoutSession { get; set; }

        public Order? Order { get; set; }
    }
}
