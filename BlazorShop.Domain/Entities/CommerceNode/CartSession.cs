namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    public sealed class CartSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public Guid? CustomerId { get; set; }

        public string? AppUserId { get; set; }

        public string State { get; set; } = CartSessionStates.Active;

        public int Version { get; set; } = 1;

        public DateTimeOffset LastActivityAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddDays(30);

        public Guid? ConvertedOrderId { get; set; }

        public Guid? MergedIntoCartId { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public CommerceStore? Store { get; set; }

        public CommerceCustomer? Customer { get; set; }

        public AppUser? AppUser { get; set; }

        public Order? ConvertedOrder { get; set; }

        public CartSession? MergedIntoCart { get; set; }

        public ICollection<CartLine> Lines { get; set; } = new List<CartLine>();
    }
}
