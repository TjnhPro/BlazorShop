namespace BlazorShop.Application.CommerceNode.Media
{
    public sealed record CommerceMediaAssetListQuery(
        int PageNumber = 1,
        int PageSize = 25,
        string? Search = null);

    public sealed record CommerceMediaAssetListResponse(
        IReadOnlyList<CommerceMediaAssetDto> Items,
        int TotalCount = 0,
        int PageNumber = 1,
        int PageSize = 25,
        int TotalPages = 0);

    public sealed record CommerceMediaAssetDto(
        Guid PublicId,
        Guid StoreId,
        string OriginalFileName,
        string CanonicalFileName,
        string DisplayName,
        string AltText,
        string? TitleText,
        string PublicUrl,
        string MimeType,
        string Extension,
        int? Width,
        int? Height,
        long FileSizeBytes,
        long Version,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record CommerceMediaAssetMetadataRequest(
        string? DisplayName,
        string? AltText,
        string? TitleText);

    public sealed record CommerceMediaAssetUploadRequest(
        Stream Content,
        string? FileName,
        string? ContentType,
        long FileSizeBytes);

    public sealed record CommerceMediaAssetOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        CommerceMediaAssetOperationFailure? Failure = null);

    public enum CommerceMediaAssetOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
