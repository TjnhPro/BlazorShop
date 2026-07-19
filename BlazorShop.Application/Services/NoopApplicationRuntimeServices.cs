namespace BlazorShop.Application.Services
{
    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;

    public sealed class NoopCatalogQueryCache : ICatalogQueryCache
    {
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
            return Task.CompletedTask;
        }
    }

    public sealed class NoopStorefrontNavigationCache : IStorefrontNavigationCache
    {
        public bool TryGet(Guid storeId, string systemName, out StoreNavigationPublicMenuDto? value)
        {
            value = null;
            return false;
        }

        public void Set(Guid storeId, string systemName, StoreNavigationPublicMenuDto value)
        {
        }

        public void Invalidate(Guid storeId)
        {
        }
    }

    public sealed class NoopStoreSeoSlugHistoryService : IStoreSeoSlugHistoryService
    {
        public Task<StoreSeoSlugHistoryDto?> GetActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<StoreSeoSlugHistoryDto?>(null);
        }

        public Task<ServiceResponse<StoreSeoSlugHistoryDto>> RecordInitialActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string slug,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Succeeded(new StoreSeoSlugHistoryDto(
                Guid.Empty,
                storeId,
                entityType,
                entityId,
                slug,
                languageCode,
                IsActive: true,
                DateTimeOffset.UtcNow,
                ReplacedAt: null,
                ReplacedBySlug: null)));
        }

        public Task<ServiceResponse<StoreSeoSlugHistoryDto>> ReplaceActiveSlugAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string newSlug,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Succeeded(new StoreSeoSlugHistoryDto(
                Guid.Empty,
                storeId,
                entityType,
                entityId,
                newSlug,
                languageCode,
                IsActive: true,
                DateTimeOffset.UtcNow,
                ReplacedAt: null,
                ReplacedBySlug: null)));
        }

        public Task<IReadOnlyList<StoreSeoSlugHistoryDto>> ListHistoryAsync(
            string entityType,
            Guid entityId,
            Guid storeId,
            string? languageCode = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<StoreSeoSlugHistoryDto>>([]);
        }

        public Task<ServiceResponse<StoreSeoSlugBackfillResultDto>> BackfillCurrentSlugsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Succeeded(new StoreSeoSlugBackfillResultDto(Created: 0, Skipped: 0)));
        }

        private static ServiceResponse<T> Succeeded<T>(T payload)
        {
            return new ServiceResponse<T>(true, "Noop slug history service completed.")
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }
    }

    public sealed class NoopStorefrontPageService : IStorefrontPageService
    {
        public Task<ServiceResponse<StorefrontPageListResponse>> ListAsync(
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Succeeded(new StorefrontPageListResponse([])));
        }

        public Task<ServiceResponse<StorefrontPageDetailDto>> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NotFound<StorefrontPageDetailDto>());
        }

        public Task<ServiceResponse<StorefrontPageDetailDto>> CreateAsync(
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NotFound<StorefrontPageDetailDto>());
        }

        public Task<ServiceResponse<StorefrontPageDetailDto>> UpdateAsync(
            Guid id,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NotFound<StorefrontPageDetailDto>());
        }

        public Task<ServiceResponse<StorefrontPageDetailDto>> ArchiveAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NotFound<StorefrontPageDetailDto>());
        }

        public Task<ServiceResponse<StorefrontPagePublicDto>> GetPublishedBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NotFound<StorefrontPagePublicDto>());
        }

        public Task<ServiceResponse<IReadOnlyList<StorefrontPageSitemapEntryDto>>> ListSitemapEntriesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Succeeded<IReadOnlyList<StorefrontPageSitemapEntryDto>>([]));
        }

        private static ServiceResponse<T> Succeeded<T>(T payload)
        {
            return new ServiceResponse<T>(true, "Noop storefront page service completed.")
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<T> NotFound<T>()
        {
            return new ServiceResponse<T>(false, "Storefront pages are not available.")
            {
                ResponseType = ServiceResponseType.NotFound,
            };
        }
    }

    public sealed class NoopSeoRedirectAutomationService : ISeoRedirectAutomationService
    {
        public Task<ServiceResponse<SeoRedirectDto>> EnsurePermanentRedirectAsync(string oldPath, string newPath)
        {
            return Task.FromResult(new ServiceResponse<SeoRedirectDto>(true, "Noop redirect automation completed.")
            {
                Payload = new SeoRedirectDto
                {
                    OldPath = oldPath,
                    NewPath = newPath,
                    StatusCode = 301,
                    IsActive = true,
                },
                ResponseType = ServiceResponseType.Success,
            });
        }
    }

    public sealed class NoopCommerceTransactionalMessageService : ICommerceTransactionalMessageService
    {
        public Task<QueuedMessageResult> QueueOrderPlacedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new QueuedMessageResult(true, null));
        }

        public Task<QueuedMessageResult> QueuePaymentStatusChangedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new QueuedMessageResult(true, null));
        }

        public Task<QueuedMessageResult> QueueFulfillmentStatusChangedAsync(
            Guid storeId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new QueuedMessageResult(true, null));
        }
    }
}
