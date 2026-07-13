namespace BlazorShop.Domain.Entities.Payment
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Domain.Constants;

    public class PaymentMethod
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Key { get; set; } = PaymentMethodKeys.Cod;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsEnabledByDefault { get; set; }

        public int SortOrder { get; set; }
    }
}
