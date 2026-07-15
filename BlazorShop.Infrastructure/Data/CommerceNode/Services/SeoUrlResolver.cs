namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;

    public sealed class SeoUrlResolver : ISeoUrlResolver
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public SeoUrlResolver(CommerceNodeDbContext context, ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<SeoUrlResolutionDto> ResolvePublicPathAsync(
            string? path,
            CancellationToken cancellationToken = default)
        {
            var normalizedPath = NormalizePath(path);
            if (normalizedPath is null)
            {
                return Invalid(path ?? string.Empty);
            }

            var route = ParseRoute(normalizedPath);
            if (route is null)
            {
                return NotFound(normalizedPath);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload == Guid.Empty)
            {
                return NotFound(normalizedPath);
            }

            var history = await this.context.StoreSeoSlugHistories
                .AsNoTracking()
                .Where(item =>
                    item.StoreId == storeResult.Payload &&
                    item.EntityType == route.EntityType &&
                    item.Slug == route.Slug &&
                    item.LanguageCode == null)
                .OrderByDescending(item => item.IsActive)
                .ThenByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (history is null)
            {
                return NotFound(normalizedPath);
            }

            if (!await this.EntityIsPubliclyVisibleAsync(history.EntityType, history.EntityId, history.StoreId, cancellationToken))
            {
                return Gone(normalizedPath, history);
            }

            if (history.IsActive)
            {
                var canonicalPath = BuildCanonicalPath(history.EntityType, history.Slug);
                return new SeoUrlResolutionDto(
                    SeoUrlResolutionStatuses.Resolved,
                    StatusCodes.Status200OK,
                    RequiresRedirect: false,
                    normalizedPath,
                    canonicalPath,
                    history.EntityType,
                    history.EntityId,
                    route.Slug,
                    history.Slug,
                    history.LanguageCode);
            }

            var active = await this.FindActiveHistoryAsync(history.EntityType, history.EntityId, history.StoreId, history.LanguageCode, cancellationToken);
            if (active is null || !await this.EntityIsPubliclyVisibleAsync(active.EntityType, active.EntityId, active.StoreId, cancellationToken))
            {
                return Gone(normalizedPath, history);
            }

            var targetPath = BuildCanonicalPath(active.EntityType, active.Slug);
            return new SeoUrlResolutionDto(
                SeoUrlResolutionStatuses.RedirectToCanonical,
                StatusCodes.Status301MovedPermanently,
                RequiresRedirect: true,
                normalizedPath,
                targetPath,
                active.EntityType,
                active.EntityId,
                route.Slug,
                active.Slug,
                active.LanguageCode);
        }

        public async Task<SeoUrlResolutionDto> ResolveEntityCanonicalAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedEntityType = SeoSlugEntityTypes.Normalize(entityType);
            if (!SeoSlugEntityTypes.IsKnown(normalizedEntityType) || entityId == Guid.Empty || storeId == Guid.Empty)
            {
                return Invalid(string.Empty);
            }

            var normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            var active = await this.FindActiveHistoryAsync(normalizedEntityType, entityId, storeId, normalizedLanguageCode, cancellationToken);
            if (active is null)
            {
                return NotFound(string.Empty);
            }

            if (!await this.EntityIsPubliclyVisibleAsync(active.EntityType, active.EntityId, active.StoreId, cancellationToken))
            {
                return Gone(string.Empty, active);
            }

            var canonicalPath = BuildCanonicalPath(active.EntityType, active.Slug);
            return new SeoUrlResolutionDto(
                SeoUrlResolutionStatuses.Resolved,
                StatusCodes.Status200OK,
                RequiresRedirect: false,
                canonicalPath,
                canonicalPath,
                active.EntityType,
                active.EntityId,
                active.Slug,
                active.Slug,
                active.LanguageCode);
        }

        private Task<StoreSeoSlugHistory?> FindActiveHistoryAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode,
            CancellationToken cancellationToken)
        {
            return this.context.StoreSeoSlugHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.StoreId == storeId &&
                        item.EntityType == entityType &&
                        item.EntityId == entityId &&
                        item.LanguageCode == languageCode &&
                        item.IsActive,
                    cancellationToken);
        }

        private async Task<bool> EntityIsPubliclyVisibleAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            CancellationToken cancellationToken)
        {
            return entityType switch
            {
                SeoSlugEntityTypes.Product => await this.context.Products
                    .AsNoTracking()
                    .Include(product => product.Category)
                    .AnyAsync(
                        product =>
                            product.Id == entityId &&
                            product.StoreId == storeId &&
                            product.ArchivedAt == null &&
                            product.IsPublished &&
                            product.PublishedOn.HasValue &&
                            product.Category != null &&
                            product.Category.IsPublished &&
                            product.Category.ArchivedAt == null,
                        cancellationToken),
                SeoSlugEntityTypes.Category => await this.context.Categories
                    .AsNoTracking()
                    .AnyAsync(
                        category =>
                            category.Id == entityId &&
                            category.StoreId == storeId &&
                            category.ArchivedAt == null &&
                            category.IsPublished,
                        cancellationToken),
                SeoSlugEntityTypes.Page => await this.context.StorefrontPages
                    .AsNoTracking()
                    .AnyAsync(
                        page =>
                            page.Id == entityId &&
                            page.StoreId == storeId &&
                            page.ArchivedAt == null &&
                            page.IsPublished,
                        cancellationToken),
                _ => false,
            };
        }

        private static ParsedSeoRoute? ParseRoute(string path)
        {
            var trimmed = path.Trim('/');
            var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length != 2 || string.IsNullOrWhiteSpace(segments[1]))
            {
                return null;
            }

            var entityType = segments[0].ToLowerInvariant() switch
            {
                "product" => SeoSlugEntityTypes.Product,
                "category" => SeoSlugEntityTypes.Category,
                "pages" => SeoSlugEntityTypes.Page,
                _ => null,
            };

            return entityType is null ? null : new ParsedSeoRoute(entityType, segments[1]);
        }

        private static string? NormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var value = path.Trim();
            var queryIndex = value.IndexOfAny(['?', '#']);
            if (queryIndex >= 0)
            {
                value = value[..queryIndex];
            }

            if (!value.StartsWith("/", StringComparison.Ordinal))
            {
                value = $"/{value}";
            }

            return value == "/" ? null : value.TrimEnd('/');
        }

        private static string BuildCanonicalPath(string entityType, string slug)
        {
            return entityType switch
            {
                SeoSlugEntityTypes.Product => $"/product/{slug}",
                SeoSlugEntityTypes.Category => $"/category/{slug}",
                SeoSlugEntityTypes.Page => $"/pages/{slug}",
                _ => $"/{slug}",
            };
        }

        private static string? NormalizeLanguageCode(string? languageCode)
        {
            return string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();
        }

        private static SeoUrlResolutionDto Invalid(string requestedPath)
        {
            return new SeoUrlResolutionDto(
                SeoUrlResolutionStatuses.Invalid,
                StatusCodes.Status400BadRequest,
                RequiresRedirect: false,
                requestedPath,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static SeoUrlResolutionDto NotFound(string requestedPath)
        {
            return new SeoUrlResolutionDto(
                SeoUrlResolutionStatuses.NotFound,
                StatusCodes.Status404NotFound,
                RequiresRedirect: false,
                requestedPath,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static SeoUrlResolutionDto Gone(string requestedPath, StoreSeoSlugHistory history)
        {
            return new SeoUrlResolutionDto(
                SeoUrlResolutionStatuses.Gone,
                StatusCodes.Status410Gone,
                RequiresRedirect: false,
                requestedPath,
                null,
                history.EntityType,
                history.EntityId,
                history.Slug,
                null,
                history.LanguageCode);
        }

        private sealed record ParsedSeoRoute(string EntityType, string Slug);
    }
}
