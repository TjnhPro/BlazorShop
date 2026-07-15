namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Repositories;
    using BlazorShop.Infrastructure.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CommerceNodeProductStoreScopeTests
    {
        [Fact]
        public async Task GetCatalogPageForCurrentStoreAsync_ReturnsOnlyCurrentStoreProducts()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedCatalogAsync(context, storeA, storeB);
            var repository = CreateRepository(context, storeA);

            var result = await repository.GetCatalogPageForCurrentStoreAsync(new ProductCatalogQuery
            {
                PageNumber = 1,
                PageSize = 10,
            });

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, product => Assert.StartsWith("Store A", product.Name, StringComparison.Ordinal));
            Assert.DoesNotContain(result.Items, product => product.Name == "Store B Product");
        }

        [Fact]
        public async Task GetProductDetailsByIdForCurrentStoreAsync_ReturnsNullForOtherStoreProduct()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            var (_, storeBProductId) = await SeedCatalogAsync(context, storeA, storeB);
            var repository = CreateRepository(context, storeA);

            var result = await repository.GetProductDetailsByIdForCurrentStoreAsync(storeBProductId);

            Assert.Null(result);
        }

        [Fact]
        public async Task ProductSlugExistsInStoreAsync_ScopesDuplicateRuleToStore()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedCatalogAsync(context, storeA, storeB);
            var repository = CreateRepository(context, storeA);

            var duplicateInStoreA = await repository.ProductSlugExistsInStoreAsync("store-a-product-1", storeA);
            var duplicateOnlyInStoreB = await repository.ProductSlugExistsInStoreAsync("store-b-product", storeA);

            Assert.True(duplicateInStoreA);
            Assert.False(duplicateOnlyInStoreB);
        }

        private static CommerceNodeProductReadRepository CreateRepository(CommerceNodeDbContext context, Guid storeId)
        {
            return new CommerceNodeProductReadRepository(context, new SlugService(), new FixedStoreContext(storeId));
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-product-store-scope-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static async Task<(Guid StoreAProductId, Guid StoreBProductId)> SeedCatalogAsync(
            CommerceNodeDbContext context,
            Guid storeA,
            Guid storeB)
        {
            var storeACategory = new Category
            {
                Id = Guid.NewGuid(),
                StoreId = storeA,
                Name = "Store A Category",
                Slug = "store-a-category",
                IsPublished = true,
            };
            var storeBCategory = new Category
            {
                Id = Guid.NewGuid(),
                StoreId = storeB,
                Name = "Store B Category",
                Slug = "store-b-category",
                IsPublished = true,
            };

            var storeAProduct1 = CreateProduct(storeA, storeACategory.Id, "Store A Product 1", "store-a-product-1");
            var storeAProduct2 = CreateProduct(storeA, storeACategory.Id, "Store A Product 2", "store-a-product-2");
            var storeBProduct = CreateProduct(storeB, storeBCategory.Id, "Store B Product", "store-b-product");

            context.Categories.AddRange(storeACategory, storeBCategory);
            context.Products.AddRange(storeAProduct1, storeAProduct2, storeBProduct);
            await context.SaveChangesAsync();

            return (storeAProduct1.Id, storeBProduct.Id);
        }

        private static Product CreateProduct(Guid storeId, Guid categoryId, string name, string slug)
        {
            return new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                CategoryId = categoryId,
                Name = name,
                Description = name,
                Price = 10m,
                Quantity = 5,
                Slug = slug,
                Sku = slug,
                IsPublished = true,
                PublishedOn = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                CreatedOn = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
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
                var currentStore = new CommerceCurrentStore(
                    this.storeId,
                    "current",
                    "Current Store",
                    "active",
                    null,
                    null,
                    true,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "USD",
                    "en-US",
                    null,
                    null,
                    false,
                    null,
                    null);

                return Task.FromResult(new CommerceStoreOperationResult<CommerceCurrentStore>(true, "Current store resolved.", currentStore));
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Current store resolved.", this.storeId));
            }
        }
    }
}
