namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
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

    public sealed class StorefrontPageTemplateServiceTests
    {
        [Fact]
        public void TemplateCatalog_ContainsRequiredContentKeysAndExcludesFunctionalRoutes()
        {
            var definitions = StorefrontPageTemplateCatalog.ListDefinitions();

            Assert.Contains(definitions, definition => definition.PageKey == "about" && definition.RequiredForReadiness);
            Assert.Contains(definitions, definition => definition.PageKey == "terms_conditions" && definition.RequiredForReadiness);
            Assert.DoesNotContain(definitions, definition => definition.PageKey is "generic" or "contact" or "cart" or "checkout" or "account" or "not_found");
        }

        [Fact]
        public async Task GetStatusAsync_MarksMissingTemplatesAndSuggestsExistingPages()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.StorefrontPages.Add(CreatePage(storeId, "about-us", "About us", pageKey: null, isPublished: true));
            context.StorefrontPages.Add(CreatePage(storeId, "custom-story", "Custom story", pageKey: null, isPublished: true));
            await context.SaveChangesAsync();
            var service = CreateTemplateService(context, storeId);

            var result = await service.GetStatusAsync();

            Assert.True(result.Success);
            var about = Assert.Single(result.Payload!, status => status.PageKey == "about");
            Assert.Equal(StorefrontPageTemplateStatuses.Missing, about.Status);
            Assert.Single(about.SuggestedExistingPages);
            Assert.Equal("about-us", about.SuggestedExistingPages[0].Slug);
            Assert.DoesNotContain(result.Payload!, status => status.SuggestedExistingPages.Any(page => page.Slug == "custom-story"));
        }

        [Fact]
        public async Task GetStatusAsync_MarksMappedDraftAndPublishedPages()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.StorefrontPages.Add(CreatePage(storeId, "about-us", "About us", "about", isPublished: true));
            context.StorefrontPages.Add(CreatePage(storeId, "privacy", "Privacy policy", "privacy_policy", isPublished: false));
            await context.SaveChangesAsync();
            var service = CreateTemplateService(context, storeId);

            var result = await service.GetStatusAsync();

            Assert.True(result.Success);
            Assert.Equal(StorefrontPageTemplateStatuses.MappedPublished, result.Payload!.Single(status => status.PageKey == "about").Status);
            Assert.Equal(StorefrontPageTemplateStatuses.MappedDraft, result.Payload!.Single(status => status.PageKey == "privacy_policy").Status);
        }

        [Fact]
        public async Task CreateDraftFromTemplateAsync_CreatesUnpublishedShellWithNoSitemapOrNavigation()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateTemplateService(context, storeId);

            var result = await service.CreateDraftFromTemplateAsync("shipping_information", new CreatePageFromTemplateRequest());

            Assert.True(result.Success);
            Assert.Equal("shipping_information", result.Payload!.PageKey);
            Assert.Equal("shipping", result.Payload.Slug);
            Assert.False(result.Payload.IsPublished);
            Assert.False(result.Payload.IncludeInSitemap);
            Assert.False(result.Payload.IncludeInNavigation);
            Assert.Null(result.Payload.NavigationLocation);
        }

        [Fact]
        public async Task MapExistingPageAsync_SetsPageKeyAndRejectsDuplicateActiveMapping()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var mapped = CreatePage(storeId, "about-us", "About us", "about", isPublished: true);
            var candidate = CreatePage(storeId, "about-company", "About company", pageKey: null, isPublished: true);
            context.StorefrontPages.AddRange(mapped, candidate);
            await context.SaveChangesAsync();
            var service = CreateTemplateService(context, storeId);

            var duplicate = await service.MapExistingPageAsync(candidate.PublicId, new MapPageTemplateRequest("about"));
            var mappedAfterClear = await service.ClearPageKeyAsync(mapped.PublicId);
            var mappedCandidate = await service.MapExistingPageAsync(candidate.PublicId, new MapPageTemplateRequest("about"));

            Assert.False(duplicate.Success);
            Assert.Equal(ServiceResponseType.Conflict, duplicate.ResponseType);
            Assert.True(mappedAfterClear.Success);
            Assert.True(mappedCandidate.Success);
            Assert.Equal("about", mappedCandidate.Payload!.PageKey);
            Assert.Equal(StorefrontPageContentRules.FooterCompany, mappedCandidate.Payload.NavigationLocation);
        }

        [Fact]
        public async Task ListNavigationLinksAsync_ReturnsPublishedNavigationPagesOnly()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.StorefrontPages.Add(CreatePage(storeId, "about-us", "About us", "about", isPublished: true, includeInNavigation: true, navigationLocation: StorefrontPageContentRules.FooterCompany, displayOrder: 100));
            context.StorefrontPages.Add(CreatePage(storeId, "privacy-draft", "Privacy policy", "privacy_policy", isPublished: false, includeInNavigation: true, navigationLocation: StorefrontPageContentRules.FooterLegal, displayOrder: 300));
            context.StorefrontPages.Add(CreatePage(storeId, "terms-hidden", "Terms", "terms_conditions", isPublished: true, includeInNavigation: false, navigationLocation: StorefrontPageContentRules.FooterLegal, displayOrder: 310));
            await context.SaveChangesAsync();
            var service = CreateTemplateService(context, storeId);

            var result = await service.ListNavigationLinksAsync();

            Assert.True(result.Success);
            var link = Assert.Single(result.Payload!);
            Assert.Equal("about", link.PageKey);
            Assert.Equal("about-us", link.Slug);
        }

        private static StorefrontPageTemplateService CreateTemplateService(CommerceNodeDbContext context, Guid storeId)
        {
            var audit = CreateAuditService();
            var slugService = new SlugService();
            var pageService = new StorefrontPageService(
                context,
                new FixedStoreContext(storeId),
                slugService,
                audit.Object,
                new NoopStorefrontNavigationCache(),
                new StoreSeoSlugPolicyService(slugService, new IStoreSeoSlugCollisionChecker[] { new CommerceNodeStoreSeoSlugCollisionChecker(context) }),
                new NoopStoreSeoSlugHistoryService(),
                new NoopSeoRedirectAutomationService());
            return new StorefrontPageTemplateService(context, new FixedStoreContext(storeId), pageService, audit.Object);
        }

        private static Mock<IAdminAuditService> CreateAuditService()
        {
            var audit = new Mock<IAdminAuditService>();
            audit
                .Setup(service => service.LogAsync(It.IsAny<CreateAdminAuditLogDto>()))
                .ReturnsAsync(new ServiceResponse<AdminAuditLogDto>(true, "Logged", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                });

            return audit;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-page-template-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static StorefrontPage CreatePage(
            Guid storeId,
            string slug,
            string title,
            string? pageKey,
            bool isPublished,
            bool includeInNavigation = false,
            string? navigationLocation = null,
            int displayOrder = 0)
        {
            var now = new DateTimeOffset(2026, 7, 15, 0, 0, 0, TimeSpan.Zero);
            return new StorefrontPage
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                Slug = slug,
                Title = title,
                BodyHtml = $"<p>{title}</p>",
                IsPublished = isPublished,
                IncludeInSitemap = isPublished,
                PageKey = pageKey,
                DisplayOrder = displayOrder,
                IncludeInNavigation = includeInNavigation,
                NavigationLocation = navigationLocation,
                CreatedAt = now,
                UpdatedAt = now,
            };
        }

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ApplicationResult<Guid>(true, "Current store resolved.", this.storeId));
            }
        }
    }
}
