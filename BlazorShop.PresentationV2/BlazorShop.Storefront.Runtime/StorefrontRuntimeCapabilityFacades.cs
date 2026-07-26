namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;

    public sealed class StorefrontRuntimeCatalogFacade : IStorefrontRuntimeCatalogFacade
    {
        private readonly IStorefrontRuntimeCatalogContentFacade inner;

        public StorefrontRuntimeCatalogFacade(IStorefrontRuntimeCatalogContentFacade inner)
        {
            this.inner = inner;
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontCategoryResponse>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return this.inner.GetPublishedCategoriesAsync(cancellationToken);
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontCategoryTreeNodeResponse>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            return this.inner.GetPublishedCategoryTreeAsync(cancellationToken);
        }

        public Task<StorefrontRuntimeResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return this.inner.GetPublishedSitemapAsync(cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontCatalogProductResponseStorefrontPagedResponse>> GetPublishedCatalogPageAsync(
            StorefrontRuntimeProductCatalogQuery query,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetPublishedCatalogPageAsync(query, currencyCode, cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontCategoryPageResponse>> GetPublishedCategoryBySlugAsync(
            string slug,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetPublishedCategoryBySlugAsync(slug, currencyCode, cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
            string? categorySlug = null,
            string? searchTerm = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetProductFilterMetadataAsync(categorySlug, searchTerm, currencyCode, cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
            string? searchTerm,
            string? categorySlug = null,
            int? limit = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetSearchSuggestionsAsync(searchTerm, categorySlug, limit, currencyCode, cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontProductResponse>> GetPublishedProductBySlugAsync(
            string slug,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetPublishedProductBySlugAsync(slug, currencyCode, cancellationToken);
        }

        public Task<StorefrontRuntimeResult<StorefrontProductResponse>> GetProductByIdAsync(
            Guid id,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetProductByIdAsync(id, currencyCode, cancellationToken);
        }

        public Task<StorefrontRuntimeSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
            Guid productId,
            StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.inner.PreviewProductSelectionAsync(productId, request, cancellationToken);
        }
    }

    public sealed class StorefrontRuntimeContentFacade : IStorefrontRuntimeContentFacade
    {
        private readonly IStorefrontRuntimeCatalogContentFacade inner;

        public StorefrontRuntimeContentFacade(IStorefrontRuntimeCatalogContentFacade inner)
        {
            this.inner = inner;
        }

        public Task<StorefrontRuntimeResult<StorefrontPagePublicDto>> GetPublishedPageBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetPublishedPageBySlugAsync(slug, cancellationToken);
        }

        public Task<StorefrontRuntimeResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetPageNavigationLinksAsync(cancellationToken);
        }
    }

    public sealed class StorefrontRuntimeNavigationFacade : IStorefrontRuntimeNavigationFacade
    {
        private readonly IStorefrontRuntimeCatalogContentFacade inner;

        public StorefrontRuntimeNavigationFacade(IStorefrontRuntimeCatalogContentFacade inner)
        {
            this.inner = inner;
        }

        public Task<StorefrontRuntimeResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetNavigationMenuAsync(systemName, cancellationToken);
        }
    }

    public sealed class StorefrontRuntimeSeoFacade : IStorefrontRuntimeSeoFacade
    {
        private readonly IStorefrontRuntimeCatalogContentFacade inner;

        public StorefrontRuntimeSeoFacade(IStorefrontRuntimeCatalogContentFacade inner)
        {
            this.inner = inner;
        }

        public Task<StorefrontRuntimeResult<SeoSettingsDto>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
        {
            return this.inner.GetSeoSettingsAsync(cancellationToken);
        }

        public Task<StorefrontRuntimeResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return this.inner.GetRedirectResolutionAsync(path, cancellationToken);
        }
    }
}
