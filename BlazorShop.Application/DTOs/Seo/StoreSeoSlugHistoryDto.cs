namespace BlazorShop.Application.DTOs.Seo
{
    public sealed record StoreSeoSlugHistoryDto(
        Guid Id,
        Guid StoreId,
        string EntityType,
        Guid EntityId,
        string Slug,
        string? LanguageCode,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReplacedAt,
        string? ReplacedBySlug);

    public sealed record StoreSeoSlugBackfillResultDto(
        int Created,
        int Skipped);
}
