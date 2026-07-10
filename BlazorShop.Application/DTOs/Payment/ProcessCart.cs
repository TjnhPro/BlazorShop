namespace BlazorShop.Application.DTOs.Payment
{
    public class ProcessCart
    {
        public required Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public required int Quantity { get; set; }
    }
}
