namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class ProductMediaServiceTests
    {
        [Fact]
        public async Task UpdateOrderAsync_InvalidatesCatalogForCurrentStore()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var productId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var mediaPublicId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            await using var context = CreateContext();
            SeedProduct(context, storeId, productId);
            SeedMedia(context, storeId, productId, mediaPublicId, sortOrder: 5);
            await context.SaveChangesAsync();
            var cache = new RecordingCatalogQueryCache();
            var service = CreateService(context, storeId, cache);

            var result = await service.UpdateOrderAsync(
                productId,
                new UpdateProductMediaOrderRequest([new UpdateProductMediaOrderItem(mediaPublicId, 1)]));

            Assert.True(result.Success);
            Assert.Equal([storeId], cache.InvalidatedStoreIds);
            Assert.Equal(1, await context.ProductMedia.Where(media => media.PublicId == mediaPublicId).Select(media => media.SortOrder).SingleAsync());
        }

        [Fact]
        public async Task DeleteAsync_NonPrimaryMediaInvalidatesCatalogAndKeepsPrimaryImage()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var productId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var primaryPublicId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var deletedPublicId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var urlBuilder = new ProductMediaUrlBuilder();

            await using var context = CreateContext();
            var product = SeedProduct(context, storeId, productId);
            product.Image = urlBuilder.BuildProductMediaUrl(primaryPublicId, 2);
            SeedMedia(context, storeId, productId, primaryPublicId, sortOrder: 0, isPrimary: true, version: 2);
            SeedMedia(context, storeId, productId, deletedPublicId, sortOrder: 1);
            await context.SaveChangesAsync();
            var cache = new RecordingCatalogQueryCache();
            var service = CreateService(context, storeId, cache);

            var result = await service.DeleteAsync(productId, deletedPublicId);

            Assert.True(result.Success);
            Assert.Equal([storeId], cache.InvalidatedStoreIds);
            var deleted = await context.ProductMedia.SingleAsync(media => media.PublicId == deletedPublicId);
            Assert.Equal(ProductMediaStatuses.Deleted, deleted.Status);
            Assert.NotNull(deleted.DeletedAt);
            Assert.Equal(urlBuilder.BuildProductMediaUrl(primaryPublicId, 2), product.Image);
        }

        [Fact]
        public async Task DeleteAsync_PrimaryMediaAssignsNextStoredMedia()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var productId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var primaryPublicId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
            var nextPublicId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var urlBuilder = new ProductMediaUrlBuilder();

            await using var context = CreateContext();
            var product = SeedProduct(context, storeId, productId);
            product.Image = urlBuilder.BuildProductMediaUrl(primaryPublicId, 1);
            SeedMedia(context, storeId, productId, primaryPublicId, sortOrder: 0, isPrimary: true);
            SeedMedia(context, storeId, productId, nextPublicId, sortOrder: 1, version: 4);
            await context.SaveChangesAsync();
            var cache = new RecordingCatalogQueryCache();
            var service = CreateService(context, storeId, cache);

            var result = await service.DeleteAsync(productId, primaryPublicId);

            Assert.True(result.Success);
            Assert.Equal([storeId], cache.InvalidatedStoreIds);
            var next = await context.ProductMedia.SingleAsync(media => media.PublicId == nextPublicId);
            Assert.True(next.IsPrimary);
            Assert.Equal(urlBuilder.BuildProductMediaUrl(nextPublicId, 4), product.Image);
        }

        [Fact]
        public async Task DeleteAsync_LastPrimaryMediaClearsProductImage()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var productId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var primaryPublicId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            await using var context = CreateContext();
            var product = SeedProduct(context, storeId, productId);
            product.Image = "/media/products/old";
            SeedMedia(context, storeId, productId, primaryPublicId, sortOrder: 0, isPrimary: true);
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId, new RecordingCatalogQueryCache());

            var result = await service.DeleteAsync(productId, primaryPublicId);

            Assert.True(result.Success);
            Assert.Null(product.Image);
        }

        [Fact]
        public async Task ListAsync_PreservesStoredAltText()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var productId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var mediaPublicId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            await using var context = CreateContext();
            SeedProduct(context, storeId, productId);
            SeedMedia(context, storeId, productId, mediaPublicId, sortOrder: 0, altText: "Lifestyle hero");
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId, new RecordingCatalogQueryCache());

            var result = await service.ListAsync(productId, new ProductMediaListQuery());

            Assert.True(result.Success);
            Assert.Equal("Lifestyle hero", Assert.Single(result.Value!.Items).AltText);
        }

        private static ProductMediaService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            ICatalogQueryCache cache)
        {
            return new ProductMediaService(
                context,
                cache,
                new FixedCommerceStoreContext(storeId),
                new ThrowingCommerceTaskService(),
                new ProductMediaUrlBuilder());
        }

        private static Product SeedProduct(CommerceNodeDbContext context, Guid storeId, Guid productId)
        {
            var product = new Product
            {
                Id = productId,
                StoreId = storeId,
                Name = "Media product",
                Slug = "media-product",
                Price = 10m,
                Quantity = 5,
                CreatedOn = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.Products.Add(product);
            return product;
        }

        private static void SeedMedia(
            CommerceNodeDbContext context,
            Guid storeId,
            Guid productId,
            Guid publicId,
            int sortOrder,
            bool isPrimary = false,
            int version = 1,
            string? altText = null)
        {
            context.ProductMedia.Add(new ProductMedia
            {
                Id = Guid.NewGuid(),
                PublicId = publicId,
                StoreId = storeId,
                ProductId = productId,
                SortOrder = sortOrder,
                IsPrimary = isPrimary,
                Status = ProductMediaStatuses.Stored,
                Version = version,
                AltText = altText,
                OriginalStoragePath = $"stores/{storeId:D}/products/{productId:D}/{publicId:D}/original.webp",
                FileName = "original.webp",
                MimeType = "image/webp",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(sortOrder),
                UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(sortOrder),
            });
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"product-media-service-{Guid.NewGuid():N}")
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

        private sealed class ThrowingCommerceTaskService : ICommerceTaskService
        {
            public Task<CommerceTaskOperationResult<CommerceTaskSummary>> EnqueueAsync(
                EnqueueCommerceTaskRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceTaskOperationResult<CommerceTaskListResponse>> ListAsync(
                CommerceTaskListQuery query,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceTaskOperationResult<CommerceTaskDetail>> GetByPublicIdAsync(
                Guid publicId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceTaskOperationResult<CommerceTaskDetail>> CancelAsync(
                Guid publicId,
                CancelCommerceTaskRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceTaskOperationResult<CommerceTaskDetail>> RetryAsync(
                Guid publicId,
                RetryCommerceTaskRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
