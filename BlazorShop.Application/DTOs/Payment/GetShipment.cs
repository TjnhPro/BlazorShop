namespace BlazorShop.Application.DTOs.Payment
{
    public class GetShipment
    {
        public Guid Id { get; set; }

        public Guid StoreId { get; set; }

        public Guid OrderId { get; set; }

        public DateTime ShipDate { get; set; }

        public string CarrierName { get; set; } = string.Empty;

        public string? CarrierService { get; set; }

        public string TrackingNumber { get; set; } = string.Empty;

        public string? TrackingUrl { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public IReadOnlyList<GetShipmentItem> Items { get; set; } = [];

        public IReadOnlyList<GetShipmentTrackingEvent> TrackingEvents { get; set; } = [];
    }

    public sealed class GetShipmentItem
    {
        public Guid Id { get; set; }

        public Guid OrderLineId { get; set; }

        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }

    public sealed class GetShipmentTrackingEvent
    {
        public Guid Id { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; }

        public string? Location { get; set; }

        public string Source { get; set; } = string.Empty;
    }
}
