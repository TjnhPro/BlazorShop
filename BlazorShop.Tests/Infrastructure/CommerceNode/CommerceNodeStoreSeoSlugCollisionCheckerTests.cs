namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CommerceNodeStoreSeoSlugCollisionCheckerTests
    {
        [Fact]
        public async Task SlugExistsAsync_ScopesProductCategoryAndPageToStore()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            context.Products.Add(new Product { Id = Guid.NewGuid(), StoreId = storeA, Slug = "shared-product" });
            context.Categories.Add(new Category { Id = Guid.NewGuid(), StoreId = storeA, Slug = "shared-category" });
            context.StorefrontPages.Add(new StorefrontPage { Id = Guid.NewGuid(), StoreId = storeA, Slug = "shared-page", Title = "Page", BodyHtml = "<p>Page</p>" });
            await context.SaveChangesAsync();
            var checker = new CommerceNodeStoreSeoSlugCollisionChecker(context);

            Assert.True(await checker.SlugExistsAsync(SeoSlugEntityTypes.Product, "shared-product", storeA));
            Assert.True(await checker.SlugExistsAsync(SeoSlugEntityTypes.Category, "shared-category", storeA));
            Assert.True(await checker.SlugExistsAsync(SeoSlugEntityTypes.Page, "shared-page", storeA));
            Assert.False(await checker.SlugExistsAsync(SeoSlugEntityTypes.Product, "shared-product", storeB));
            Assert.False(await checker.SlugExistsAsync(SeoSlugEntityTypes.Category, "shared-category", storeB));
            Assert.False(await checker.SlugExistsAsync(SeoSlugEntityTypes.Page, "shared-page", storeB));
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-seo-slug-collision-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
