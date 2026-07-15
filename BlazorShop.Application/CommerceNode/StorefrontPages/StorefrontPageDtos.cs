namespace BlazorShop.Application.CommerceNode.StorefrontPages
{
    using BlazorShop.Application.DTOs;

    public sealed record StorefrontPageListQuery(
        int PageNumber = 1,
        int PageSize = 25,
        string? Search = null,
        string? Status = null);

    public sealed record StorefrontPageListResponse(
        IReadOnlyList<StorefrontPageSummaryDto> Items,
        int TotalCount = 0,
        int PageNumber = 1,
        int PageSize = 25,
        int TotalPages = 0);

    public sealed record StorefrontPageSummaryDto(
        Guid Id,
        Guid PublicId,
        Guid StoreId,
        string Slug,
        string Title,
        string? Intro,
        bool IsPublished,
        bool IncludeInSitemap,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? PageKey = null,
        int DisplayOrder = 0,
        bool IncludeInNavigation = false,
        string? NavigationLocation = null);

    public sealed record StorefrontPageDetailDto(
        Guid Id,
        Guid PublicId,
        Guid StoreId,
        string Slug,
        string Title,
        string? Intro,
        string BodyHtml,
        bool IsPublished,
        bool IncludeInSitemap,
        StorefrontPageSeoDto Seo,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string? PageKey,
        int DisplayOrder,
        bool IncludeInNavigation,
        string? NavigationLocation);

    public sealed record StorefrontPageSeoDto(
        string? MetaTitle,
        string? MetaDescription,
        string? CanonicalUrl,
        string? OgTitle,
        string? OgDescription,
        string? OgImage,
        bool RobotsIndex = true,
        bool RobotsFollow = true);

    public sealed record CreateStorefrontPageRequest(
        string? Slug,
        string? Title,
        string? Intro,
        string? BodyHtml,
        bool IsPublished = false,
        bool IncludeInSitemap = false,
        StorefrontPageSeoDto? Seo = null,
        string? PageKey = null,
        int DisplayOrder = 0,
        bool IncludeInNavigation = false,
        string? NavigationLocation = null);

    public sealed record UpdateStorefrontPageRequest(
        string? Slug,
        string? Title,
        string? Intro,
        string? BodyHtml,
        bool IsPublished = false,
        bool IncludeInSitemap = false,
        StorefrontPageSeoDto? Seo = null,
        string? PageKey = null,
        int DisplayOrder = 0,
        bool IncludeInNavigation = false,
        string? NavigationLocation = null);

    public sealed record StorefrontPagePublicDto(
        string Slug,
        string Title,
        string? Intro,
        string BodyHtml,
        StorefrontPageSeoDto Seo,
        DateTimeOffset UpdatedAt);

    public sealed record StorefrontPageSitemapEntryDto(
        string Slug,
        DateTimeOffset UpdatedAt);

    public static class StorefrontPageStatuses
    {
        public const string All = "all";
        public const string Published = "published";
        public const string Draft = "draft";
    }

    public interface IStorefrontPageService
    {
        Task<ServiceResponse<StorefrontPageListResponse>> ListAsync(
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> CreateAsync(
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> UpdateAsync(
            Guid id,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> ArchiveAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPagePublicDto>> GetPublishedBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<IReadOnlyList<StorefrontPageSitemapEntryDto>>> ListSitemapEntriesAsync(
            CancellationToken cancellationToken = default);
    }
}
