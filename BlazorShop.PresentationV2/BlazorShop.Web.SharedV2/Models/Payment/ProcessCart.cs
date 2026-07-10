namespace BlazorShop.Web.SharedV2.Models.Payment
{
    public class ProcessCart
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public Guid? ProductVariantId { get; set; }

        public Guid? VariantId { get; set; }

        public IReadOnlyList<SelectedAttribute>? SelectedAttributes { get; set; }

        public string? SizeValue { get; set; }

        public decimal? UnitPrice { get; set; }
    }

    public sealed record SelectedAttribute(string Name, string Value);
}
