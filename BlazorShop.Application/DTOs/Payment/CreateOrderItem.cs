namespace BlazorShop.Application.DTOs.Payment
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.VariationTemplates;

    public class CreateOrderItem
    {
        [Required]
        public Guid ProductId { get; set; }

        public Guid? ProductVariantId { get; set; }

        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string? UserId { get; set; }
    }
}
