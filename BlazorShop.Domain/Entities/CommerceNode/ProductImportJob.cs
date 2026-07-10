namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class ProductImportJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PublicId { get; set; } = Guid.NewGuid();

        public Guid StoreId { get; set; }

        public Guid? TaskPublicId { get; set; }

        public string Mode { get; set; } = "create_only";

        public string Status { get; set; } = ProductImportJobStatuses.Queued;

        public string FileName { get; set; } = string.Empty;

        public string StoredFilePath { get; set; } = string.Empty;

        public string FileHash { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public int TotalRows { get; set; }

        public int CreatedCount { get; set; }

        public int UpdatedCount { get; set; }

        public int FailedCount { get; set; }

        public int SkippedCount { get; set; }

        public int MediaQueuedCount { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductImportRow> Rows { get; set; } = new List<ProductImportRow>();
    }
}
