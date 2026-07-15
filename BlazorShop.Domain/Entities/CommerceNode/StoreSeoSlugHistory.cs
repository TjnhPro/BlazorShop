namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class StoreSeoSlugHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string EntityType { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        public string Slug { get; set; } = string.Empty;

        public string? LanguageCode { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ReplacedAt { get; set; }

        public string? ReplacedBySlug { get; set; }

        public CommerceStore? Store { get; set; }
    }
}
