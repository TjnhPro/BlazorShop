namespace BlazorShop.Application.CommerceNode.Media
{
    public sealed record CategoryMediaAssignmentDto(
        Guid CategoryId,
        Guid? MediaAssetPublicId,
        string? PublicUrl,
        string? AltText,
        int SortOrder,
        bool IsPrimary,
        DateTimeOffset? UpdatedAt);

    public sealed record SetCategoryPrimaryMediaRequest(
        Guid MediaAssetPublicId,
        string? AltText = null);

    public sealed record CategoryMediaOperationResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        CategoryMediaOperationFailure? Failure = null);

    public enum CategoryMediaOperationFailure
    {
        Validation,
        NotFound,
        Conflict
    }
}
