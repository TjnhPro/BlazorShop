namespace BlazorShop.Domain.Entities.Payment
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;

    public sealed class StorePaymentMethod
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string PaymentMethodKey { get; set; } = PaymentMethodKeys.Cod;

        public bool Enabled { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public string? SettingsJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public CommerceStore? Store { get; set; }
    }
}
