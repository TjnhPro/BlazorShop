namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontPageTemplateService : IStorefrontPageTemplateService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IStorefrontPageService pageService;
        private readonly IAdminAuditService auditService;

        public StorefrontPageTemplateService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IStorefrontPageService pageService,
            IAdminAuditService auditService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.pageService = pageService;
            this.auditService = auditService;
        }

        public IReadOnlyList<StorefrontPageTemplateDefinitionDto> ListDefinitions()
        {
            return StorefrontPageTemplateCatalog.ListDefinitions();
        }

        public async Task<ServiceResponse<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<IReadOnlyList<StorefrontPageTemplateStatusDto>>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var pages = await this.context.StorefrontPages
                .AsNoTracking()
                .Where(page => page.StoreId == storeId && page.ArchivedAt == null)
                .OrderBy(page => page.DisplayOrder)
                .ThenBy(page => page.Title)
                .ToListAsync(cancellationToken);

            var statuses = StorefrontPageTemplateCatalog
                .ListDefinitions()
                .Select(definition => BuildStatus(definition, pages))
                .ToArray();

            return Success<IReadOnlyList<StorefrontPageTemplateStatusDto>>(statuses, "Storefront page template status retrieved.");
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> CreateDraftFromTemplateAsync(
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var definition = StorefrontPageTemplateCatalog.Find(pageKey);
            if (definition is null)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page template was not found.", ServiceResponseType.NotFound);
            }

            var result = await this.pageService.CreateAsync(
                new CreateStorefrontPageRequest(
                    request.Slug ?? definition.DefaultSlug,
                    request.Title ?? definition.DefaultTitle,
                    "Draft shell generated from the content template catalog.",
                    $"<p>Draft placeholder for {definition.DefaultTitle}. Replace this content before publishing.</p>",
                    IsPublished: false,
                    IncludeInSitemap: false,
                    PageKey: definition.PageKey,
                    DisplayOrder: definition.DisplayOrder,
                    IncludeInNavigation: false,
                    NavigationLocation: null),
                cancellationToken);

            return result.Success && result.Payload is not null
                ? Success(result.Payload, "Storefront page draft created from template.")
                : Failure<StorefrontPageDetailDto>(
                    string.IsNullOrWhiteSpace(result.Message) ? "Storefront page draft could not be created." : result.Message,
                    result.ResponseType);
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> MapExistingPageAsync(
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var definition = StorefrontPageTemplateCatalog.Find(request.PageKey);
            if (definition is null)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page template was not found.", ServiceResponseType.NotFound);
            }

            var page = await this.LoadPageAsync(pagePublicId, cancellationToken);
            if (page is null)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page was not found.", ServiceResponseType.NotFound);
            }

            var duplicate = await this.context.StorefrontPages.AnyAsync(
                item =>
                    item.StoreId == page.StoreId &&
                    item.PageKey == definition.PageKey &&
                    item.Id != page.Id &&
                    item.ArchivedAt == null,
                cancellationToken);
            if (duplicate)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page key already exists for this store.", ServiceResponseType.Conflict);
            }

            page.PageKey = definition.PageKey;
            if (page.DisplayOrder == 0)
            {
                page.DisplayOrder = definition.DisplayOrder;
            }

            page.NavigationLocation ??= definition.DefaultNavigationLocation;
            page.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("StorefrontPage.TemplateMapped", page.Id, "Storefront page template mapped.", new { page.Title, page.Slug, page.PageKey }, cancellationToken);

            return Success(MapDetail(page), "Storefront page template mapped.");
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> ClearPageKeyAsync(
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            var page = await this.LoadPageAsync(pagePublicId, cancellationToken);
            if (page is null)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page was not found.", ServiceResponseType.NotFound);
            }

            page.PageKey = null;
            page.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("StorefrontPage.TemplateCleared", page.Id, "Storefront page template mapping cleared.", new { page.Title, page.Slug }, cancellationToken);

            return Success(MapDetail(page), "Storefront page template mapping cleared.");
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> UpdateNavigationAsync(
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var normalizedLocation = NormalizeNavigationLocation(request.NavigationLocation);
            if (normalizedLocation.Invalid)
            {
                return Failure<StorefrontPageDetailDto>("Navigation location is not supported.", ServiceResponseType.ValidationError);
            }

            if (request.IncludeInNavigation && normalizedLocation.Value is null)
            {
                return Failure<StorefrontPageDetailDto>("Navigation location is required when the page is included in navigation.", ServiceResponseType.ValidationError);
            }

            var page = await this.LoadPageAsync(pagePublicId, cancellationToken);
            if (page is null)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page was not found.", ServiceResponseType.NotFound);
            }

            page.DisplayOrder = request.DisplayOrder;
            page.IncludeInNavigation = request.IncludeInNavigation;
            page.NavigationLocation = normalizedLocation.Value;
            page.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("StorefrontPage.NavigationUpdated", page.Id, "Storefront page navigation updated.", new { page.Title, page.Slug, page.DisplayOrder, page.IncludeInNavigation, page.NavigationLocation }, cancellationToken);

            return Success(MapDetail(page), "Storefront page navigation updated.");
        }

        public async Task<ServiceResponse<IReadOnlyList<StorefrontPageNavigationLinkDto>>> ListNavigationLinksAsync(
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<IReadOnlyList<StorefrontPageNavigationLinkDto>>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var links = await this.context.StorefrontPages
                .AsNoTracking()
                .Where(page =>
                    page.StoreId == storeId &&
                    page.PageKey != null &&
                    page.IsPublished &&
                    page.IncludeInNavigation &&
                    page.NavigationLocation != null &&
                    page.ArchivedAt == null)
                .OrderBy(page => page.NavigationLocation)
                .ThenBy(page => page.DisplayOrder)
                .ThenBy(page => page.Title)
                .Select(page => new StorefrontPageNavigationLinkDto(
                    page.PageKey!,
                    page.Slug,
                    page.Title,
                    page.NavigationLocation,
                    page.DisplayOrder))
                .ToListAsync(cancellationToken);

            return Success<IReadOnlyList<StorefrontPageNavigationLinkDto>>(links, "Storefront page navigation links retrieved.");
        }

        private async Task<StorefrontPage?> LoadPageAsync(Guid pagePublicId, CancellationToken cancellationToken)
        {
            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
            if (!storeId.HasValue || pagePublicId == Guid.Empty)
            {
                return null;
            }

            return await this.context.StorefrontPages.FirstOrDefaultAsync(
                page =>
                    page.StoreId == storeId &&
                    (page.PublicId == pagePublicId || page.Id == pagePublicId) &&
                    page.ArchivedAt == null,
                cancellationToken);
        }

        private static StorefrontPageTemplateStatusDto BuildStatus(
            StorefrontPageTemplateDefinitionDto definition,
            IReadOnlyList<StorefrontPage> pages)
        {
            var mapped = pages.FirstOrDefault(page => string.Equals(page.PageKey, definition.PageKey, StringComparison.OrdinalIgnoreCase));
            if (mapped is not null)
            {
                return new StorefrontPageTemplateStatusDto(
                    definition.PageKey,
                    definition.DisplayName,
                    definition.RequiredForReadiness,
                    definition.DefaultSlug,
                    definition.DefaultTitle,
                    mapped.IsPublished ? StorefrontPageTemplateStatuses.MappedPublished : StorefrontPageTemplateStatuses.MappedDraft,
                    MapSummary(mapped),
                    []);
            }

            var suggestions = pages
                .Where(page =>
                    page.PageKey is null &&
                    (string.Equals(page.Slug, definition.DefaultSlug, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(page.Title, definition.DefaultTitle, StringComparison.OrdinalIgnoreCase)))
                .Select(MapSummary)
                .ToArray();

            return new StorefrontPageTemplateStatusDto(
                definition.PageKey,
                definition.DisplayName,
                definition.RequiredForReadiness,
                definition.DefaultSlug,
                definition.DefaultTitle,
                StorefrontPageTemplateStatuses.Missing,
                null,
                suggestions);
        }

        private static NormalizedOptionalValue NormalizeNavigationLocation(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return NormalizedOptionalValue.Valid(null);
            }

            var normalized = value.Trim().ToLowerInvariant();
            return StorefrontPageContentRules.IsKnownNavigationLocation(normalized)
                ? NormalizedOptionalValue.Valid(normalized)
                : NormalizedOptionalValue.InvalidValue();
        }

        private async Task LogAsync(string action, Guid entityId, string summary, object metadata, CancellationToken cancellationToken)
        {
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = "StorefrontPage",
                EntityId = entityId.ToString(),
                Summary = summary,
                MetadataJson = JsonSerializer.Serialize(metadata),
            });
        }

        private static StorefrontPageSummaryDto MapSummary(StorefrontPage page)
        {
            return new StorefrontPageSummaryDto(
                page.Id,
                page.PublicId,
                page.StoreId,
                page.Slug,
                page.Title,
                page.Intro,
                page.IsPublished,
                page.IncludeInSitemap,
                page.CreatedAt,
                page.UpdatedAt,
                page.PageKey,
                page.DisplayOrder,
                page.IncludeInNavigation,
                page.NavigationLocation);
        }

        private static StorefrontPageDetailDto MapDetail(StorefrontPage page)
        {
            return new StorefrontPageDetailDto(
                page.Id,
                page.PublicId,
                page.StoreId,
                page.Slug,
                page.Title,
                page.Intro,
                page.BodyHtml,
                page.IsPublished,
                page.IncludeInSitemap,
                new StorefrontPageSeoDto(
                    page.MetaTitle,
                    page.MetaDescription,
                    page.CanonicalUrl,
                    page.OgTitle,
                    page.OgDescription,
                    page.OgImage,
                    page.RobotsIndex,
                    page.RobotsFollow),
                page.CreatedAt,
                page.UpdatedAt,
                page.PageKey,
                page.DisplayOrder,
                page.IncludeInNavigation,
                page.NavigationLocation);
        }

        private static ServiceResponse<TPayload> Success<TPayload>(TPayload payload, string message)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failure<TPayload>(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private sealed record NormalizedOptionalValue(bool Invalid, string? Value)
        {
            public static NormalizedOptionalValue Valid(string? value)
            {
                return new NormalizedOptionalValue(false, value);
            }

            public static NormalizedOptionalValue InvalidValue()
            {
                return new NormalizedOptionalValue(true, null);
            }
        }
    }
}
