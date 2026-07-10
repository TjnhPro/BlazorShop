namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Collections.Concurrent;

    using BlazorShop.Application.CommerceNode.Catalog;

    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Primitives;

    public sealed class MemoryCatalogQueryCache : ICatalogQueryCache
    {
        private const string StoreKeyPrefix = "store:";

        private readonly IMemoryCache memoryCache;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> storeTokens = new();

        public MemoryCatalogQueryCache(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(this.memoryCache.TryGetValue(key, out T? value) ? value : default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            };

            var storeId = TryReadStoreId(key);
            if (storeId.HasValue)
            {
                var tokenSource = this.storeTokens.GetOrAdd(storeId.Value, _ => new CancellationTokenSource());
                options.AddExpirationToken(new CancellationChangeToken(tokenSource.Token));
            }

            this.memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task InvalidateStoreCatalogAsync(Guid storeId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (storeId == Guid.Empty)
            {
                return Task.CompletedTask;
            }

            if (this.storeTokens.TryRemove(storeId, out var tokenSource))
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }

            return Task.CompletedTask;
        }

        private static Guid? TryReadStoreId(string key)
        {
            if (!key.StartsWith(StoreKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var start = StoreKeyPrefix.Length;
            var end = key.IndexOf(':', start);
            if (end <= start)
            {
                return null;
            }

            return Guid.TryParse(key[start..end], out var storeId) ? storeId : null;
        }
    }
}
