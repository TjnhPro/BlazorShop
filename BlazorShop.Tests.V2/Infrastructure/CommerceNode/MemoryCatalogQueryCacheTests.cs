namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.Extensions.Caching.Memory;

    using Xunit;

    public sealed class MemoryCatalogQueryCacheTests
    {
        [Fact]
        public async Task InvalidateStoreCatalogAsync_RemovesOnlyEntriesForTargetStore()
        {
            var storeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var otherStoreId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new MemoryCatalogQueryCache(memoryCache);

            await cache.SetAsync(BuildCatalogKey(storeId), "target", TimeSpan.FromMinutes(5));
            await cache.SetAsync(BuildCatalogKey(otherStoreId), "other", TimeSpan.FromMinutes(5));

            await cache.InvalidateStoreCatalogAsync(storeId);

            Assert.Null(await cache.GetAsync<string>(BuildCatalogKey(storeId)));
            Assert.Equal("other", await cache.GetAsync<string>(BuildCatalogKey(otherStoreId)));
        }

        [Fact]
        public async Task InvalidateStoreCatalogAsync_DoesNotRemoveKeysWithoutStorePrefix()
        {
            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var cache = new MemoryCatalogQueryCache(memoryCache);

            await cache.SetAsync("global:catalog:health", "alive", TimeSpan.FromMinutes(5));

            await cache.InvalidateStoreCatalogAsync(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            Assert.Equal("alive", await cache.GetAsync<string>("global:catalog:health"));
        }

        private static string BuildCatalogKey(Guid storeId)
        {
            return $"store:{storeId:D}:catalog:products:v2:page:1:size:24:sort:DisplayOrder";
        }
    }
}
