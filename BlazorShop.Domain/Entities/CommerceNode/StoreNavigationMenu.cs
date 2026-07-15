namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreNavigationMenu
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string SystemName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ArchivedAt { get; set; }

        public CommerceStore? Store { get; set; }

        public ICollection<StoreNavigationMenuItem> Items { get; set; } = new List<StoreNavigationMenuItem>();
    }
}
