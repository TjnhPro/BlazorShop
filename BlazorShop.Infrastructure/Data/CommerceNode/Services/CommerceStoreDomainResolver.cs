namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    public sealed class CommerceStoreDomainResolver : ICommerceStoreDomainResolver
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly CommerceNodeDbContext context;
        private readonly IMemoryCache memoryCache;

        public CommerceStoreDomainResolver(
            CommerceNodeDbContext context,
            IMemoryCache memoryCache)
        {
            this.context = context;
            this.memoryCache = memoryCache;
        }

        public async Task<CommerceStoreOperationResult<CommerceCurrentStore>> ResolveAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(storeKey))
            {
                return await this.ResolveByStoreKeyAsync(storeKey, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                var normalizedHost = CommerceStoreService.NormalizeDomain(host);
                if (normalizedHost is null)
                {
                    return Failed(CommerceStoreOperationFailure.Validation, "Store host is invalid.");
                }

                return await this.ResolveByHostAsync(normalizedHost, cancellationToken);
            }

            return await this.ResolveSingleActiveStoreAsync(cancellationToken);
        }

        public async Task<CommerceStoreOperationResult<Guid>> ResolveStoreIdAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.ResolveStoreAsync(storeKey, host, cancellationToken);
            return storeResult.Success && storeResult.Payload is not null
                ? new CommerceStoreOperationResult<Guid>(true, "Current store id resolved.", storeResult.Payload.Id)
                : new CommerceStoreOperationResult<Guid>(
                    false,
                    storeResult.Message,
                    Failure: storeResult.Failure);
        }

        public async Task<CommerceStoreOperationResult<CommerceCurrentStore>> ResolveForReadinessAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(storeKey))
            {
                var normalizedStoreKey = storeKey.Trim().ToLowerInvariant();
                var store = await this.StoreReadinessQuery()
                    .FirstOrDefaultAsync(entity => entity.StoreKey == normalizedStoreKey, cancellationToken);

                return store is null
                    ? Failed(CommerceStoreOperationFailure.NotFound, "Store was not found.")
                    : Succeeded(MapCurrentStore(store));
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                var normalizedHost = CommerceStoreService.NormalizeDomain(host);
                if (normalizedHost is null)
                {
                    return Failed(CommerceStoreOperationFailure.Validation, "Store host is invalid.");
                }

                var store = await this.StoreReadinessQuery()
                    .FirstOrDefaultAsync(
                        entity => entity.Domains.Any(
                            domain =>
                                domain.NormalizedDomain == normalizedHost &&
                                domain.DisabledAt == null &&
                                domain.Status == CommerceStoreDomainStatuses.Verified),
                        cancellationToken);

                return store is null
                    ? Failed(CommerceStoreOperationFailure.NotFound, "Store host was not found.")
                    : Succeeded(MapCurrentStore(store));
            }

            var stores = await this.StoreReadinessQuery()
                .OrderBy(store => store.DisplayOrder)
                .ThenBy(store => store.Name)
                .Take(2)
                .ToListAsync(cancellationToken);

            return stores.Count switch
            {
                1 => Succeeded(MapCurrentStore(stores[0])),
                0 => Failed(CommerceStoreOperationFailure.NotFound, "No store is configured."),
                _ => Failed(CommerceStoreOperationFailure.Conflict, "Multiple stores require an explicit store key or host."),
            };
        }

        private async Task<CommerceStoreOperationResult<CommerceCurrentStore>> ResolveByStoreKeyAsync(
            string storeKey,
            CancellationToken cancellationToken)
        {
            var normalizedStoreKey = storeKey.Trim().ToLowerInvariant();
            var cacheKey = $"commerce-store:key:{normalizedStoreKey}";
            if (this.memoryCache.TryGetValue(cacheKey, out CommerceCurrentStore? cached) && cached is not null)
            {
                return Succeeded(cached);
            }

            var store = await this.ActiveStoreQuery()
                .FirstOrDefaultAsync(entity => entity.StoreKey == normalizedStoreKey, cancellationToken);

            if (store is null)
            {
                return Failed(CommerceStoreOperationFailure.NotFound, "Store was not found.");
            }

            var currentStore = MapCurrentStore(store);
            this.memoryCache.Set(cacheKey, currentStore, CacheDuration);
            return Succeeded(currentStore);
        }

        private async Task<CommerceStoreOperationResult<CommerceCurrentStore>> ResolveByHostAsync(
            string normalizedHost,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"commerce-store:host:{normalizedHost}";
            if (this.memoryCache.TryGetValue(cacheKey, out CommerceCurrentStore? cached) && cached is not null)
            {
                return Succeeded(cached);
            }

            var store = await this.ActiveStoreQuery()
                .FirstOrDefaultAsync(
                    entity => entity.Domains.Any(
                        domain =>
                            domain.NormalizedDomain == normalizedHost &&
                            domain.DisabledAt == null &&
                            domain.Status == CommerceStoreDomainStatuses.Verified),
                    cancellationToken);

            if (store is null)
            {
                return Failed(CommerceStoreOperationFailure.NotFound, "Store host was not found.");
            }

            var currentStore = MapCurrentStore(store);
            this.memoryCache.Set(cacheKey, currentStore, CacheDuration);
            return Succeeded(currentStore);
        }

        private async Task<CommerceStoreOperationResult<CommerceCurrentStore>> ResolveSingleActiveStoreAsync(
            CancellationToken cancellationToken)
        {
            var stores = await this.ActiveStoreQuery()
                .OrderBy(store => store.DisplayOrder)
                .ThenBy(store => store.Name)
                .Take(2)
                .ToListAsync(cancellationToken);

            return stores.Count switch
            {
                1 => Succeeded(MapCurrentStore(stores[0])),
                0 => Failed(CommerceStoreOperationFailure.NotFound, "No active store is configured."),
                _ => Failed(CommerceStoreOperationFailure.Conflict, "Multiple active stores require an explicit store key or host."),
            };
        }

        private IQueryable<CommerceStore> ActiveStoreQuery()
        {
            return this.context.CommerceStores
                .AsNoTracking()
                .Include(store => store.Domains.OrderByDescending(domain => domain.IsPrimary).ThenBy(domain => domain.Domain))
                .Where(store => store.ArchivedAt == null && store.Status == CommerceStoreStatuses.Active);
        }

        private IQueryable<CommerceStore> StoreReadinessQuery()
        {
            return this.context.CommerceStores
                .AsNoTracking()
                .Include(store => store.Domains.OrderByDescending(domain => domain.IsPrimary).ThenBy(domain => domain.Domain))
                .Where(store => store.ArchivedAt == null);
        }

        private async Task<CommerceStoreOperationResult<CommerceStore>> ResolveStoreAsync(
            string? storeKey,
            string? host,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(storeKey))
            {
                var normalizedStoreKey = storeKey.Trim().ToLowerInvariant();
                var store = await this.ActiveStoreQuery()
                    .FirstOrDefaultAsync(entity => entity.StoreKey == normalizedStoreKey, cancellationToken);

                return store is null
                    ? FailedStore(CommerceStoreOperationFailure.NotFound, "Store was not found.")
                    : SucceededStore(store);
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                var normalizedHost = CommerceStoreService.NormalizeDomain(host);
                if (normalizedHost is null)
                {
                    return FailedStore(CommerceStoreOperationFailure.Validation, "Store host is invalid.");
                }

                var store = await this.ActiveStoreQuery()
                    .FirstOrDefaultAsync(
                        entity => entity.Domains.Any(
                            domain =>
                                domain.NormalizedDomain == normalizedHost &&
                                domain.DisabledAt == null &&
                                domain.Status == CommerceStoreDomainStatuses.Verified),
                        cancellationToken);

                return store is null
                    ? FailedStore(CommerceStoreOperationFailure.NotFound, "Store host was not found.")
                    : SucceededStore(store);
            }

            var stores = await this.ActiveStoreQuery()
                .OrderBy(store => store.DisplayOrder)
                .ThenBy(store => store.Name)
                .Take(2)
                .ToListAsync(cancellationToken);

            return stores.Count switch
            {
                1 => SucceededStore(stores[0]),
                0 => FailedStore(CommerceStoreOperationFailure.NotFound, "No active store is configured."),
                _ => FailedStore(CommerceStoreOperationFailure.Conflict, "Multiple active stores require an explicit store key or host."),
            };
        }

        private static CommerceCurrentStore MapCurrentStore(CommerceStore store)
        {
            var primaryDomain = store.Domains.FirstOrDefault(domain => domain.IsPrimary && domain.DisabledAt == null);
            return new CommerceCurrentStore(
                store.PublicId,
                store.StoreKey,
                store.Name,
                store.Status,
                store.BaseUrl,
                primaryDomain?.Domain,
                store.ForceHttps,
                store.CdnHost,
                store.LogoUrl,
                store.CompanyName,
                store.CompanyEmail,
                store.CompanyPhone,
                store.CompanyAddress,
                store.FaviconUrl,
                store.PngIconUrl,
                store.AppleTouchIconUrl,
                store.MsTileImageUrl,
                store.MsTileColor,
                store.DefaultCurrencyCode,
                store.DefaultCulture,
                store.SupportEmail,
                store.SupportPhone,
                store.MaintenanceModeEnabled,
                store.MaintenanceMessage,
                store.HtmlBodyId);
        }

        private static CommerceStoreOperationResult<CommerceCurrentStore> Succeeded(CommerceCurrentStore store)
        {
            return new CommerceStoreOperationResult<CommerceCurrentStore>(true, "Current store resolved.", store);
        }

        private static CommerceStoreOperationResult<CommerceCurrentStore> Failed(
            CommerceStoreOperationFailure failure,
            string message)
        {
            return new CommerceStoreOperationResult<CommerceCurrentStore>(false, message, Failure: failure);
        }

        private static CommerceStoreOperationResult<CommerceStore> SucceededStore(CommerceStore store)
        {
            return new CommerceStoreOperationResult<CommerceStore>(true, "Current store resolved.", store);
        }

        private static CommerceStoreOperationResult<CommerceStore> FailedStore(
            CommerceStoreOperationFailure failure,
            string message)
        {
            return new CommerceStoreOperationResult<CommerceStore>(false, message, Failure: failure);
        }
    }
}
