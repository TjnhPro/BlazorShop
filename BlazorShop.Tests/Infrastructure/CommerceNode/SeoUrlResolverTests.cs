namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class SeoUrlResolverTests
    {
        [Fact]
        public async Task ResolvePublicPathAsync_WhenActiveProductSlugExists_ReturnsResolvedCanonical()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedPublishedProductAsync(context, storeId, productId);
            context.StoreSeoSlugHistories.Add(new StoreSeoSlugHistory
            {
                StoreId = storeId,
                EntityType = SeoSlugEntityTypes.Product,
                EntityId = productId,
                Slug = "current-product",
                IsActive = true,
            });
            await context.SaveChangesAsync();
            var resolver = CreateResolver(context, storeId);

            var result = await resolver.ResolvePublicPathAsync("/product/current-product");

            Assert.Equal(SeoUrlResolutionStatuses.Resolved, result.Status);
            Assert.False(result.RequiresRedirect);
            Assert.Equal("/product/current-product", result.CanonicalPath);
            Assert.Equal(productId, result.EntityId);
        }

        [Fact]
        public async Task ResolvePublicPathAsync_WhenOldProductSlugExists_ReturnsRedirectToCanonical()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedPublishedProductAsync(context, storeId, productId);
            context.StoreSeoSlugHistories.AddRange(
                new StoreSeoSlugHistory
                {
                    StoreId = storeId,
                    EntityType = SeoSlugEntityTypes.Product,
                    EntityId = productId,
                    Slug = "old-product",
                    IsActive = false,
                    ReplacedAt = DateTimeOffset.UtcNow,
                    ReplacedBySlug = "current-product",
                },
                new StoreSeoSlugHistory
                {
                    StoreId = storeId,
                    EntityType = SeoSlugEntityTypes.Product,
                    EntityId = productId,
                    Slug = "current-product",
                    IsActive = true,
                });
            await context.SaveChangesAsync();
            var resolver = CreateResolver(context, storeId);

            var result = await resolver.ResolvePublicPathAsync("/product/old-product");

            Assert.Equal(SeoUrlResolutionStatuses.RedirectToCanonical, result.Status);
            Assert.True(result.RequiresRedirect);
            Assert.Equal(301, result.HttpStatusCode);
            Assert.Equal("/product/current-product", result.CanonicalPath);
            Assert.Equal("old-product", result.RequestedSlug);
            Assert.Equal("current-product", result.CanonicalSlug);
        }

        [Fact]
        public async Task ResolvePublicPathAsync_WhenSlugBelongsToAnotherStore_ReturnsNotFound()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedPublishedProductAsync(context, storeB, productId);
            context.StoreSeoSlugHistories.Add(new StoreSeoSlugHistory
            {
                StoreId = storeB,
                EntityType = SeoSlugEntityTypes.Product,
                EntityId = productId,
                Slug = "other-store-product",
                IsActive = true,
            });
            await context.SaveChangesAsync();
            var resolver = CreateResolver(context, storeA);

            var result = await resolver.ResolvePublicPathAsync("/product/other-store-product");

            Assert.Equal(SeoUrlResolutionStatuses.NotFound, result.Status);
            Assert.False(result.RequiresRedirect);
        }

        [Fact]
        public async Task ResolvePublicPathAsync_WhenPageIsUnpublished_ReturnsGone()
        {
            var storeId = Guid.NewGuid();
            var pageId = Guid.NewGuid();
            await using var context = CreateContext();
            context.StorefrontPages.Add(new StorefrontPage
            {
                Id = pageId,
                StoreId = storeId,
                Slug = "draft-page",
                Title = "Draft",
                BodyHtml = "<p>Draft</p>",
                IsPublished = false,
            });
            context.StoreSeoSlugHistories.Add(new StoreSeoSlugHistory
            {
                StoreId = storeId,
                EntityType = SeoSlugEntityTypes.Page,
                EntityId = pageId,
                Slug = "draft-page",
                IsActive = true,
            });
            await context.SaveChangesAsync();
            var resolver = CreateResolver(context, storeId);

            var result = await resolver.ResolvePublicPathAsync("/pages/draft-page");

            Assert.Equal(SeoUrlResolutionStatuses.Gone, result.Status);
            Assert.Equal(410, result.HttpStatusCode);
        }

        private static SeoUrlResolver CreateResolver(CommerceNodeDbContext context, Guid storeId)
        {
            return new SeoUrlResolver(context, new FixedStoreContext(storeId));
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-seo-url-resolver-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static async Task SeedPublishedProductAsync(CommerceNodeDbContext context, Guid storeId, Guid productId)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Slug = "category",
                IsPublished = true,
            };
            var product = new Product
            {
                Id = productId,
                StoreId = storeId,
                CategoryId = category.Id,
                Category = category,
                Slug = "current-product",
                IsPublished = true,
                PublishedOn = DateTime.UtcNow,
            };

            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();
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
