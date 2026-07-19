namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.Common.Results;
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

        public async Task<ApplicationResult<CommerceCurrentStore>> ResolveAsync(
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
                    return Failed(ApplicationErrorKind.Validation, "Store host is invalid.");
                }

                return await this.ResolveByHostAsync(normalizedHost, cancellationToken);
            }

            return await this.ResolveSingleActiveStoreAsync(cancellationToken);
        }

        public async Task<ApplicationResult<Guid>> ResolveStoreIdAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.ResolveStoreAsync(storeKey, host, cancellationToken);
            return storeResult.Success && storeResult.Value is not null
                ? ApplicationResult<Guid>.Succeeded(storeResult.Value.Id, "Current store id resolved.")
                : ApplicationResult<Guid>.Failed(storeResult.Error!);
        }

        public async Task<ApplicationResult<CommerceCurrentStore>> ResolveForReadinessAsync(
            string? storeKey = null,
            string? host = null,
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.ResolveReadinessStoreAsync(storeKey, host, cancellationToken);
            return storeResult.Success && storeResult.Value is not null
                ? Succeeded(MapCurrentStore(storeResult.Value))
                : ApplicationResult<CommerceCurrentStore>.Failed(storeResult.Error!);
        }

        public async Task<ApplicationResult<StoreExecutionContext>> ResolveExecutionContextAsync(
            string? storeKey = null,
            string? host = null,
            string source = StoreExecutionContextSources.Unknown,
            CancellationToken cancellationToken = default)
        {
            var storeResult = await this.ResolveReadinessStoreAsync(storeKey, host, cancellationToken);
            if (!storeResult.Success || storeResult.Value is null)
            {
                return ApplicationResult<StoreExecutionContext>.Failed(storeResult.Error!);
            }

            var store = storeResult.Value;
            return ApplicationResult<StoreExecutionContext>.Succeeded(
                new StoreExecutionContext(
                    store.Id,
                    store.StoreKey,
                    string.IsNullOrWhiteSpace(host) ? null : host.Trim(),
                    string.IsNullOrWhiteSpace(source) ? StoreExecutionContextSources.Unknown : source.Trim(),
                    store.Status,
                    string.Equals(store.Status, CommerceStoreStatuses.Active, StringComparison.OrdinalIgnoreCase),
                    MapCurrentStore(store)),
                "Store execution context resolved.");
        }

        private async Task<ApplicationResult<CommerceCurrentStore>> ResolveByStoreKeyAsync(
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
                return Failed(ApplicationErrorKind.NotFound, "Store was not found.");
            }

            var currentStore = MapCurrentStore(store);
            this.memoryCache.Set(cacheKey, currentStore, CacheDuration);
            return Succeeded(currentStore);
        }

        private async Task<ApplicationResult<CommerceCurrentStore>> ResolveByHostAsync(
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
                return Failed(ApplicationErrorKind.NotFound, "Store host was not found.");
            }

            var currentStore = MapCurrentStore(store);
            this.memoryCache.Set(cacheKey, currentStore, CacheDuration);
            return Succeeded(currentStore);
        }

        private async Task<ApplicationResult<CommerceCurrentStore>> ResolveSingleActiveStoreAsync(
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
                0 => Failed(ApplicationErrorKind.NotFound, "No active store is configured."),
                _ => Failed(ApplicationErrorKind.Conflict, "Multiple active stores require an explicit store key or host."),
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

        private async Task<ApplicationResult<CommerceStore>> ResolveStoreAsync(
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
                    ? FailedStore(ApplicationErrorKind.NotFound, "Store was not found.")
                    : SucceededStore(store);
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                var normalizedHost = CommerceStoreService.NormalizeDomain(host);
                if (normalizedHost is null)
                {
                    return FailedStore(ApplicationErrorKind.Validation, "Store host is invalid.");
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
                    ? FailedStore(ApplicationErrorKind.NotFound, "Store host was not found.")
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
                0 => FailedStore(ApplicationErrorKind.NotFound, "No active store is configured."),
                _ => FailedStore(ApplicationErrorKind.Conflict, "Multiple active stores require an explicit store key or host."),
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

        private static ApplicationResult<CommerceCurrentStore> Succeeded(CommerceCurrentStore store)
        {
            return ApplicationResult<CommerceCurrentStore>.Succeeded(store, "Current store resolved.");
        }

        private static ApplicationResult<CommerceCurrentStore> Failed(
            ApplicationErrorKind failure,
            string message)
        {
            return ApplicationResult<CommerceCurrentStore>.Failed(ToError(failure, message));
        }

        private static ApplicationResult<CommerceStore> SucceededStore(CommerceStore store)
        {
            return ApplicationResult<CommerceStore>.Succeeded(store, "Current store resolved.");
        }

        private static ApplicationResult<CommerceStore> FailedStore(
            ApplicationErrorKind failure,
            string message)
        {
            return ApplicationResult<CommerceStore>.Failed(ToError(failure, message));
        }

        private static ApplicationError ToError(ApplicationErrorKind failure, string message)
        {
            return failure switch
            {
                ApplicationErrorKind.Validation => ApplicationError.Validation("store.validation", message),
                ApplicationErrorKind.NotFound => ApplicationError.NotFound("store.not_found", message),
                ApplicationErrorKind.Conflict => ApplicationError.Conflict("store.conflict", message),
                _ => ApplicationError.Failure("store.failure", message),
            };
        }

        private async Task<ApplicationResult<CommerceStore>> ResolveReadinessStoreAsync(
            string? storeKey,
            string? host,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(storeKey))
            {
                var normalizedStoreKey = storeKey.Trim().ToLowerInvariant();
                var store = await this.StoreReadinessQuery()
                    .FirstOrDefaultAsync(entity => entity.StoreKey == normalizedStoreKey, cancellationToken);

                return store is null
                    ? FailedStore(ApplicationErrorKind.NotFound, "Store was not found.")
                    : SucceededStore(store);
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                var normalizedHost = CommerceStoreService.NormalizeDomain(host);
                if (normalizedHost is null)
                {
                    return FailedStore(ApplicationErrorKind.Validation, "Store host is invalid.");
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
                    ? FailedStore(ApplicationErrorKind.NotFound, "Store host was not found.")
                    : SucceededStore(store);
            }

            var stores = await this.StoreReadinessQuery()
                .OrderBy(store => store.DisplayOrder)
                .ThenBy(store => store.Name)
                .Take(2)
                .ToListAsync(cancellationToken);

            return stores.Count switch
            {
                1 => SucceededStore(stores[0]),
                0 => FailedStore(ApplicationErrorKind.NotFound, "No store is configured."),
                _ => FailedStore(ApplicationErrorKind.Conflict, "Multiple stores require an explicit store key or host."),
            };
        }
    }
}
