namespace BlazorShop.Web.SharedV2.Models.Payment
{
    public class GetOrder
    {
        public Guid Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string OrderStatus { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string PaymentMethodKey { get; set; } = string.Empty;

        public DateTime? PaymentAt { get; set; }

        public GetOrderStoreSnapshot? StoreSnapshot { get; set; }

        public string? CurrencyCode { get; set; }

        public decimal TotalAmount { get; set; }

        public GetOrderTotalBreakdown? TotalBreakdown { get; set; }

        public string? BaseCurrencyCode { get; set; }

        public decimal? BaseTotalAmount { get; set; }

        public GetOrderTotalBreakdown? BaseTotalBreakdown { get; set; }

        public decimal? ExchangeRate { get; set; }

        public string? ExchangeRateProviderKey { get; set; }

        public string? ExchangeRateSource { get; set; }

        public DateTimeOffset? ExchangeRateEffectiveAtUtc { get; set; }

        public DateTimeOffset? ExchangeRateExpiresAtUtc { get; set; }

        public DateTime CreatedOn { get; set; }

        public string ShippingStatus { get; set; } = string.Empty;

        public string? ShippingCarrier { get; set; }

        public string? TrackingNumber { get; set; }

        public string? TrackingUrl { get; set; }

        public DateTime? ShippedOn { get; set; }

        public DateTime? DeliveredOn { get; set; }

        public string? UserId { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerEmail { get; set; }

        public GetOrderAddress? BillingAddress { get; set; }

        public GetOrderAddress? ShippingAddressSnapshot { get; set; }

        public string? ShippingFullName { get; set; }

        public string? ShippingEmail { get; set; }

        public string? ShippingPhone { get; set; }

        public string? ShippingAddress1 { get; set; }

        public string? ShippingAddress2 { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingState { get; set; }

        public string? ShippingPostalCode { get; set; }

        public string? ShippingCountryCode { get; set; }

        public GetOrderShippingMethodSnapshot? ShippingMethod { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? AdminNote { get; set; }

        public IEnumerable<GetOrderLine> Lines { get; set; } = Array.Empty<GetOrderLine>();
    }

    public sealed class GetOrderStoreSnapshot
    {
        public Guid? PublicId { get; set; }

        public string? StoreKey { get; set; }

        public string? Name { get; set; }

        public string? BaseUrl { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyAddress { get; set; }
    }

    public sealed class GetOrderTotalBreakdown
    {
        public decimal? Subtotal { get; set; }

        public decimal? ShippingTotal { get; set; }

        public decimal? TaxTotal { get; set; }

        public decimal? DiscountTotal { get; set; }

        public decimal? GrandTotal { get; set; }
    }

    public sealed class GetOrderAddress
    {
        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address1 { get; set; }

        public string? Address2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? PostalCode { get; set; }

        public string? CountryCode { get; set; }
    }

    public sealed class GetOrderShippingMethodSnapshot
    {
        public string? Key { get; set; }

        public string? ProviderSystemName { get; set; }

        public string? MethodCode { get; set; }

        public string? Name { get; set; }

        public decimal? Total { get; set; }

        public string? CurrencyCode { get; set; }

        public string? DeliveryEstimateText { get; set; }
    }
}
