namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services;
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
        private readonly IStorefrontNavigationCache navigationCache;
        private readonly IStoreSeoSlugPolicyService slugPolicyService;
        private readonly IStoreSeoSlugHistoryService slugHistoryService;
        private readonly ISeoRedirectAutomationService seoRedirectAutomationService;

        public StorefrontPageService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ISlugService slugService,
            IAdminAuditService auditService,
            IStorefrontNavigationCache navigationCache,
            IStoreSeoSlugPolicyService slugPolicyService,
            IStoreSeoSlugHistoryService slugHistoryService,
            ISeoRedirectAutomationService seoRedirectAutomationService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.slugService = slugService;
            this.auditService = auditService;
            this.navigationCache = navigationCache;
            this.slugPolicyService = slugPolicyService;
            this.slugHistoryService = slugHistoryService;
            this.seoRedirectAutomationService = seoRedirectAutomationService;
        }

        public async Task<ServiceResponse<StorefrontPageListResponse>> ListAsync(
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
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

            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<StorefrontPageDetailDto>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var normalized = this.NormalizeCreate(request);
            if (!normalized.Success)
            {
                return Failure<StorefrontPageDetailDto>(normalized.Message!, ServiceResponseType.ValidationError);
            }

            var slugResult = await this.ResolveCreateSlugAsync(normalized, request.Slug, storeId.Value, cancellationToken);
            if (!slugResult.Success)
            {
                return Failure<StorefrontPageDetailDto>(slugResult.Message!, slugResult.ResponseType);
            }

            normalized = normalized with { Slug = slugResult.Slug };

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
            var historyResult = await this.EnsureSlugHistoryAsync(page.StoreId, page.Id, oldSlug: null, page.Slug, cancellationToken);
            if (!historyResult.Success)
            {
                return Failure<StorefrontPageDetailDto>(historyResult.Message!, historyResult.ResponseType);
            }

            await this.context.SaveChangesAsync(cancellationToken);
            this.navigationCache.Invalidate(page.StoreId);
            var legacyRedirectResult = await this.EnsureApprovedLegacyPageRedirectAsync(page);
            if (!legacyRedirectResult.Success)
            {
                return Failure<StorefrontPageDetailDto>(legacyRedirectResult.Message!, legacyRedirectResult.ResponseType);
            }

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

            var slugResult = await this.ValidateUpdateSlugAsync(normalized.Slug!, page.StoreId, page.Id, cancellationToken);
            if (!slugResult.Success)
            {
                return Failure<StorefrontPageDetailDto>(slugResult.Message!, slugResult.ResponseType);
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

            var oldSlug = page.Slug;
            var oldPublicPath = BuildPagePublicPath(page.Slug, page.IsPublished);
            var newPublicPath = BuildPagePublicPath(normalized.Slug, request.IsPublished);

            var redirectResult = await this.EnsureRedirectAsync(oldPublicPath, newPublicPath);
            if (!redirectResult.Success)
            {
                return Failure<StorefrontPageDetailDto>(redirectResult.Message!, redirectResult.ResponseType);
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

            var historyResult = await this.EnsureSlugHistoryAsync(page.StoreId, page.Id, oldSlug, page.Slug, cancellationToken);
            if (!historyResult.Success)
            {
                return Failure<StorefrontPageDetailDto>(historyResult.Message!, historyResult.ResponseType);
            }

            await this.context.SaveChangesAsync(cancellationToken);
            this.navigationCache.Invalidate(page.StoreId);
            var legacyRedirectResult = await this.EnsureApprovedLegacyPageRedirectAsync(page);
            if (!legacyRedirectResult.Success)
            {
                return Failure<StorefrontPageDetailDto>(legacyRedirectResult.Message!, legacyRedirectResult.ResponseType);
            }

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
            this.navigationCache.Invalidate(page.StoreId);
            await this.LogAsync("StorefrontPage.Archived", page.Id, "Storefront page archived.", new { page.Title, page.Slug }, cancellationToken);

            return Success(MapDetail(page), "Storefront page archived.");
        }

        public async Task<ServiceResponse<StorefrontPagePublicDto>> GetPublishedBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
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
            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
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
            var storeId = await this.storeContext.GetCurrentStoreIdOrDefaultAsync(cancellationToken);
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

        private NormalizedStorefrontPage NormalizeCreate(CreateStorefrontPageRequest request)
        {
            return this.Normalize(
                string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug,
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

        private async Task<SlugPolicyOutcome> ResolveCreateSlugAsync(
            NormalizedStorefrontPage normalized,
            string? requestedSlug,
            Guid storeId,
            CancellationToken cancellationToken)
        {
            var result = string.IsNullOrWhiteSpace(requestedSlug)
                ? await this.slugPolicyService.GenerateSlugAsync(SeoSlugEntityTypes.Page, normalized.Title, storeId, excludedEntityId: null, cancellationToken: cancellationToken)
                : await this.slugPolicyService.ValidateSlugAsync(SeoSlugEntityTypes.Page, normalized.Slug, storeId, excludedEntityId: null, cancellationToken: cancellationToken);

            return ToPageSlugOutcome(result);
        }

        private async Task<SlugPolicyOutcome> ValidateUpdateSlugAsync(
            string slug,
            Guid storeId,
            Guid pageId,
            CancellationToken cancellationToken)
        {
            return ToPageSlugOutcome(await this.slugPolicyService.ValidateSlugAsync(
                SeoSlugEntityTypes.Page,
                slug,
                storeId,
                excludedEntityId: pageId,
                cancellationToken: cancellationToken));
        }

        private async Task<SlugPolicyOutcome> EnsureSlugHistoryAsync(
            Guid storeId,
            Guid pageId,
            string? oldSlug,
            string newSlug,
            CancellationToken cancellationToken)
        {
            var active = await this.slugHistoryService.GetActiveSlugAsync(SeoSlugEntityTypes.Page, pageId, storeId, cancellationToken: cancellationToken);
            if (active is null && !string.IsNullOrWhiteSpace(oldSlug))
            {
                var initial = await this.slugHistoryService.RecordInitialActiveSlugAsync(
                    SeoSlugEntityTypes.Page,
                    pageId,
                    storeId,
                    oldSlug,
                    cancellationToken: cancellationToken);
                if (!initial.Success)
                {
                    return SlugPolicyOutcome.Failed(
                        initial.Message ?? "Storefront page slug history could not be recorded.",
                        initial.ResponseType);
                }
            }

            var replacement = await this.slugHistoryService.ReplaceActiveSlugAsync(
                SeoSlugEntityTypes.Page,
                pageId,
                storeId,
                newSlug,
                cancellationToken: cancellationToken);

            return replacement.Success
                ? SlugPolicyOutcome.Succeeded(replacement.Payload?.Slug ?? newSlug)
                : SlugPolicyOutcome.Failed(
                    replacement.Message ?? "Storefront page slug history could not be updated.",
                    replacement.ResponseType);
        }

        private async Task<SlugPolicyOutcome> EnsureRedirectAsync(string? oldPublicPath, string? newPublicPath)
        {
            if (string.IsNullOrWhiteSpace(oldPublicPath) ||
                string.IsNullOrWhiteSpace(newPublicPath) ||
                PathsEqual(oldPublicPath, newPublicPath))
            {
                return SlugPolicyOutcome.Succeeded(newPublicPath ?? string.Empty);
            }

            var redirect = await this.seoRedirectAutomationService.EnsurePermanentRedirectAsync(oldPublicPath, newPublicPath);
            return redirect.Success
                ? SlugPolicyOutcome.Succeeded(newPublicPath)
                : SlugPolicyOutcome.Failed(redirect.Message ?? "Storefront page redirect could not be created.", redirect.ResponseType);
        }

        private async Task<SlugPolicyOutcome> EnsureApprovedLegacyPageRedirectAsync(StorefrontPage page)
        {
            if (!page.IsPublished || page.ArchivedAt is not null)
            {
                return SlugPolicyOutcome.Succeeded(page.Slug);
            }

            var legacyPath = ResolveApprovedLegacyPath(page.PageKey, page.Slug);
            var canonicalPath = BuildPagePublicPath(page.Slug, page.IsPublished);
            if (string.IsNullOrWhiteSpace(legacyPath) ||
                string.IsNullOrWhiteSpace(canonicalPath) ||
                PathsEqual(legacyPath, canonicalPath))
            {
                return SlugPolicyOutcome.Succeeded(page.Slug);
            }

            var redirect = await this.seoRedirectAutomationService.EnsurePermanentRedirectAsync(legacyPath, canonicalPath);
            return redirect.Success
                ? SlugPolicyOutcome.Succeeded(page.Slug)
                : SlugPolicyOutcome.Failed(redirect.Message ?? "Legacy storefront page redirect could not be created.", redirect.ResponseType);
        }

        private static SlugPolicyOutcome ToPageSlugOutcome(StoreSeoSlugPolicyResult result)
        {
            if (result.Success)
            {
                return SlugPolicyOutcome.Succeeded(result.Slug!);
            }

            return string.Equals(result.Message, "Slug is already in use.", StringComparison.Ordinal)
                ? SlugPolicyOutcome.Failed("Storefront page slug already exists for this store.", ServiceResponseType.Conflict)
                : SlugPolicyOutcome.Failed(result.Message ?? "Storefront page slug is invalid.", ServiceResponseType.ValidationError);
        }

        private static string? BuildPagePublicPath(string? slug, bool isPublished)
        {
            return isPublished && !string.IsNullOrWhiteSpace(slug)
                ? $"/pages/{slug}"
                : null;
        }

        private static string? ResolveApprovedLegacyPath(string? pageKey, string? slug)
        {
            var templateLegacyPath = StorefrontPageTemplateCatalog.FindLegacyPath(pageKey);
            if (!string.IsNullOrWhiteSpace(templateLegacyPath))
            {
                return templateLegacyPath;
            }

            return slug?.Trim().ToLowerInvariant() switch
            {
                "faq" => "/faq",
                "customer-service" => "/customer-service",
                _ => null,
            };
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(left.TrimEnd('/'), right.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
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

        private sealed record SlugPolicyOutcome(
            bool Success,
            string? Slug,
            ServiceResponseType ResponseType,
            string? Message = null)
        {
            public static SlugPolicyOutcome Succeeded(string slug)
            {
                return new SlugPolicyOutcome(true, slug, ServiceResponseType.Success);
            }

            public static SlugPolicyOutcome Failed(string message, ServiceResponseType responseType)
            {
                return new SlugPolicyOutcome(false, null, responseType, message);
            }
        }
    }
}
