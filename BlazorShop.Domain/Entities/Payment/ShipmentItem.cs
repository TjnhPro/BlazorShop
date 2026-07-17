namespace BlazorShop.Domain.Entities.Payment
{
    public sealed class ShipmentItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ShipmentId { get; set; }

        public Guid OrderLineId { get; set; }

        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Shipment? Shipment { get; set; }

        public OrderLine? OrderLine { get; set; }
    }
}
