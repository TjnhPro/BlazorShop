namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CategoryMediaServiceTests
    {
        [Fact]
        public async Task GetPrimaryAsync_WhenNoAssignment_ReturnsEmptyDto()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var categoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            await using var context = CreateContext();
            SeedCategory(context, storeId, categoryId);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId, new RecordingCatalogQueryCache());

            var result = await service.GetPrimaryAsync(categoryId);

            Assert.True(result.Success);
            Assert.Null(result.Payload!.MediaAssetPublicId);
            Assert.Null(result.Payload.PublicUrl);
            Assert.False(result.Payload.IsPrimary);
        }

        [Fact]
        public async Task SetPrimaryAsync_SyncsCategoryImageAndInvalidatesCatalog()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var categoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var assetPublicId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            await using var context = CreateContext();
            var category = SeedCategory(context, storeId, categoryId);
            SeedAsset(context, storeId, assetPublicId, "category.jpg", "Asset alt");
            await context.SaveChangesAsync();
            var cache = new RecordingCatalogQueryCache();
            var service = CreateService(context, storeId, cache);

            var result = await service.SetPrimaryAsync(
                categoryId,
                new SetCategoryPrimaryMediaRequest(assetPublicId, "Category tile"));

            Assert.True(result.Success);
            Assert.Equal(assetPublicId, result.Payload!.MediaAssetPublicId);
            Assert.Equal("Category tile", result.Payload.AltText);
            Assert.Contains("/media/assets/cccccccc-cccc-cccc-cccc-cccccccccccc/category.jpg?w=600&h=400&fit=cover&format=webp&v=", category.Image);
            Assert.Equal([storeId], cache.InvalidatedStoreIds);
            Assert.Single(context.CategoryMediaAssignments);
        }

        [Fact]
        public async Task SetPrimaryAsync_RejectsAssetFromAnotherStore()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var otherStoreId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var categoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var assetPublicId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

            await using var context = CreateContext();
            SeedCategory(context, storeId, categoryId);
            SeedAsset(context, otherStoreId, assetPublicId, "other.jpg", "Other alt");
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId, new RecordingCatalogQueryCache());

            var result = await service.SetPrimaryAsync(
                categoryId,
                new SetCategoryPrimaryMediaRequest(assetPublicId));

            Assert.False(result.Success);
            Assert.Equal(CategoryMediaOperationFailure.NotFound, result.Failure);
            Assert.Empty(context.CategoryMediaAssignments);
        }

        [Fact]
        public async Task ClearPrimaryAsync_RemovesAssignmentClearsImageAndInvalidatesCatalog()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var categoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var assetId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

            await using var context = CreateContext();
            var category = SeedCategory(context, storeId, categoryId);
            category.Image = "/media/assets/old";
            var asset = SeedAsset(context, storeId, assetId, "category.jpg", "Asset alt");
            context.CategoryMediaAssignments.Add(new CategoryMediaAssignment
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                CategoryId = categoryId,
                MediaAssetId = asset.Id,
                IsPrimary = true,
                SortOrder = 0,
                AltText = "Category alt",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await context.SaveChangesAsync();
            var cache = new RecordingCatalogQueryCache();
            var service = CreateService(context, storeId, cache);

            var result = await service.ClearPrimaryAsync(categoryId);

            Assert.True(result.Success);
            Assert.Null(category.Image);
            Assert.Empty(context.CategoryMediaAssignments);
            Assert.Equal([storeId], cache.InvalidatedStoreIds);
        }

        private static CategoryMediaService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            ICatalogQueryCache cache)
        {
            return new CategoryMediaService(
                context,
                new FixedCommerceStoreContext(storeId),
                new CommerceMediaUrlBuilder(),
                cache);
        }

        private static Category SeedCategory(CommerceNodeDbContext context, Guid storeId, Guid categoryId)
        {
            var category = new Category
            {
                Id = categoryId,
                StoreId = storeId,
                Name = "Category",
                Slug = "category",
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow,
            };
            context.Categories.Add(category);
            return category;
        }

        private static CommerceMediaAsset SeedAsset(
            CommerceNodeDbContext context,
            Guid storeId,
            Guid publicId,
            string canonicalFileName,
            string altText)
        {
            var asset = new CommerceMediaAsset
            {
                Id = Guid.NewGuid(),
                PublicId = publicId,
                StoreId = storeId,
                OriginalFileName = canonicalFileName,
                CanonicalFileName = canonicalFileName,
                DisplayName = canonicalFileName,
                AltText = altText,
                OriginalStoragePath = $"stores/{storeId:D}/{publicId:D}/original.jpg",
                ContentHash = publicId.ToString("N"),
                MimeType = "image/jpeg",
                Extension = ".jpg",
                Width = 600,
                Height = 400,
                FileSizeBytes = 1024,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            context.CommerceMediaAssets.Add(asset);
            return asset;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"category-media-service-{Guid.NewGuid():N}")
                .Options;
            return new CommerceNodeDbContext(options);
        }

        private sealed class RecordingCatalogQueryCache : ICatalogQueryCache
        {
            public List<Guid> InvalidatedStoreIds { get; } = [];

            public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<T?>(default);
            }

            public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task InvalidateStoreCatalogAsync(Guid storeId, CancellationToken cancellationToken = default)
            {
                this.InvalidatedStoreIds.Add(storeId);
                return Task.CompletedTask;
            }
        }

        private sealed class FixedCommerceStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedCommerceStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Store resolved.", this.storeId));
            }
        }
    }
}
