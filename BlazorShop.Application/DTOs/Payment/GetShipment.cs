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
    }
}
