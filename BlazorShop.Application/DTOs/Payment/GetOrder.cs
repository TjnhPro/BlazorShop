namespace BlazorShop.Application.DTOs.Payment
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

        public string? CurrencyCode { get; set; }

        public decimal TotalAmount { get; set; }

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

        public string? ShippingFullName { get; set; }

        public string? ShippingEmail { get; set; }

        public string? ShippingPhone { get; set; }

        public string? ShippingAddress1 { get; set; }

        public string? ShippingAddress2 { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingState { get; set; }

        public string? ShippingPostalCode { get; set; }

        public string? ShippingCountryCode { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? AdminNote { get; set; }

        public IEnumerable<GetOrderLine> Lines { get; set; } = Array.Empty<GetOrderLine>();
    }
}
