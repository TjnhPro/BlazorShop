namespace BlazorShop.Domain.Entities.CommerceNode
{
    public sealed class ProductImportRow
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid JobId { get; set; }

        public int RowNumber { get; set; }

        public string? Sku { get; set; }

        public string Status { get; set; } = ProductImportRowStatuses.Pending;

        public string Action { get; set; } = ProductImportRowActions.Skipped;

        public Guid? ProductId { get; set; }

        public string MediaStatus { get; set; } = ProductImportMediaStatuses.None;

        public Guid? MediaTaskPublicId { get; set; }

        public string? ErrorMessage { get; set; }

        public string? ErrorJson { get; set; }

        public string? RawDataJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ProductImportJob? Job { get; set; }
    }
}
