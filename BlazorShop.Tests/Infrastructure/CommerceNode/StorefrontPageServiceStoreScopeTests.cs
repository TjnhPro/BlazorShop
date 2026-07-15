namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Moq;

    using Xunit;

    public sealed class StorefrontPageServiceStoreScopeTests
    {
        [Fact]
        public async Task ListAsync_ReturnsOnlyCurrentStorePages()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedPagesAsync(context, storeA, storeB);
            var service = CreateService(context, storeA);

            var result = await service.ListAsync(new StorefrontPageListQuery(PageNumber: 1, PageSize: 25));

            Assert.True(result.Success);
            Assert.Equal(2, result.Payload!.TotalCount);
            Assert.All(result.Payload.Items, page => Assert.Equal(storeA, page.StoreId));
            Assert.DoesNotContain(result.Payload.Items, page => page.Slug == "store-b-page");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNotFoundForOtherStorePage()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            var (_, storeBPageId) = await SeedPagesAsync(context, storeA, storeB);
            var service = CreateService(context, storeA);

            var result = await service.GetByIdAsync(storeBPageId);

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsNotFoundForOtherStorePage()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            var (_, storeBPageId) = await SeedPagesAsync(context, storeA, storeB);
            var service = CreateService(context, storeA);

            var result = await service.UpdateAsync(
                storeBPageId,
                new UpdateStorefrontPageRequest("store-b-page", "Updated", null, "<p>Updated</p>", true, true));

            var unchanged = await context.StorefrontPages.SingleAsync(page => page.Id == storeBPageId);

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal("Store B Page", unchanged.Title);
        }

        [Fact]
        public async Task ArchiveAsync_ReturnsNotFoundForOtherStorePage()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            var (_, storeBPageId) = await SeedPagesAsync(context, storeA, storeB);
            var service = CreateService(context, storeA);

            var result = await service.ArchiveAsync(storeBPageId);

            var unchanged = await context.StorefrontPages.SingleAsync(page => page.Id == storeBPageId);

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Null(unchanged.ArchivedAt);
            Assert.True(unchanged.IsPublished);
        }

        [Fact]
        public async Task GetPublishedBySlugAsync_ReturnsOnlyCurrentStorePublishedPage()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedPagesAsync(context, storeA, storeB);
            var service = CreateService(context, storeA);

            var current = await service.GetPublishedBySlugAsync("store-a-page");
            var other = await service.GetPublishedBySlugAsync("store-b-page");

            Assert.True(current.Success);
            Assert.Equal("Store A Page", current.Payload!.Title);
            Assert.False(other.Success);
            Assert.Equal(ServiceResponseType.NotFound, other.ResponseType);
        }

        [Fact]
        public async Task ListSitemapEntriesAsync_ReturnsOnlyCurrentStoreIncludedPages()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedPagesAsync(context, storeA, storeB);
            var service = CreateService(context, storeA);

            var result = await service.ListSitemapEntriesAsync();

            Assert.True(result.Success);
            Assert.Single(result.Payload!);
            Assert.Equal("store-a-page", result.Payload![0].Slug);
        }

        [Fact]
        public async Task CreateAsync_AllowsSlugThatExistsOnlyInAnotherStore()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedPagesAsync(context, storeA, storeB);
            var service = CreateService(context, storeA);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "store-b-page",
                "Shared Slug In Store A",
                null,
                "<p>Shared slug</p>",
                true,
                false));

            Assert.True(result.Success);
            Assert.Equal(storeA, result.Payload!.StoreId);
            Assert.Equal("store-b-page", result.Payload.Slug);
            Assert.Equal(2, await context.StorefrontPages.CountAsync(page => page.Slug == "store-b-page"));
        }

        [Fact]
        public async Task CreateAsync_PersistsPageContentMetadata()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeA);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "about-us",
                "About us",
                null,
                "<p>About</p>",
                true,
                true,
                PageKey: "about",
                DisplayOrder: 100,
                IncludeInNavigation: true,
                NavigationLocation: StorefrontPageContentRules.FooterCompany));

            Assert.True(result.Success);
            Assert.Equal("about", result.Payload!.PageKey);
            Assert.Equal(100, result.Payload.DisplayOrder);
            Assert.True(result.Payload.IncludeInNavigation);
            Assert.Equal(StorefrontPageContentRules.FooterCompany, result.Payload.NavigationLocation);
        }

        [Fact]
        public async Task CreateAsync_PublishedMappedLegacyPage_CreatesApprovedLegacyRedirect()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var redirects = new Mock<ISeoRedirectAutomationService>();
            redirects
                .Setup(service => service.EnsurePermanentRedirectAsync("/about-us", "/pages/about-us"))
                .ReturnsAsync(SuccessfulRedirect("/about-us", "/pages/about-us"));
            var service = CreateService(context, storeA, navigationCache: null, redirectAutomationService: redirects.Object);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "about-us",
                "About us",
                null,
                "<p>About</p>",
                true,
                true,
                PageKey: "about"));

            Assert.True(result.Success);
            redirects.Verify(service => service.EnsurePermanentRedirectAsync("/about-us", "/pages/about-us"), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_DraftMappedLegacyPage_DoesNotCreateLegacyRedirect()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var redirects = new Mock<ISeoRedirectAutomationService>();
            var service = CreateService(context, storeA, navigationCache: null, redirectAutomationService: redirects.Object);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "about-us",
                "About us",
                null,
                "<p>About</p>",
                false,
                true,
                PageKey: "about"));

            Assert.True(result.Success);
            redirects.Verify(service => service.EnsurePermanentRedirectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_UnknownLegacyPath_DoesNotCreateLegacyRedirect()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var redirects = new Mock<ISeoRedirectAutomationService>();
            var service = CreateService(context, storeA, navigationCache: null, redirectAutomationService: redirects.Object);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "custom-page",
                "Custom page",
                null,
                "<p>Custom</p>",
                true,
                true));

            Assert.True(result.Success);
            redirects.Verify(service => service.EnsurePermanentRedirectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_InvalidatesNavigationCacheForCurrentStore()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var page = CreatePage(storeA, "about-us", "About us", includeInSitemap: true, DateTimeOffset.UtcNow);
            context.StorefrontPages.Add(page);
            await context.SaveChangesAsync();
            var navigationCache = new Mock<IStorefrontNavigationCache>();
            var service = CreateService(context, storeA, navigationCache.Object);

            var result = await service.UpdateAsync(
                page.Id,
                new UpdateStorefrontPageRequest(
                    "company",
                    "Company",
                    null,
                    "<p>Company</p>",
                    true,
                    true));

            Assert.True(result.Success);
            navigationCache.Verify(cache => cache.Invalidate(storeA), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenSlugIsMissing_GeneratesSlugFromTitleAndRecordsHistory()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeA, navigationCache: null, enableSlugLifecycle: true);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                null,
                "About Us",
                null,
                "<p>About us</p>",
                true,
                true));

            Assert.True(result.Success);
            Assert.Equal("about-us", result.Payload!.Slug);

            var history = await context.StoreSeoSlugHistories.SingleAsync(item => item.EntityId == result.Payload.Id);
            Assert.Equal("about-us", history.Slug);
            Assert.True(history.IsActive);
        }

        [Fact]
        public async Task UpdateAsync_WhenPublishedSlugChanges_CreatesRedirectAndSlugHistory()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var page = CreatePage(storeA, "about-us", "About us", includeInSitemap: true, DateTimeOffset.UtcNow);
            context.StorefrontPages.Add(page);
            await context.SaveChangesAsync();
            var redirects = new Mock<ISeoRedirectAutomationService>();
            redirects
                .Setup(service => service.EnsurePermanentRedirectAsync("/pages/about-us", "/pages/company"))
                .ReturnsAsync(SuccessfulRedirect("/pages/about-us", "/pages/company"));
            var service = CreateService(
                context,
                storeA,
                navigationCache: null,
                enableSlugLifecycle: true,
                redirectAutomationService: redirects.Object);

            var result = await service.UpdateAsync(
                page.Id,
                new UpdateStorefrontPageRequest(
                    "company",
                    "Company",
                    null,
                    "<p>Company</p>",
                    true,
                    true));

            Assert.True(result.Success);
            redirects.Verify(service => service.EnsurePermanentRedirectAsync("/pages/about-us", "/pages/company"), Times.Once);

            var history = await context.StoreSeoSlugHistories
                .Where(item => item.EntityId == page.Id)
                .OrderBy(item => item.IsActive)
                .ToListAsync();
            Assert.Equal(2, history.Count);
            Assert.Contains(history, item => item.Slug == "about-us" && !item.IsActive && item.ReplacedBySlug == "company");
            Assert.Contains(history, item => item.Slug == "company" && item.IsActive);
        }

        [Fact]
        public async Task CreateAsync_RejectsUnknownPageKey()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeA);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "generic",
                "Generic",
                null,
                "<p>Generic</p>",
                PageKey: "generic"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
        }

        [Fact]
        public async Task CreateAsync_RejectsNavigationWithoutLocation()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeA);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "privacy",
                "Privacy",
                null,
                "<p>Privacy</p>",
                IncludeInNavigation: true));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
        }

        [Fact]
        public async Task CreateAsync_RejectsDuplicateActivePageKeyInCurrentStore()
        {
            var storeA = Guid.NewGuid();
            await using var context = CreateContext();
            context.StorefrontPages.Add(CreatePage(storeA, "about-us", "About us", includeInSitemap: true, DateTimeOffset.UtcNow, pageKey: "about"));
            await context.SaveChangesAsync();
            var service = CreateService(context, storeA);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "about-company",
                "About company",
                null,
                "<p>About company</p>",
                PageKey: "about"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
        }

        [Fact]
        public async Task CreateAsync_AllowsSamePageKeyInDifferentStore()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            context.StorefrontPages.Add(CreatePage(storeB, "about-us", "About us", includeInSitemap: true, DateTimeOffset.UtcNow, pageKey: "about"));
            await context.SaveChangesAsync();
            var service = CreateService(context, storeA);

            var result = await service.CreateAsync(new CreateStorefrontPageRequest(
                "about-us",
                "About us",
                null,
                "<p>About us</p>",
                PageKey: "about"));

            Assert.True(result.Success);
            Assert.Equal(storeA, result.Payload!.StoreId);
            Assert.Equal("about", result.Payload.PageKey);
        }

        private static StorefrontPageService CreateService(CommerceNodeDbContext context, Guid storeId)
        {
            return CreateService(context, storeId, navigationCache: null);
        }

        private static StorefrontPageService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            IStorefrontNavigationCache? navigationCache,
            bool enableSlugLifecycle = false,
            ISeoRedirectAutomationService? redirectAutomationService = null)
        {
            var audit = new Mock<IAdminAuditService>();
            audit
                .Setup(service => service.LogAsync(It.IsAny<CreateAdminAuditLogDto>()))
                .ReturnsAsync(new ServiceResponse<AdminAuditLogDto>(true, "Logged", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                });

            var slugService = new SlugService();
            IStoreSeoSlugPolicyService? slugPolicy = enableSlugLifecycle
                ? new StoreSeoSlugPolicyService(
                    slugService,
                    new IStoreSeoSlugCollisionChecker[] { new CommerceNodeStoreSeoSlugCollisionChecker(context) })
                : null;
            IStoreSeoSlugHistoryService? slugHistory = enableSlugLifecycle
                ? new StoreSeoSlugHistoryService(context)
                : null;

            return new StorefrontPageService(
                context,
                new FixedStoreContext(storeId),
                slugService,
                audit.Object,
                navigationCache,
                slugPolicy,
                slugHistory,
                redirectAutomationService);
        }

        private static ServiceResponse<BlazorShop.Application.DTOs.Seo.SeoRedirectDto> SuccessfulRedirect(
            string oldPath,
            string newPath)
        {
            return new ServiceResponse<BlazorShop.Application.DTOs.Seo.SeoRedirectDto>(true, "Created", Guid.NewGuid())
            {
                ResponseType = ServiceResponseType.Success,
                Payload = new BlazorShop.Application.DTOs.Seo.SeoRedirectDto
                {
                    OldPath = oldPath,
                    NewPath = newPath,
                    StatusCode = 301,
                    IsActive = true,
                },
            };
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-page-store-scope-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static async Task<(Guid StoreAPageId, Guid StoreBPageId)> SeedPagesAsync(
            CommerceNodeDbContext context,
            Guid storeA,
            Guid storeB)
        {
            var now = new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);
            var storeAPage = CreatePage(storeA, "store-a-page", "Store A Page", includeInSitemap: true, now);
            var storeADraft = CreatePage(storeA, "store-a-draft", "Store A Draft", includeInSitemap: false, now);
            storeADraft.IsPublished = false;
            var storeBPage = CreatePage(storeB, "store-b-page", "Store B Page", includeInSitemap: true, now);

            context.StorefrontPages.AddRange(storeAPage, storeADraft, storeBPage);
            await context.SaveChangesAsync();

            return (storeAPage.Id, storeBPage.Id);
        }

        private static StorefrontPage CreatePage(
            Guid storeId,
            string slug,
            string title,
            bool includeInSitemap,
            DateTimeOffset timestamp,
            string? pageKey = null)
        {
            return new StorefrontPage
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                Slug = slug,
                Title = title,
                BodyHtml = $"<p>{title}</p>",
                IsPublished = true,
                IncludeInSitemap = includeInSitemap,
                PageKey = pageKey,
                CreatedAt = timestamp,
                UpdatedAt = timestamp,
            };
        }

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Current store resolved.", this.storeId));
            }
        }
    }
}
