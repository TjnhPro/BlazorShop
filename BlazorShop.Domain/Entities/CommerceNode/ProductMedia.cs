namespace BlazorShop.Domain.Entities.CommerceNode
{
    using BlazorShop.Domain.Entities;

    public sealed class ProductMedia
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid ProductId { get; set; }

        public string? OriginalSourceUrl { get; set; }

        public string? OriginalStoragePath { get; set; }

        public string? ContentHash { get; set; }

        public string? FileName { get; set; }

        public string? MimeType { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public long? FileSizeBytes { get; set; }

        public int SortOrder { get; set; }

        public bool IsPrimary { get; set; }

        public string? AltText { get; set; }

        public string Status { get; set; } = ProductMediaStatuses.Pending;

        public string? ErrorMessage { get; set; }

        public int Version { get; set; } = 1;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ProcessedAt { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public Product? Product { get; set; }
    }
}
