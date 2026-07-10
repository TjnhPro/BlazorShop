namespace BlazorShop.Application.DTOs.Payment
{
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    public class ProcessCart
    {
        public required Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        public required int Quantity { get; set; }
    }
}
