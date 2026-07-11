namespace BlazorShop.Application.CommerceNode.ProductImports
{
    using BlazorShop.Application.DTOs;

    public static class ProductImportModes
    {
        public const string CreateOnly = "create_only";

        public const string Upsert = "upsert";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            CreateOnly,
            Upsert,
        };
    }

    public static class ProductImportTaskTypes
    {
        public const string Import = "product.import";
    }

    public sealed record ProductImportUploadRequest(
        string? FileName,
        string? Mode,
        Stream Content,
        long FileSizeBytes,
        string? CreatedBy = null);

    public sealed record ProductImportJobListQuery(
        string? Status = null,
        int Skip = 0,
        int Take = 50);

    public sealed record ProductImportRowsQuery(
        string? Status = null,
        int Skip = 0,
        int Take = 100);

    public sealed record ProductImportJobListResponse(
        IReadOnlyList<ProductImportJobDto> Items,
        int TotalCount,
        int Skip,
        int Take);

    public sealed record ProductImportUploadResponse(ProductImportJobDto Job);

    public sealed record ProductImportJobDto(
        Guid PublicId,
        Guid StoreId,
        Guid? TaskPublicId,
        string Mode,
        string Status,
        string FileName,
        string FileHash,
        long FileSizeBytes,
        int TotalRows,
        int CreatedCount,
        int UpdatedCount,
        int FailedCount,
        int SkippedCount,
        int MediaQueuedCount,
        string? ErrorMessage,
        string? ErrorJson,
        DateTime CreatedAt,
        DateTime? StartedAt,
        DateTime? CompletedAt,
        DateTime UpdatedAt);

    public sealed record ProductImportJobDetailDto(
        ProductImportJobDto Job,
        IReadOnlyList<ProductImportRowDto> RecentRows);

    public sealed record ProductImportRowsResponse(
        IReadOnlyList<ProductImportRowDto> Items,
        int TotalCount,
        int Skip,
        int Take);

    public sealed record ProductImportRowDto(
        int RowNumber,
        string? Sku,
        string Status,
        string Action,
        Guid? ProductId,
        string MediaStatus,
        Guid? MediaTaskPublicId,
        string? ErrorMessage,
        string? ErrorJson,
        string? RawDataJson,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record ProductImportTaskPayload(
        string SchemaVersion,
        Guid JobPublicId,
        Guid StoreId,
        string Mode,
        string StoredFilePath);

    public sealed record ProductImportTaskResult(
        Guid JobPublicId,
        int TotalRows,
        int Created,
        int Updated,
        int Failed,
        int Skipped,
        int MediaQueued);

    public sealed record ProductImportParsedFile(
        IReadOnlyList<ProductImportParsedRow> Rows,
        IReadOnlyList<ProductImportError> Errors);

    public sealed record ProductImportParsedRow(
        int RowNumber,
        IReadOnlyDictionary<string, string?> Values);

    public sealed record ProductImportError(string Column, string Message);

    public interface IProductImportCsvParser
    {
        Task<ProductImportParsedFile> ParseAsync(Stream content, int maxRows, CancellationToken cancellationToken = default);
    }

    public interface IProductImportService
    {
        Task<ServiceResponse<ProductImportUploadResponse>> UploadAsync(
            ProductImportUploadRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<ProductImportJobListResponse>> ListAsync(
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<ProductImportJobDetailDto>> GetByPublicIdAsync(
            Guid jobPublicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<ProductImportRowsResponse>> ListRowsAsync(
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default);
    }
}
