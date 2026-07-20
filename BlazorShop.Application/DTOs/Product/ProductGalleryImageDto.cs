namespace BlazorShop.Application.DTOs.Product
{
    public sealed record ProductGalleryImageDto(
        Guid PublicId,
        string ImageUrl,
        string? ThumbnailUrl,
        string? FullSizeUrl,
        string? AltText,
        int SortOrder,
        bool IsPrimary,
        int? Width,
        int? Height,
        int Version);
}
