namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities;

    public sealed class CategoryMediaAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid CategoryId { get; set; }

        public Guid MediaAssetId { get; set; }

        public string? AltText { get; set; }

        public int SortOrder { get; set; }

        public bool IsPrimary { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public Category? Category { get; set; }

        public CommerceMediaAsset? MediaAsset { get; set; }
    }
}
