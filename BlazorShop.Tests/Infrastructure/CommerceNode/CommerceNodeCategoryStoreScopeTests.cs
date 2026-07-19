namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Repositories;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CommerceNodeCategoryStoreScopeTests
    {
        [Fact]
        public async Task GetCategoriesForCurrentStoreAsync_ReturnsOnlyCurrentStoreCategories()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedCategoriesAsync(context, storeA, storeB);
            var repository = CreateRepository(context, storeA);

            var result = await repository.GetCategoriesForCurrentStoreAsync();

            Assert.Equal(2, result.Count);
            Assert.All(result, category => Assert.Equal(storeA, category.StoreId));
            Assert.DoesNotContain(result, category => category.Name == "Store B Category");
        }

        [Fact]
        public async Task GetCategoryByIdForCurrentStoreAsync_ReturnsNullForOtherStoreCategory()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            var (_, storeBCategoryId) = await SeedCategoriesAsync(context, storeA, storeB);
            var repository = CreateRepository(context, storeA);

            var result = await repository.GetCategoryByIdForCurrentStoreAsync(storeBCategoryId);

            Assert.Null(result);
        }

        [Fact]
        public async Task CategorySlugExistsInStoreAsync_ScopesDuplicateRuleToStore()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedCategoriesAsync(context, storeA, storeB);
            var repository = CreateRepository(context, storeA);

            var duplicateInStoreA = await repository.CategorySlugExistsInStoreAsync("store-a-category", storeA);
            var duplicateOnlyInStoreB = await repository.CategorySlugExistsInStoreAsync("store-b-category", storeA);

            Assert.True(duplicateInStoreA);
            Assert.False(duplicateOnlyInStoreB);
        }

        [Fact]
        public async Task CategoryBelongsToCurrentStoreAsync_ReturnsFalseForOtherStoreCategory()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            var (_, storeBCategoryId) = await SeedCategoriesAsync(context, storeA, storeB);
            var repository = CreateRepository(context, storeA);

            var result = await repository.CategoryBelongsToCurrentStoreAsync(storeBCategoryId);

            Assert.False(result);
        }

        private static CommerceNodeCategoryRepository CreateRepository(CommerceNodeDbContext context, Guid storeId)
        {
            return new CommerceNodeCategoryRepository(context, new FixedStoreContext(storeId));
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-category-store-scope-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static async Task<(Guid StoreACategoryId, Guid StoreBCategoryId)> SeedCategoriesAsync(
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
            var storeAChild = new Category
            {
                Id = Guid.NewGuid(),
                StoreId = storeA,
                ParentCategoryId = storeACategory.Id,
                Name = "Store A Child",
                Slug = "store-a-child",
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

            context.Categories.AddRange(storeACategory, storeAChild, storeBCategory);
            await context.SaveChangesAsync();

            return (storeACategory.Id, storeBCategory.Id);
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
