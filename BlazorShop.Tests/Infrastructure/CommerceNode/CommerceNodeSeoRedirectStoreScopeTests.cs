namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Repositories;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging.Abstractions;

    using Xunit;

    public sealed class CommerceNodeSeoRedirectStoreScopeTests
    {
        [Fact]
        public async Task GetActiveByOldPathInStoreAsync_ReturnsOnlyCurrentStoreRedirect()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedRedirectsAsync(context, storeA, storeB);
            var repository = new CommerceNodeSeoRedirectRepository(context);

            var result = await repository.GetActiveByOldPathInStoreAsync(storeA, "/legacy-sale");

            Assert.NotNull(result);
            Assert.Equal(storeA, result!.StoreId);
            Assert.Equal("/sale-a", result.NewPath);
        }

        [Fact]
        public async Task ResolvePublicPathAsync_DoesNotResolveOtherStoreRedirect()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            context.SeoRedirects.Add(new SeoRedirect
            {
                Id = Guid.NewGuid(),
                StoreId = storeB,
                OldPath = "/legacy-sale",
                NewPath = "/sale-b",
                StatusCode = 301,
                IsActive = true,
            });
            await context.SaveChangesAsync();

            var repository = new CommerceNodeSeoRedirectRepository(context);
            var service = new SeoRedirectResolutionService(
                repository,
                NullLogger<SeoRedirectResolutionService>.Instance,
                new FixedStoreContext(storeA));

            var result = await service.ResolvePublicPathAsync("/legacy-sale");

            Assert.Null(result);
        }

        [Fact]
        public async Task OldPathExistsInStoreAsync_AllowsSamePathInDifferentStore()
        {
            var storeA = Guid.NewGuid();
            var storeB = Guid.NewGuid();
            await using var context = CreateContext();
            await SeedRedirectsAsync(context, storeA, storeB);
            var repository = new CommerceNodeSeoRedirectRepository(context);

            var duplicateInStoreA = await repository.OldPathExistsInStoreAsync(storeA, "/legacy-sale");
            var missingInStoreA = await repository.OldPathExistsInStoreAsync(storeA, "/store-b-only");

            Assert.True(duplicateInStoreA);
            Assert.False(missingInStoreA);
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-node-seo-redirect-store-scope-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private static async Task SeedRedirectsAsync(CommerceNodeDbContext context, Guid storeA, Guid storeB)
        {
            context.SeoRedirects.AddRange(
                new SeoRedirect
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeA,
                    OldPath = "/legacy-sale",
                    NewPath = "/sale-a",
                    StatusCode = 301,
                    IsActive = true,
                },
                new SeoRedirect
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeB,
                    OldPath = "/legacy-sale",
                    NewPath = "/sale-b",
                    StatusCode = 301,
                    IsActive = true,
                },
                new SeoRedirect
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeB,
                    OldPath = "/store-b-only",
                    NewPath = "/store-b",
                    StatusCode = 301,
                    IsActive = true,
                });

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
