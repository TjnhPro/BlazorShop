namespace BlazorShop.Application.DTOs.Payment
{
    public class UpsertShipmentRequest
    {
        public DateTime ShipDate { get; set; }

        public string CarrierName { get; set; } = string.Empty;

        public string? CarrierService { get; set; }

        public string TrackingNumber { get; set; } = string.Empty;

        public string? TrackingUrl { get; set; }

        public string? Note { get; set; }
    }
}
