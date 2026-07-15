namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreNavigationMenuItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid MenuId { get; set; }

        public Guid? ParentItemId { get; set; }

        public string Label { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string? TargetKey { get; set; }

        public Guid? TargetEntityPublicId { get; set; }

        public string? Url { get; set; }

        public bool IsEnabled { get; set; } = true;

        public int DisplayOrder { get; set; }

        public bool OpensInNewTab { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ArchivedAt { get; set; }

        public CommerceStore? Store { get; set; }

        public StoreNavigationMenu? Menu { get; set; }

        public StoreNavigationMenuItem? ParentItem { get; set; }

        public ICollection<StoreNavigationMenuItem> Children { get; set; } = new List<StoreNavigationMenuItem>();
    }
}
