namespace BlazorShop.Web.SharedV2.Models.Payment
{
    public class GetOrderItem
    {
        public string? ProductName { get; set; }

        public int QuantityOrdered { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerEmail { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime DatePurchased { get; set; }

        public string? TrackingNumber { get; set; }

        public string? TrackingUrl { get; set; }

        public string? ShippingStatus { get; set; }
    }
}
