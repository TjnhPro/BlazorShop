namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    public sealed class CommerceCustomer
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string? AppUserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string NormalizedEmail { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LastCheckoutAt { get; set; }

        public CommerceStore? Store { get; set; }

        public AppUser? AppUser { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<CommerceCustomerAddress> Addresses { get; set; } = new List<CommerceCustomerAddress>();
    }
}
