namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Settings;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    public sealed class StorefrontPublicConfigurationCache : IStorefrontPublicConfigurationCache
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly CommerceNodeDbContext context;
        private readonly IMemoryCache memoryCache;

        public StorefrontPublicConfigurationCache(CommerceNodeDbContext context, IMemoryCache memoryCache)
        {
            this.context = context;
            this.memoryCache = memoryCache;
        }

        public bool TryGet<TValue>(string storeKey, out TValue? value)
        {
            return this.memoryCache.TryGetValue(BuildKey(storeKey), out value);
        }

        public void Set<TValue>(string storeKey, TValue value)
        {
            this.memoryCache.Set(BuildKey(storeKey), value, CacheDuration);
        }

        public void Invalidate(string storeKey)
        {
            this.memoryCache.Remove(BuildKey(storeKey));
        }

        public async Task InvalidateAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            var storeKey = await this.context.CommerceStores
                .AsNoTracking()
                .Where(store => store.Id == storeId)
                .Select(store => store.StoreKey)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(storeKey))
            {
                this.Invalidate(storeKey);
            }
        }

        private static string BuildKey(string storeKey)
        {
            return $"store-public-config:{storeKey.Trim().ToLowerInvariant()}";
        }
    }
}
