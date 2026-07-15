namespace BlazorShop.Domain.Entities.Payment
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;

    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string UserId { get; set; } = string.Empty;

        public Guid? CustomerId { get; set; }

        public string OrderStatus { get; set; } = OrderStatuses.Pending;

        public string PaymentStatus { get; set; } = PaymentStatuses.Pending;

        public string PaymentMethodKey { get; set; } = PaymentMethodKeys.Cod;

        public DateTime? PaymentAt { get; set; }

        public string? PaymentMetadataJson { get; set; }

        public string Reference { get; set; } = string.Empty;

        public Guid? StoreId { get; set; }

        public string? CurrencyCode { get; set; }

        public decimal TotalAmount { get; set; }

        public string? BaseCurrencyCode { get; set; }

        public decimal? BaseTotalAmount { get; set; }

        public decimal? ExchangeRate { get; set; }

        public string? ExchangeRateProviderKey { get; set; }

        public string? ExchangeRateSource { get; set; }

        public DateTimeOffset? ExchangeRateEffectiveAtUtc { get; set; }

        public DateTimeOffset? ExchangeRateExpiresAtUtc { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();

        public string? CustomerName { get; set; }

        public string? CustomerEmail { get; set; }

        public string ShippingFullName { get; set; } = string.Empty;

        public string ShippingEmail { get; set; } = string.Empty;

        public string? ShippingPhone { get; set; }

        public string ShippingAddress1 { get; set; } = string.Empty;

        public string? ShippingAddress2 { get; set; }

        public string ShippingCity { get; set; } = string.Empty;

        public string? ShippingState { get; set; }

        public string ShippingPostalCode { get; set; } = string.Empty;

        public string ShippingCountryCode { get; set; } = string.Empty;

        public string? ShippingCarrier { get; set; }

        public string? TrackingNumber { get; set; }

        public string? TrackingUrl { get; set; }

        public string ShippingStatus { get; set; } = ShippingStatuses.NotYetShipped;

        public DateTime? ShippedOn { get; set; }

        public DateTime? DeliveredOn { get; set; }

        public DateTime? LastTrackingUpdate { get; set; }

        public string? AdminNote { get; set; }

        public CommerceCustomer? Customer { get; set; }
    }
}
