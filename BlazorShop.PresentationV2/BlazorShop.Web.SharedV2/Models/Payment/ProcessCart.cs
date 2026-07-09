namespace BlazorShop.Web.SharedV2.Models.Payment
{
    public class ProcessCart
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public Guid? VariantId { get; set; }

        public string? SizeValue { get; set; }

        public decimal? UnitPrice { get; set; }
    }
}
