namespace BlazorShop.Web.SharedV2.Models.Payment
{
    public class GetOrder
    {
        public Guid Id { get; set; }

        public string Reference { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? CurrencyCode { get; set; }

        public decimal TotalAmount { get; set; }

        public string? BaseCurrencyCode { get; set; }

        public decimal? BaseTotalAmount { get; set; }

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

        public string? AdminNote { get; set; }

        public IEnumerable<GetOrderLine> Lines { get; set; } = Array.Empty<GetOrderLine>();
    }
}
