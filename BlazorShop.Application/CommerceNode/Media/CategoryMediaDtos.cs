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

}
