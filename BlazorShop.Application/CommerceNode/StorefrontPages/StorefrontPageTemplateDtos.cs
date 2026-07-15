namespace BlazorShop.Application.CommerceNode.StorefrontPages
{
    using BlazorShop.Application.DTOs;

    public sealed record StorefrontPageTemplateDefinitionDto(
        string PageKey,
        string DisplayName,
        string DefaultSlug,
        string DefaultTitle,
        bool RequiredForReadiness,
        string? DefaultNavigationLocation,
        int DisplayOrder,
        string? LegacyPath = null);

    public sealed record StorefrontPageTemplateStatusDto(
        string PageKey,
        string DisplayName,
        bool RequiredForReadiness,
        string DefaultSlug,
        string DefaultTitle,
        string Status,
        StorefrontPageSummaryDto? MappedPage,
        IReadOnlyList<StorefrontPageSummaryDto> SuggestedExistingPages);

    public sealed record CreatePageFromTemplateRequest(
        string? Slug = null,
        string? Title = null);

    public sealed record MapPageTemplateRequest(
        string PageKey);

    public sealed record UpdatePageNavigationRequest(
        int DisplayOrder,
        bool IncludeInNavigation,
        string? NavigationLocation);

    public sealed record StorefrontPageNavigationLinkDto(
        string PageKey,
        string Slug,
        string Title,
        string? NavigationLocation,
        int DisplayOrder);

    public static class StorefrontPageTemplateStatuses
    {
        public const string Missing = "missing";
        public const string MappedDraft = "mapped_draft";
        public const string MappedPublished = "mapped_published";
    }

    public interface IStorefrontPageTemplateService
    {
        IReadOnlyList<StorefrontPageTemplateDefinitionDto> ListDefinitions();

        Task<ServiceResponse<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStatusAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> CreateDraftFromTemplateAsync(
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> MapExistingPageAsync(
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> ClearPageKeyAsync(
            Guid pagePublicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StorefrontPageDetailDto>> UpdateNavigationAsync(
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<IReadOnlyList<StorefrontPageNavigationLinkDto>>> ListNavigationLinksAsync(
            CancellationToken cancellationToken = default);
    }
}
