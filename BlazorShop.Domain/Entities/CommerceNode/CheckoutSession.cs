namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities.Payment;

    public sealed class CheckoutSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid CartSessionId { get; set; }

        public Guid? CustomerId { get; set; }

        public Guid? OrderId { get; set; }

        public string State { get; set; } = CheckoutSessionStates.Draft;

        public int CheckoutVersion { get; set; } = 1;

        public string CurrentStep { get; set; } = CheckoutSteps.Entry;

        public string CompletedStepsJson { get; set; } = "[]";

        public int CartVersion { get; set; }

        public int LastValidatedCartVersion { get; set; }

        public string CustomerEmail { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }

        public string? BillingAddressSnapshotJson { get; set; }

        public string ShippingAddressSource { get; set; } = "direct";

        public string ShippingFullName { get; set; } = string.Empty;

        public string ShippingEmail { get; set; } = string.Empty;

        public string? ShippingPhone { get; set; }

        public string ShippingAddress1 { get; set; } = string.Empty;

        public string? ShippingAddress2 { get; set; }

        public string ShippingCity { get; set; } = string.Empty;

        public string? ShippingState { get; set; }

        public string ShippingPostalCode { get; set; } = string.Empty;

        public string ShippingCountryCode { get; set; } = string.Empty;

        public string? SelectedShippingOptionJson { get; set; }

        public string PaymentMethodKey { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal ShippingTotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public string CurrencyCode { get; set; } = "USD";

        public string? BaseCurrencyCode { get; set; }

        public decimal? BaseSubtotal { get; set; }

        public decimal? BaseGrandTotal { get; set; }

        public decimal? ExchangeRate { get; set; }

        public string? ExchangeRateProviderKey { get; set; }

        public string? ExchangeRateSource { get; set; }

        public DateTimeOffset? ExchangeRateEffectiveAtUtc { get; set; }

        public DateTimeOffset? ExchangeRateExpiresAtUtc { get; set; }

        public string? ValidationIssuesJson { get; set; }

        public bool TermsAccepted { get; set; }

        public string? TermsVersion { get; set; }

        public DateTimeOffset? TermsAcceptedAtUtc { get; set; }

        public string NextAction { get; set; } = "review";

        public string? IdempotencyKey { get; set; }

        public DateTimeOffset? PlacedAtUtc { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddHours(1);

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }

        public CartSession? CartSession { get; set; }

        public CommerceCustomer? Customer { get; set; }

        public Order? Order { get; set; }
    }
}
