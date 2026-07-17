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

        public Guid? StorePublicId { get; set; }

        public string? StoreKeySnapshot { get; set; }

        public string? StoreNameSnapshot { get; set; }

        public string? StoreBaseUrlSnapshot { get; set; }

        public string? StoreCompanyNameSnapshot { get; set; }

        public string? StoreCompanyEmailSnapshot { get; set; }

        public string? StoreCompanyPhoneSnapshot { get; set; }

        public string? StoreCompanyAddressSnapshot { get; set; }

        public string? CurrencyCode { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal? SubtotalAmount { get; set; }

        public decimal? ShippingTotalAmount { get; set; }

        public decimal? TaxTotalAmount { get; set; }

        public decimal? DiscountTotalAmount { get; set; }

        public decimal? GrandTotalAmount { get; set; }

        public string? BaseCurrencyCode { get; set; }

        public decimal? BaseTotalAmount { get; set; }

        public decimal? BaseSubtotalAmount { get; set; }

        public decimal? BaseShippingTotalAmount { get; set; }

        public decimal? BaseTaxTotalAmount { get; set; }

        public decimal? BaseDiscountTotalAmount { get; set; }

        public decimal? BaseGrandTotalAmount { get; set; }

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

        public string? BillingAddressSnapshotJson { get; set; }

        public string? ShippingAddressSnapshotJson { get; set; }

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

        public string? ShippingMethodKey { get; set; }

        public string? ShippingProviderSystemName { get; set; }

        public string? ShippingMethodCode { get; set; }

        public string? ShippingMethodName { get; set; }

        public decimal ShippingTotal { get; set; }

        public string? ShippingCurrencyCode { get; set; }

        public string? ShippingDeliveryEstimateText { get; set; }

        public string? ShippingMethodSnapshotJson { get; set; }

        public DateTime? ShippedOn { get; set; }

        public DateTime? DeliveredOn { get; set; }

        public DateTime? LastTrackingUpdate { get; set; }

        public string? AdminNote { get; set; }

        public string? GuestAccessTokenHash { get; set; }

        public DateTimeOffset? GuestAccessTokenExpiresAtUtc { get; set; }

        public CommerceCustomer? Customer { get; set; }
    }
}
