namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class CommerceMediaAsset
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string CanonicalFileName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string AltText { get; set; } = string.Empty;

        public string? TitleText { get; set; }

        public string OriginalStoragePath { get; set; } = string.Empty;

        public string ContentHash { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public int? Width { get; set; }

        public int? Height { get; set; }

        public long FileSizeBytes { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
