namespace BlazorShop.Storefront.Services
{

    using BlazorShop.Storefront.Models;
using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public partial class StorefrontApiClient
    {
        public async Task<StorefrontApiResult<IReadOnlyList<GetCategory>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsyncWithFallback<List<GetCategory>>(
                StorefrontCategoriesRoute,
                LegacyCategoriesRoute,
                cancellationToken,
                []);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<GetCategory>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<GetCategory>>.ServiceUnavailable()
                : StorefrontApiResult<IReadOnlyList<GetCategory>>.Success([]);
        }
        public async Task<StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<GetCategoryTreeNode>>(
                StorefrontCategoryTreeRoute,
                cancellationToken,
                [],
                CatalogRequestTimeout);

            return result.IsSuccess && result.Value is not null
                ? StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.Success(result.Value)
                : result.IsServiceUnavailable
                    ? StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.ServiceUnavailable()
                    : StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>.Success([]);
        }
        public Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback(
                StorefrontCatalogSitemapRoute,
                LegacyCatalogSitemapRoute,
                cancellationToken,
                new GetPublicCatalogSitemap(),
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, CancellationToken cancellationToken = default)
        {
            return GetPublishedCatalogPageAsync(query, currencyCode: null, cancellationToken);
        }
        public Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(
            ProductCatalogQuery query,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            return GetAsyncWithFallback(
                BuildCatalogRoute(query, StorefrontProductsRoute, currencyCode),
                BuildCatalogRoute(query, LegacyProductsRoute),
                cancellationToken,
                new PagedResult<GetCatalogProduct>(),
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetPublishedCategoryBySlugAsync(slug, currencyCode: null, cancellationToken);
        }
        public Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetCategoryPage>(
                AppendCurrencyQuery($"{StorefrontCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}", currencyCode),
                $"{LegacyCategoriesRoute}/slug/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
            string? categorySlug = null,
            string? searchTerm = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return GetAsync<StorefrontProductFilterMetadataResponse>(
                BuildProductFilterMetadataRoute(categorySlug, searchTerm, currencyCode),
                cancellationToken,
                fallbackValue: null,
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
            string? searchTerm,
            string? categorySlug = null,
            int? limit = null,
            string? currencyCode = null,
            CancellationToken cancellationToken = default)
        {
            return GetAsync<StorefrontSearchSuggestionResponse>(
                BuildSearchSuggestionsRoute(searchTerm, categorySlug, limit, currencyCode),
                cancellationToken,
                fallbackValue: null,
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return GetPublishedProductBySlugAsync(slug, currencyCode: null, cancellationToken);
        }
        public Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(
            string slug,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            return GetMaybeNotFoundWithFallbackAsync<GetProduct>(
                AppendCurrencyQuery($"{StorefrontProductsRoute}/slug/{Uri.EscapeDataString(slug)}", currencyCode),
                $"{LegacyProductsRoute}/slug/{Uri.EscapeDataString(slug)}",
                cancellationToken,
                CatalogRequestTimeout);
        }
        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return GetProductByIdAsync(id, currencyCode: null, cancellationToken);
        }
        public Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(
            Guid id,
            string? currencyCode,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                return Task.FromResult(StorefrontApiResult<GetProduct>.NotFound());
            }

            return GetMaybeNotFoundWithFallbackAsync<GetProduct>(
                AppendCurrencyQuery($"{StorefrontProductsRoute}/{id}", currencyCode),
                $"/api/product/single/{id}",
                cancellationToken,
                CatalogRequestTimeout);
        }
        public Task<StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
            Guid productId,
            StorefrontProductSelectionPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>.Failed("Product is required."));
            }

            return PostAsync<StorefrontProductSelectionPreviewRequest, StorefrontProductSelectionPreviewResponse>(
                $"{StorefrontProductsRoute}/{productId:D}/selection-preview",
                request,
                "Unable to preview this product selection right now.",
                cancellationToken);
        }
    }
}
