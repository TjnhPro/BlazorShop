namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class StoreSeoSlugHistoryServiceTests
    {
        [Fact]
        public async Task RecordInitialActiveSlugAsync_CreatesActiveSlugRow()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = new StoreSeoSlugHistoryService(context);

            var result = await service.RecordInitialActiveSlugAsync(SeoSlugEntityTypes.Product, productId, storeId, "running-shoes");

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.NotNull(result.Payload);
            Assert.Equal("running-shoes", result.Payload!.Slug);
            Assert.True(result.Payload.IsActive);
        }

        [Fact]
        public async Task ReplaceActiveSlugAsync_RetainsOldSlugAndCreatesNewActiveSlug()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = new StoreSeoSlugHistoryService(context);
            await service.RecordInitialActiveSlugAsync(SeoSlugEntityTypes.Product, productId, storeId, "old-slug");

            var result = await service.ReplaceActiveSlugAsync(SeoSlugEntityTypes.Product, productId, storeId, "new-slug");
            var history = await service.ListHistoryAsync(SeoSlugEntityTypes.Product, productId, storeId);

            Assert.True(result.Success);
            Assert.Equal("new-slug", result.Payload!.Slug);
            Assert.Equal(2, history.Count);
            Assert.Contains(history, item => item.Slug == "old-slug" && !item.IsActive && item.ReplacedBySlug == "new-slug");
            Assert.Contains(history, item => item.Slug == "new-slug" && item.IsActive);
        }

        [Fact]
        public async Task RecordInitialActiveSlugAsync_RejectsSecondActiveSlugForSameEntityLanguage()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = new StoreSeoSlugHistoryService(context);
            await service.RecordInitialActiveSlugAsync(SeoSlugEntityTypes.Product, productId, storeId, "old-slug");

            var result = await service.RecordInitialActiveSlugAsync(SeoSlugEntityTypes.Product, productId, storeId, "new-slug");

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
        }

        [Fact]
        public async Task RecordInitialActiveSlugAsync_RejectsSecondActiveSlugForSameRouteFamilyStoreLanguage()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            var service = new StoreSeoSlugHistoryService(context);
            await service.RecordInitialActiveSlugAsync(SeoSlugEntityTypes.Category, Guid.NewGuid(), storeId, "sale");

            var result = await service.RecordInitialActiveSlugAsync(SeoSlugEntityTypes.Category, Guid.NewGuid(), storeId, "sale");

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
        }

        [Fact]
        public async Task BackfillCurrentSlugsAsync_IsIdempotent()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.Products.Add(new Product { Id = Guid.NewGuid(), StoreId = storeId, Slug = "product-one" });
            context.Categories.Add(new Category { Id = Guid.NewGuid(), StoreId = storeId, Slug = "category-one" });
            context.StorefrontPages.Add(new StorefrontPage { Id = Guid.NewGuid(), StoreId = storeId, Slug = "page-one", Title = "Page", BodyHtml = "<p>Page</p>" });
            await context.SaveChangesAsync();
            var service = new StoreSeoSlugHistoryService(context);

            var first = await service.BackfillCurrentSlugsAsync();
            var second = await service.BackfillCurrentSlugsAsync();

            Assert.True(first.Success);
            Assert.Equal(3, first.Payload!.Created);
            Assert.Equal(0, first.Payload.Skipped);
            Assert.True(second.Success);
            Assert.Equal(0, second.Payload!.Created);
            Assert.Equal(3, second.Payload.Skipped);
            Assert.Equal(3, await context.StoreSeoSlugHistories.CountAsync());
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-seo-slug-history-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
