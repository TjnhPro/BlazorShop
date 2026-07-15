namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed partial class StorefrontPageService : IStorefrontPageService
    {
        private const int DefaultPageSize = 25;
        private const int MaxPageSize = 100;
        private const int MaxBodyHtmlLength = 100 * 1024;

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ISlugService slugService;
        private readonly IAdminAuditService auditService;

        public StorefrontPageService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ISlugService slugService,
            IAdminAuditService auditService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.slugService = slugService;
            this.auditService = auditService;
        }

        public async Task<ServiceResponse<StorefrontPageListResponse>> ListAsync(
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<StorefrontPageListResponse>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var page = NormalizePage(query.PageNumber, query.PageSize);
            var pages = this.context.StorefrontPages
                .AsNoTracking()
                .Where(item => item.StoreId == storeId && item.ArchivedAt == null);

            var status = NormalizeStatus(query.Status);
            if (status == StorefrontPageStatuses.Published)
            {
                pages = pages.Where(item => item.IsPublished);
            }
            else if (status == StorefrontPageStatuses.Draft)
            {
                pages = pages.Where(item => !item.IsPublished);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                pages = pages.Where(item => item.Title.Contains(search) || item.Slug.Contains(search));
            }

            var total = await pages.CountAsync(cancellationToken);
            var items = await pages
                .OrderByDescending(item => item.UpdatedAt)
                .ThenBy(item => item.Title)
                .Skip(page.Skip)
                .Take(page.PageSize)
                .Select(item => MapSummary(item))
                .ToListAsync(cancellationToken);

            return Success(
                new StorefrontPageListResponse(items, total, page.PageNumber, page.PageSize, TotalPages(total, page.PageSize)),
                "Storefront pages retrieved.");
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var page = await this.LoadPageAsync(id, asTracking: false, includeArchived: false, cancellationToken);
            return page is null
                ? Failure<StorefrontPageDetailDto>("Storefront page was not found.", ServiceResponseType.NotFound)
                : Success(MapDetail(page), "Storefront page retrieved.");
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> CreateAsync(
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<StorefrontPageDetailDto>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var normalized = this.NormalizeCreate(request);
            if (!normalized.Success)
            {
                return Failure<StorefrontPageDetailDto>(normalized.Message!, ServiceResponseType.ValidationError);
            }

            var duplicate = await this.context.StorefrontPages.AnyAsync(
                page => page.StoreId == storeId && page.Slug == normalized.Slug,
                cancellationToken);
            if (duplicate)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page slug already exists for this store.", ServiceResponseType.Conflict);
            }

            if (!string.IsNullOrWhiteSpace(normalized.PageKey))
            {
                var duplicatePageKey = await this.context.StorefrontPages.AnyAsync(
                    item =>
                        item.StoreId == storeId &&
                        item.PageKey == normalized.PageKey &&
                        item.ArchivedAt == null,
                    cancellationToken);
                if (duplicatePageKey)
                {
                    return Failure<StorefrontPageDetailDto>("Storefront page key already exists for this store.", ServiceResponseType.Conflict);
                }
            }

            var now = DateTimeOffset.UtcNow;
            var page = new StorefrontPage
            {
                StoreId = storeId.Value,
                Slug = normalized.Slug!,
                Title = normalized.Title!,
                Intro = normalized.Intro,
                BodyHtml = normalized.BodyHtml!,
                IsPublished = request.IsPublished,
                IncludeInSitemap = request.IncludeInSitemap,
                PageKey = normalized.PageKey,
                DisplayOrder = normalized.DisplayOrder,
                IncludeInNavigation = normalized.IncludeInNavigation,
                NavigationLocation = normalized.NavigationLocation,
                MetaTitle = normalized.Seo.MetaTitle,
                MetaDescription = normalized.Seo.MetaDescription,
                CanonicalUrl = normalized.Seo.CanonicalUrl,
                OgTitle = normalized.Seo.OgTitle,
                OgDescription = normalized.Seo.OgDescription,
                OgImage = normalized.Seo.OgImage,
                RobotsIndex = normalized.Seo.RobotsIndex,
                RobotsFollow = normalized.Seo.RobotsFollow,
                CreatedAt = now,
                UpdatedAt = now,
            };

            this.context.StorefrontPages.Add(page);
            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("StorefrontPage.Created", page.Id, "Storefront page created.", new { page.Title, page.Slug }, cancellationToken);

            return Success(MapDetail(page), "Storefront page created.");
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> UpdateAsync(
            Guid id,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var page = await this.LoadPageAsync(id, asTracking: true, includeArchived: false, cancellationToken);
            if (page is null)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page was not found.", ServiceResponseType.NotFound);
            }

            var normalized = this.NormalizeUpdate(request);
            if (!normalized.Success)
            {
                return Failure<StorefrontPageDetailDto>(normalized.Message!, ServiceResponseType.ValidationError);
            }

            var duplicate = await this.context.StorefrontPages.AnyAsync(
                item => item.StoreId == page.StoreId && item.Slug == normalized.Slug && item.Id != page.Id,
                cancellationToken);
            if (duplicate)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page slug already exists for this store.", ServiceResponseType.Conflict);
            }

            if (!string.IsNullOrWhiteSpace(normalized.PageKey))
            {
                var duplicatePageKey = await this.context.StorefrontPages.AnyAsync(
                    item =>
                        item.StoreId == page.StoreId &&
                        item.PageKey == normalized.PageKey &&
                        item.Id != page.Id &&
                        item.ArchivedAt == null,
                    cancellationToken);
                if (duplicatePageKey)
                {
                    return Failure<StorefrontPageDetailDto>("Storefront page key already exists for this store.", ServiceResponseType.Conflict);
                }
            }

            page.Slug = normalized.Slug!;
            page.Title = normalized.Title!;
            page.Intro = normalized.Intro;
            page.BodyHtml = normalized.BodyHtml!;
            page.IsPublished = request.IsPublished;
            page.IncludeInSitemap = request.IncludeInSitemap;
            page.PageKey = normalized.PageKey;
            page.DisplayOrder = normalized.DisplayOrder;
            page.IncludeInNavigation = normalized.IncludeInNavigation;
            page.NavigationLocation = normalized.NavigationLocation;
            page.MetaTitle = normalized.Seo.MetaTitle;
            page.MetaDescription = normalized.Seo.MetaDescription;
            page.CanonicalUrl = normalized.Seo.CanonicalUrl;
            page.OgTitle = normalized.Seo.OgTitle;
            page.OgDescription = normalized.Seo.OgDescription;
            page.OgImage = normalized.Seo.OgImage;
            page.RobotsIndex = normalized.Seo.RobotsIndex;
            page.RobotsFollow = normalized.Seo.RobotsFollow;
            page.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("StorefrontPage.Updated", page.Id, "Storefront page updated.", new { page.Title, page.Slug, page.IsPublished }, cancellationToken);

            return Success(MapDetail(page), "Storefront page updated.");
        }

        public async Task<ServiceResponse<StorefrontPageDetailDto>> ArchiveAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var page = await this.LoadPageAsync(id, asTracking: true, includeArchived: false, cancellationToken);
            if (page is null)
            {
                return Failure<StorefrontPageDetailDto>("Storefront page was not found.", ServiceResponseType.NotFound);
            }

            var now = DateTimeOffset.UtcNow;
            page.IsPublished = false;
            page.ArchivedAt = now;
            page.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("StorefrontPage.Archived", page.Id, "Storefront page archived.", new { page.Title, page.Slug }, cancellationToken);

            return Success(MapDetail(page), "Storefront page archived.");
        }

        public async Task<ServiceResponse<StorefrontPagePublicDto>> GetPublishedBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            var normalizedSlug = this.NormalizeSlug(slug);
            if (!storeId.HasValue || normalizedSlug is null)
            {
                return Failure<StorefrontPagePublicDto>("Storefront page was not found.", ServiceResponseType.NotFound);
            }

            var page = await this.context.StorefrontPages
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.StoreId == storeId &&
                        item.Slug == normalizedSlug &&
                        item.IsPublished &&
                        item.ArchivedAt == null,
                    cancellationToken);

            return page is null
                ? Failure<StorefrontPagePublicDto>("Storefront page was not found.", ServiceResponseType.NotFound)
                : Success(MapPublic(page), "Storefront page retrieved.");
        }

        public async Task<ServiceResponse<IReadOnlyList<StorefrontPageSitemapEntryDto>>> ListSitemapEntriesAsync(
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<IReadOnlyList<StorefrontPageSitemapEntryDto>>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var entries = await this.context.StorefrontPages
                .AsNoTracking()
                .Where(item =>
                    item.StoreId == storeId &&
                    item.IsPublished &&
                    item.IncludeInSitemap &&
                    item.ArchivedAt == null)
                .OrderByDescending(item => item.UpdatedAt)
                .Select(item => new StorefrontPageSitemapEntryDto(item.Slug, item.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Success<IReadOnlyList<StorefrontPageSitemapEntryDto>>(entries, "Storefront page sitemap entries retrieved.");
        }

        private async Task<StorefrontPage?> LoadPageAsync(
            Guid id,
            bool asTracking,
            bool includeArchived,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue || id == Guid.Empty)
            {
                return null;
            }

            var pages = asTracking ? this.context.StorefrontPages : this.context.StorefrontPages.AsNoTracking();
            return await pages.FirstOrDefaultAsync(
                page =>
                    page.StoreId == storeId &&
                    (page.Id == id || page.PublicId == id) &&
                    (includeArchived || page.ArchivedAt == null),
                cancellationToken);
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success && result.Payload != Guid.Empty ? result.Payload : null;
        }

        private NormalizedStorefrontPage NormalizeCreate(CreateStorefrontPageRequest request)
        {
            return this.Normalize(
                request.Slug,
                request.Title,
                request.Intro,
                request.BodyHtml,
                request.Seo,
                request.PageKey,
                request.DisplayOrder,
                request.IncludeInNavigation,
                request.NavigationLocation);
        }

        private NormalizedStorefrontPage NormalizeUpdate(UpdateStorefrontPageRequest request)
        {
            return this.Normalize(
                request.Slug,
                request.Title,
                request.Intro,
                request.BodyHtml,
                request.Seo,
                request.PageKey,
                request.DisplayOrder,
                request.IncludeInNavigation,
                request.NavigationLocation);
        }

        private NormalizedStorefrontPage Normalize(
            string? slug,
            string? title,
            string? intro,
            string? bodyHtml,
            StorefrontPageSeoDto? seo,
            string? pageKey,
            int displayOrder,
            bool includeInNavigation,
            string? navigationLocation)
        {
            var normalizedTitle = NormalizeRequired(title);
            if (normalizedTitle is null)
            {
                return NormalizedStorefrontPage.Failed("Page title is required.");
            }

            if (normalizedTitle.Length > 200)
            {
                return NormalizedStorefrontPage.Failed("Page title must be 200 characters or fewer.");
            }

            var normalizedSlug = this.NormalizeSlug(slug);
            if (normalizedSlug is null)
            {
                return NormalizedStorefrontPage.Failed("Page slug is required.");
            }

            if (normalizedSlug.Length > 160)
            {
                return NormalizedStorefrontPage.Failed("Page slug must be 160 characters or fewer.");
            }

            var normalizedIntro = NormalizeOptional(intro);
            if (normalizedIntro?.Length > 1000)
            {
                return NormalizedStorefrontPage.Failed("Page intro must be 1000 characters or fewer.");
            }

            var normalizedBody = bodyHtml?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedBody))
            {
                return NormalizedStorefrontPage.Failed("Page body HTML is required.");
            }

            if (normalizedBody.Length > MaxBodyHtmlLength)
            {
                return NormalizedStorefrontPage.Failed("Page body HTML must be 100 KB or less.");
            }

            var bodyValidation = ValidateBodyHtml(normalizedBody);
            if (bodyValidation is not null)
            {
                return NormalizedStorefrontPage.Failed(bodyValidation);
            }

            var normalizedSeo = NormalizeSeo(seo);
            var seoValidation = ValidateSeo(normalizedSeo);
            if (seoValidation is not null)
            {
                return NormalizedStorefrontPage.Failed(seoValidation);
            }

            var normalizedPageKey = NormalizePageKey(pageKey);
            if (normalizedPageKey.Invalid)
            {
                return NormalizedStorefrontPage.Failed("Page key is not supported.");
            }

            var normalizedNavigationLocation = NormalizeNavigationLocation(navigationLocation);
            if (normalizedNavigationLocation.Invalid)
            {
                return NormalizedStorefrontPage.Failed("Navigation location is not supported.");
            }

            if (includeInNavigation && normalizedNavigationLocation.Value is null)
            {
                return NormalizedStorefrontPage.Failed("Navigation location is required when the page is included in navigation.");
            }

            return NormalizedStorefrontPage.Succeeded(
                normalizedSlug,
                normalizedTitle,
                normalizedIntro,
                normalizedBody,
                normalizedSeo,
                normalizedPageKey.Value,
                displayOrder,
                includeInNavigation,
                normalizedNavigationLocation.Value);
        }

        private string? NormalizeSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = this.slugService.NormalizeSlug(value);
            return string.IsNullOrWhiteSpace(normalized) || !this.slugService.IsSlugSafe(normalized) || normalized.Contains('/')
                ? null
                : normalized;
        }

        private static StorefrontPageSeoDto NormalizeSeo(StorefrontPageSeoDto? seo)
        {
            return new StorefrontPageSeoDto(
                NormalizeOptional(seo?.MetaTitle),
                NormalizeOptional(seo?.MetaDescription),
                NormalizeOptional(seo?.CanonicalUrl),
                NormalizeOptional(seo?.OgTitle),
                NormalizeOptional(seo?.OgDescription),
                NormalizeOptional(seo?.OgImage),
                seo?.RobotsIndex ?? true,
                seo?.RobotsFollow ?? true);
        }

        private static string? ValidateSeo(StorefrontPageSeoDto seo)
        {
            if (seo.MetaTitle?.Length > 200)
            {
                return "Meta title must be 200 characters or fewer.";
            }

            if (seo.MetaDescription?.Length > 500)
            {
                return "Meta description must be 500 characters or fewer.";
            }

            if (seo.CanonicalUrl?.Length > 500)
            {
                return "Canonical URL must be 500 characters or fewer.";
            }

            if (seo.OgTitle?.Length > 200)
            {
                return "Open Graph title must be 200 characters or fewer.";
            }

            if (seo.OgDescription?.Length > 500)
            {
                return "Open Graph description must be 500 characters or fewer.";
            }

            if (seo.OgImage?.Length > 500)
            {
                return "Open Graph image URL must be 500 characters or fewer.";
            }

            return null;
        }

        private static NormalizedOptionalValue NormalizePageKey(string? value)
        {
            var normalized = NormalizeOptional(value);
            if (normalized is null)
            {
                return NormalizedOptionalValue.Valid(null);
            }

            normalized = normalized.ToLowerInvariant();
            return StorefrontPageContentRules.IsKnownPageKey(normalized)
                ? NormalizedOptionalValue.Valid(normalized)
                : NormalizedOptionalValue.InvalidValue();
        }

        private static NormalizedOptionalValue NormalizeNavigationLocation(string? value)
        {
            var normalized = NormalizeOptional(value);
            if (normalized is null)
            {
                return NormalizedOptionalValue.Valid(null);
            }

            normalized = normalized.ToLowerInvariant();
            return StorefrontPageContentRules.IsKnownNavigationLocation(normalized)
                ? NormalizedOptionalValue.Valid(normalized)
                : NormalizedOptionalValue.InvalidValue();
        }

        private static string? ValidateBodyHtml(string bodyHtml)
        {
            if (DangerousTagRegex().IsMatch(bodyHtml))
            {
                return "Page body HTML contains a disallowed tag.";
            }

            if (DangerousAttributeRegex().IsMatch(bodyHtml) || DangerousProtocolRegex().IsMatch(bodyHtml))
            {
                return "Page body HTML contains a disallowed attribute or URL protocol.";
            }

            foreach (Match match in ImageSourceRegex().Matches(bodyHtml))
            {
                var source = match.Groups["url"].Value.Trim();
                if (string.IsNullOrWhiteSpace(source) ||
                    source.StartsWith("//", StringComparison.Ordinal) ||
                    source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    !source.StartsWith("/", StringComparison.Ordinal))
                {
                    return "Page image URLs must use local paths.";
                }
            }

            return null;
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
                MapSeo(page),
                page.CreatedAt,
                page.UpdatedAt,
                page.PageKey,
                page.DisplayOrder,
                page.IncludeInNavigation,
                page.NavigationLocation);
        }

        private static StorefrontPagePublicDto MapPublic(StorefrontPage page)
        {
            return new StorefrontPagePublicDto(
                page.Slug,
                page.Title,
                page.Intro,
                page.BodyHtml,
                MapSeo(page),
                page.UpdatedAt);
        }

        private static StorefrontPageSeoDto MapSeo(StorefrontPage page)
        {
            return new StorefrontPageSeoDto(
                page.MetaTitle,
                page.MetaDescription,
                page.CanonicalUrl,
                page.OgTitle,
                page.OgDescription,
                page.OgImage,
                page.RobotsIndex,
                page.RobotsFollow);
        }

        private static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return StorefrontPageStatuses.All;
            }

            var value = status.Trim().ToLowerInvariant();
            return value is StorefrontPageStatuses.Published or StorefrontPageStatuses.Draft
                ? value
                : StorefrontPageStatuses.All;
        }

        private static PageWindow NormalizePage(int pageNumber, int pageSize)
        {
            var normalizedPageNumber = Math.Max(1, pageNumber);
            var normalizedPageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);
            return new PageWindow(normalizedPageNumber, normalizedPageSize);
        }

        private static int TotalPages(int totalCount, int pageSize)
        {
            return totalCount <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));
        }

        private static string? NormalizeRequired(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

        [GeneratedRegex("<\\s*(script|iframe|object|embed)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DangerousTagRegex();

        [GeneratedRegex("\\son[a-z]+\\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DangerousAttributeRegex();

        [GeneratedRegex("(javascript|data)\\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DangerousProtocolRegex();

        [GeneratedRegex("<img\\b[^>]*\\bsrc\\s*=\\s*(?:\"(?<url>[^\"]*)\"|'(?<url>[^']*)'|(?<url>[^\\s>]+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ImageSourceRegex();

        private readonly record struct PageWindow(int PageNumber, int PageSize)
        {
            public int Skip => (this.PageNumber - 1) * this.PageSize;
        }

        private sealed record NormalizedStorefrontPage(
            bool Success,
            string? Slug,
            string? Title,
            string? Intro,
            string? BodyHtml,
            StorefrontPageSeoDto Seo,
            string? PageKey,
            int DisplayOrder,
            bool IncludeInNavigation,
            string? NavigationLocation,
            string? Message = null)
        {
            public static NormalizedStorefrontPage Succeeded(
                string slug,
                string title,
                string? intro,
                string bodyHtml,
                StorefrontPageSeoDto seo,
                string? pageKey,
                int displayOrder,
                bool includeInNavigation,
                string? navigationLocation)
            {
                return new NormalizedStorefrontPage(
                    true,
                    slug,
                    title,
                    intro,
                    bodyHtml,
                    seo,
                    pageKey,
                    displayOrder,
                    includeInNavigation,
                    navigationLocation);
            }

            public static NormalizedStorefrontPage Failed(string message)
            {
                return new NormalizedStorefrontPage(
                    false,
                    null,
                    null,
                    null,
                    null,
                    new StorefrontPageSeoDto(null, null, null, null, null, null),
                    null,
                    0,
                    false,
                    null,
                    message);
            }
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
