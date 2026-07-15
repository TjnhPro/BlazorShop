namespace BlazorShop.Web.SharedV2.Models.Payment
{
    public class GetOrderLine
    {
        public Guid ProductId { get; set; }

        public string? ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public string? CurrencyCode { get; set; }

        public decimal? BaseUnitPrice { get; set; }

        public decimal? ConvertedUnitPrice { get; set; }

        public decimal? PersistedLineTotal { get; set; }

        public decimal? BaseLineTotal { get; set; }

        public decimal LineTotal => PersistedLineTotal ?? UnitPrice * Quantity;
    }
}
