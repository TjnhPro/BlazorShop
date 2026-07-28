namespace BlazorShop.Storefront.Presentation.Services
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using BlazorShop.Storefront.Presentation.Models;
    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Presentation.Contracts;

    using GeneratedClients = BlazorShop.Storefront.Client;

    public sealed class GeneratedStorefrontCatalogContentClient : IStorefrontCatalogClient, IStorefrontContentClient, IStorefrontSitemapReader, IStorefrontSeoSettingsReader
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IStorefrontRuntimeCatalogFacade catalogFacade;
        private readonly IStorefrontRuntimeContentFacade contentFacade;
        private readonly IStorefrontRuntimeNavigationFacade navigationFacade;
        private readonly IStorefrontRuntimeSeoFacade seoFacade;

        public GeneratedStorefrontCatalogContentClient(
            IStorefrontRuntimeCatalogFacade catalogFacade,
            IStorefrontRuntimeContentFacade contentFacade,
            IStorefrontRuntimeNavigationFacade navigationFacade,
            IStorefrontRuntimeSeoFacade seoFacade)
        {
            this.catalogFacade = catalogFacade;
            this.contentFacade = contentFacade;
            this.navigationFacade = navigationFacade;
            this.seoFacade = seoFacade;
        }

        public async Task<StorefrontApiResult<IReadOnlyList<GetCategory>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetPublishedCategoriesAsync(cancellationToken);
            return MapListResult<GeneratedClients.StorefrontCategoryResponse, GetCategory>(result, fallbackEmpty: true);
        }

        public async Task<StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetPublishedCategoryTreeAsync(cancellationToken);
            return MapListResult<GeneratedClients.StorefrontCategoryTreeNodeResponse, GetCategoryTreeNode>(result, fallbackEmpty: true);
        }

        public async Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetPublishedSitemapAsync(cancellationToken);
            return MapResult<GeneratedClients.GetPublicCatalogSitemap, GetPublicCatalogSitemap>(result, fallbackValue: new GetPublicCatalogSitemap());
        }

        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return GetPublishedCatalogPageAsync(query, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(
            ProductCatalogQuery query,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetPublishedCatalogPageAsync(MapCatalogQuery(query), currencyCode, cancellationToken);
            return MapResult<GeneratedClients.StorefrontCatalogProductResponseStorefrontPagedResponse, PagedResult<GetCatalogProduct>>(
                result,
                fallbackValue: new PagedResult<GetCatalogProduct>());
        }

        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return GetPublishedCategoryBySlugAsync(slug, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetPublishedCategoryBySlugAsync(slug, currencyCode, cancellationToken);
            return MapResult<GeneratedClients.StorefrontCategoryPageResponse, GetCategoryPage>(result);
        }

        public async Task<StorefrontApiResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
            string? categorySlug = null,
            string? searchTerm = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetProductFilterMetadataAsync(categorySlug, searchTerm, currencyCode, cancellationToken);
            return MapResult<GeneratedClients.StorefrontProductFilterMetadataResponse, StorefrontProductFilterMetadataResponse>(result);
        }

        public async Task<StorefrontApiResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
            string? searchTerm,
            string? categorySlug = null,
            int? limit = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetSearchSuggestionsAsync(searchTerm, categorySlug, limit, currencyCode, cancellationToken);
            return MapResult<GeneratedClients.StorefrontSearchSuggestionResponse, StorefrontSearchSuggestionResponse>(result);
        }

        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            return GetPublishedProductBySlugAsync(slug, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetPublishedProductBySlugAsync(slug, currencyCode, cancellationToken);
            return MapResult<GeneratedClients.StorefrontProductResponse, GetProduct>(result);
        }

        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return GetProductByIdAsync(id, currencyCode: null, cancellationToken);
        }

        public async Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(
            Guid id,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.GetProductByIdAsync(id, currencyCode, cancellationToken);
            return MapResult<GeneratedClients.StorefrontProductResponse, GetProduct>(result);
        }

        public async Task<StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
            Guid productId,
            StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.catalogFacade.PreviewProductSelectionAsync(
                productId,
                Project<GeneratedClients.StorefrontProductSelectionPreviewRequest>(request),
                cancellationToken);
            return result.Success
                ? StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Succeeded(
                    result.Value is null ? null : Project<StorefrontProductSelectionPreviewResponse>(result.Value),
                    "Request completed.")
                : StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed(result.Error?.Message);
        }

        public async Task<StorefrontApiResult<GetStorefrontPage>> GetPublishedPageBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            var result = await this.contentFacade.GetPublishedPageBySlugAsync(slug, cancellationToken);
            return MapResult<GeneratedClients.StorefrontPagePublicDto, GetStorefrontPage>(result);
        }

        public async Task<StorefrontApiResult<IReadOnlyList<StorefrontPageNavigationLinkDto>>> GetPageNavigationLinksAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await this.contentFacade.GetPageNavigationLinksAsync(cancellationToken);
            return MapListResult<GeneratedClients.StorefrontPageNavigationLinkDto, StorefrontPageNavigationLinkDto>(result, fallbackEmpty: true);
        }

        public async Task<StorefrontApiResult<StoreNavigationPublicMenuDto>> GetNavigationMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            var result = await this.navigationFacade.GetNavigationMenuAsync(systemName, cancellationToken);
            return MapResult<GeneratedClients.StoreNavigationPublicMenuDto, StoreNavigationPublicMenuDto>(result);
        }

        public async Task<StorefrontApiResult<GetSeoSettings>> GetSeoSettingsAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.seoFacade.GetSeoSettingsAsync(cancellationToken);
            return MapResult<GeneratedClients.SeoSettingsDto, GetSeoSettings>(result);
        }

        public async Task<StorefrontApiResult<SeoRedirectResolutionDto>> GetRedirectResolutionAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var result = await this.seoFacade.GetRedirectResolutionAsync(path, cancellationToken);
            return MapResult<GeneratedClients.SeoRedirectResolutionDto, SeoRedirectResolutionDto>(result);
        }

        private static StorefrontRuntimeProductCatalogQuery MapCatalogQuery(ProductCatalogQuery query)
        {
            return new StorefrontRuntimeProductCatalogQuery(
                query.PageNumber,
                query.PageSize,
                query.CategoryId,
                query.CategorySlug,
                query.IncludeSubcategories,
                query.SearchTerm,
                query.MinPrice,
                query.MaxPrice,
                query.InStock,
                query.SortBy.ToApiValue(),
                query.CreatedAfterUtc);
        }

        private static StorefrontApiResult<IReadOnlyList<TLocal>> MapListResult<TGenerated, TLocal>(
            StorefrontRuntimeResult<IReadOnlyList<TGenerated>> result,
            bool fallbackEmpty)
        {
            if (result.Success && result.Value is not null)
            {
                return StorefrontApiResult<IReadOnlyList<TLocal>>.Success(result.Value.Select(item => Project<TLocal>(item!)).ToArray());
            }

            return fallbackEmpty && result.Error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<IReadOnlyList<TLocal>>.Success([])
                : MapFailure<IReadOnlyList<TLocal>>(result.Error);
        }

        private static StorefrontApiResult<TLocal> MapResult<TGenerated, TLocal>(
            StorefrontRuntimeResult<TGenerated> result,
            TLocal? fallbackValue = default)
        {
            if (result.Success && result.Value is not null)
            {
                return StorefrontApiResult<TLocal>.Success(Project<TLocal>(result.Value));
            }

            return fallbackValue is not null && result.Error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<TLocal>.Success(fallbackValue)
                : MapFailure<TLocal>(result.Error);
        }

        private static StorefrontApiResult<T> MapFailure<T>(StorefrontRuntimeError? error)
        {
            return error?.Status == StorefrontRuntimeStatusCodes.ServiceUnavailable
                ? StorefrontApiResult<T>.ServiceUnavailable()
                : StorefrontApiResult<T>.NotFound();
        }

        private static TTarget Project<TTarget>(object source)
        {
            return JsonSerializer.Deserialize<TTarget>(JsonSerializer.Serialize(source, JsonOptions), JsonOptions)
                ?? throw new InvalidOperationException($"Could not project generated Storefront DTO to {typeof(TTarget).Name}.");
        }
    }
}
