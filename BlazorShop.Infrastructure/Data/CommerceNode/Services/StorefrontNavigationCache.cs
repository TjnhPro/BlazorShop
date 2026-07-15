namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Collections.Concurrent;

    using BlazorShop.Application.CommerceNode.Navigation;

    using Microsoft.Extensions.Caching.Memory;

    public sealed class StorefrontNavigationCache : IStorefrontNavigationCache
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly IMemoryCache memoryCache;
        private readonly ConcurrentDictionary<Guid, long> versions = new();

        public StorefrontNavigationCache(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public bool TryGet(Guid storeId, string systemName, out StoreNavigationPublicMenuDto? value)
        {
            return this.memoryCache.TryGetValue(BuildKey(storeId, systemName, this.GetVersion(storeId)), out value);
        }

        public void Set(Guid storeId, string systemName, StoreNavigationPublicMenuDto value)
        {
            this.memoryCache.Set(BuildKey(storeId, systemName, this.GetVersion(storeId)), value, CacheDuration);
        }

        public void Invalidate(Guid storeId)
        {
            this.versions.AddOrUpdate(storeId, 1, (_, version) => version + 1);
        }

        private long GetVersion(Guid storeId)
        {
            return this.versions.GetOrAdd(storeId, 0);
        }

        private static string BuildKey(Guid storeId, string systemName, long version)
        {
            return $"store-navigation:{storeId:N}:{Normalize(systemName)}:{version}";
        }

        private static string Normalize(string systemName)
        {
            return systemName.Trim().ToLowerInvariant();
        }
    }
}
