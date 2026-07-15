namespace BlazorShop.Application.DTOs.Seo
{
    public static class SeoUrlResolutionStatuses
    {
        public const string Resolved = "resolved";
        public const string RedirectToCanonical = "redirect_to_canonical";
        public const string NotFound = "not_found";
        public const string Gone = "gone";
        public const string Invalid = "invalid";
    }

    public sealed record SeoUrlResolutionDto(
        string Status,
        int HttpStatusCode,
        bool RequiresRedirect,
        string RequestedPath,
        string? CanonicalPath,
        string? EntityType,
        Guid? EntityId,
        string? RequestedSlug,
        string? CanonicalSlug,
        string? LanguageCode);
}
