namespace BlazorShop.Domain.Entities.Payment
{
    public sealed class ShipmentTrackingEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ShipmentId { get; set; }

        public Guid StoreId { get; set; }

        public Guid OrderId { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        public string? Location { get; set; }

        public string Source { get; set; } = "manual_admin";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Shipment? Shipment { get; set; }
    }
}
