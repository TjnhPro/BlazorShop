namespace BlazorShop.Application.CommerceNode.ProductMedia
{
    public static class ProductMediaTaskTypes
    {
        public const string Import = "product.media.import";
    }

    public sealed record ProductMediaListResponse(IReadOnlyList<ProductMediaDto> Items);

    public sealed record ImportProductMediaRequest(IReadOnlyList<ImportProductMediaItem> Items);

    public sealed record ImportProductMediaItem(
        string? SourceUrl,
        int SortOrder = 0,
        bool IsPrimary = false,
        string? AltText = null);

    public sealed record ImportProductMediaResponse(
        Guid TaskPublicId,
        IReadOnlyList<ProductMediaDto> Items);

    public sealed record UpdateProductMediaOrderRequest(IReadOnlyList<UpdateProductMediaOrderItem> Items);

    public sealed record UpdateProductMediaOrderItem(Guid MediaPublicId, int SortOrder);

    public sealed record ProductMediaDto(
        Guid PublicId,
        Guid StoreId,
        Guid ProductId,
        string? OriginalSourceUrl,
        string? PublicUrl,
        string Status,
        string? ErrorMessage,
        int SortOrder,
        bool IsPrimary,
        string? AltText,
        int Version,
        int? Width,
        int? Height,
        long? FileSizeBytes,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? ProcessedAt);

    public sealed record ProductMediaImportTaskPayload(
        string SchemaVersion,
        Guid StoreId,
        Guid ProductId,
        IReadOnlyList<ProductMediaImportTaskItem> Items,
        string? RequestedBy = null,
        string? CorrelationId = null);

    public sealed record ProductMediaImportTaskItem(
        Guid MediaPublicId,
        string SourceUrl,
        int SortOrder,
        bool IsPrimary,
        string? AltText);

    public sealed record ProductMediaOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ProductMediaOperationFailure? Failure = null);

    public enum ProductMediaOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
