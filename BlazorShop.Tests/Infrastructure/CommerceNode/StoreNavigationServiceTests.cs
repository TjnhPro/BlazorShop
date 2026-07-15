namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    using Moq;

    using Xunit;

    public sealed class StoreNavigationServiceTests
    {
        [Fact]
        public async Task CreateMenuAsync_RejectsUnknownSystemNameAndDuplicateActiveMenu()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeId);

            var unknown = await service.CreateMenuAsync(new CreateStoreNavigationMenuRequest("mega", "Mega"));
            var created = await service.CreateMenuAsync(new CreateStoreNavigationMenuRequest(StoreNavigationMenuNames.Main, "Main"));
            var duplicate = await service.CreateMenuAsync(new CreateStoreNavigationMenuRequest(StoreNavigationMenuNames.Main, "Main duplicate"));

            Assert.False(unknown.Success);
            Assert.Equal(ServiceResponseType.ValidationError, unknown.ResponseType);
            Assert.True(created.Success);
            Assert.False(duplicate.Success);
            Assert.Equal(ServiceResponseType.Conflict, duplicate.ResponseType);
        }

        [Fact]
        public async Task CreateItemAsync_RejectsNonHttpsExternalUrl()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeId);
            var menu = await service.CreateMenuAsync(new CreateStoreNavigationMenuRequest(StoreNavigationMenuNames.Main, "Main"));

            var result = await service.CreateItemAsync(
                menu.Payload!.PublicId,
                new CreateStoreNavigationMenuItemRequest(
                    null,
                    "Vendor",
                    StoreNavigationTargetTypes.ExternalUrl,
                    null,
                    null,
                    "http://example.test"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
        }

        [Fact]
        public async Task UpdateItemAsync_RejectsParentCycle()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeId);
            var menu = await service.CreateMenuAsync(new CreateStoreNavigationMenuRequest(StoreNavigationMenuNames.Main, "Main"));
            var parent = await service.CreateItemAsync(
                menu.Payload!.PublicId,
                SystemItem("Shop", StoreNavigationSystemTargets.Home));
            var child = await service.CreateItemAsync(
                menu.Payload.PublicId,
                SystemItem("Deals", StoreNavigationSystemTargets.TodaysDeals, parent.Payload!.Items.Single().PublicId));

            var cycle = await service.UpdateItemAsync(
                parent.Payload!.Items.Single().PublicId,
                new UpdateStoreNavigationMenuItemRequest(
                    Flatten(child.Payload!.Items).Single(item => item.Label == "Deals").PublicId,
                    "Shop",
                    StoreNavigationTargetTypes.System,
                    StoreNavigationSystemTargets.Home,
                    null,
                    null));

            Assert.False(cycle.Success);
            Assert.Equal(ServiceResponseType.ValidationError, cycle.ResponseType);
        }

        [Fact]
        public async Task GetPublicMenuAsync_ProjectsOnlyValidCurrentStorePublishedTargets()
        {
            var storeId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            await using var context = CreateContext();
            var page = CreatePage(storeId, "about-us", "About us", isPublished: true);
            var draftPage = CreatePage(storeId, "draft", "Draft", isPublished: false);
            var otherStoreCategory = CreateCategory(otherStoreId, "other-store", isPublished: true);
            context.StorefrontPages.AddRange(page, draftPage);
            context.Categories.Add(otherStoreCategory);
            var menu = CreateMenu(storeId, StoreNavigationMenuNames.Main);
            context.StoreNavigationMenus.Add(menu);
            context.StoreNavigationMenuItems.AddRange(
                CreateItem(storeId, menu.Id, "Home", StoreNavigationTargetTypes.System, StoreNavigationSystemTargets.Home),
                CreateItem(storeId, menu.Id, "About", StoreNavigationTargetTypes.Page, targetEntityPublicId: page.PublicId, displayOrder: 10),
                CreateItem(storeId, menu.Id, "Draft", StoreNavigationTargetTypes.Page, targetEntityPublicId: draftPage.PublicId, displayOrder: 20),
                CreateItem(storeId, menu.Id, "Other category", StoreNavigationTargetTypes.Category, targetEntityPublicId: otherStoreCategory.Id, displayOrder: 30));
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var result = await service.GetPublicMenuAsync(StoreNavigationMenuNames.Main);

            Assert.True(result.Success);
            Assert.Collection(
                result.Payload!.Items,
                item =>
                {
                    Assert.Equal("Home", item.Label);
                    Assert.Equal("/", item.Href);
                },
                item =>
                {
                    Assert.Equal("About", item.Label);
                    Assert.Equal("/pages/about-us", item.Href);
                });
        }

        [Fact]
        public async Task CreateItemAsync_InvalidatesPublicMenuCache()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = CreateService(context, storeId);
            var menu = await service.CreateMenuAsync(new CreateStoreNavigationMenuRequest(StoreNavigationMenuNames.Main, "Main"));

            var empty = await service.GetPublicMenuAsync(StoreNavigationMenuNames.Main);
            await service.CreateItemAsync(menu.Payload!.PublicId, SystemItem("Home", StoreNavigationSystemTargets.Home));
            var updated = await service.GetPublicMenuAsync(StoreNavigationMenuNames.Main);

            Assert.True(empty.Success);
            Assert.Empty(empty.Payload!.Items);
            Assert.True(updated.Success);
            Assert.Single(updated.Payload!.Items);
            Assert.Equal("Home", updated.Payload.Items[0].Label);
        }

        [Fact]
        public async Task GetPublicMenuAsync_UsesNewPageSlugAfterPageServiceInvalidatesCache()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var page = CreatePage(storeId, "about-us", "About us", isPublished: true);
            var menu = CreateMenu(storeId, StoreNavigationMenuNames.Main);
            context.StorefrontPages.Add(page);
            context.StoreNavigationMenus.Add(menu);
            context.StoreNavigationMenuItems.Add(CreateItem(
                storeId,
                menu.Id,
                "About",
                StoreNavigationTargetTypes.Page,
                targetEntityPublicId: page.PublicId));
            await context.SaveChangesAsync();

            var cache = new StorefrontNavigationCache(new MemoryCache(new MemoryCacheOptions()));
            var navigation = CreateService(context, storeId, cache);
            var pages = new StorefrontPageService(
                context,
                new FixedStoreContext(storeId),
                new SlugService(),
                CreateAuditService().Object,
                cache);

            var before = await navigation.GetPublicMenuAsync(StoreNavigationMenuNames.Main);
            var updated = await pages.UpdateAsync(
                page.Id,
                new UpdateStorefrontPageRequest(
                    "company",
                    "About us",
                    null,
                    "<p>About us</p>",
                    IsPublished: true,
                    IncludeInSitemap: true));
            var after = await navigation.GetPublicMenuAsync(StoreNavigationMenuNames.Main);

            Assert.True(before.Success);
            Assert.Equal("/pages/about-us", before.Payload!.Items.Single().Href);
            Assert.True(updated.Success);
            Assert.True(after.Success);
            Assert.Equal("/pages/company", after.Payload!.Items.Single().Href);
        }

        private static StoreNavigationService CreateService(CommerceNodeDbContext context, Guid storeId)
        {
            return CreateService(
                context,
                storeId,
                new StorefrontNavigationCache(new MemoryCache(new MemoryCacheOptions())));
        }

        private static StoreNavigationService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            IStorefrontNavigationCache cache)
        {
            return new StoreNavigationService(
                context,
                new FixedStoreContext(storeId),
                cache,
                CreateAuditService().Object);
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
                .UseInMemoryDatabase($"store-navigation-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static StoreNavigationMenu CreateMenu(Guid storeId, string systemName)
        {
            return new StoreNavigationMenu
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                SystemName = systemName,
                DisplayName = systemName,
                IsEnabled = true,
            };
        }

        private static StoreNavigationMenuItem CreateItem(
            Guid storeId,
            Guid menuId,
            string label,
            string targetType,
            string? targetKey = null,
            Guid? targetEntityPublicId = null,
            int displayOrder = 0)
        {
            return new StoreNavigationMenuItem
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                MenuId = menuId,
                Label = label,
                TargetType = targetType,
                TargetKey = targetKey,
                TargetEntityPublicId = targetEntityPublicId,
                IsEnabled = true,
                DisplayOrder = displayOrder,
            };
        }

        private static CreateStoreNavigationMenuItemRequest SystemItem(
            string label,
            string targetKey,
            Guid? parentItemPublicId = null)
        {
            return new CreateStoreNavigationMenuItemRequest(
                parentItemPublicId,
                label,
                StoreNavigationTargetTypes.System,
                targetKey,
                null,
                null);
        }

        private static IEnumerable<StoreNavigationMenuItemAdminDto> Flatten(IEnumerable<StoreNavigationMenuItemAdminDto> items)
        {
            foreach (var item in items)
            {
                yield return item;
                foreach (var child in Flatten(item.Children))
                {
                    yield return child;
                }
            }
        }

        private static StorefrontPage CreatePage(Guid storeId, string slug, string title, bool isPublished)
        {
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
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
        }

        private static Category CreateCategory(Guid storeId, string slug, bool isPublished)
        {
            return new Category
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Name = slug,
                Slug = slug,
                IsPublished = isPublished,
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
