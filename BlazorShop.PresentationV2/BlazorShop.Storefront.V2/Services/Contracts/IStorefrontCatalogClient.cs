namespace BlazorShop.Storefront.Services.Contracts
{

    using BlazorShop.Storefront.Models;
using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;

    using BlazorShop.Storefront.Services;

    public interface IStorefrontCatalogClient
    {
        Task<StorefrontApiResult<IReadOnlyList<GetCategory>>> GetPublishedCategoriesAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<IReadOnlyList<GetCategoryTreeNode>>> GetPublishedCategoryTreeAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetPublicCatalogSitemap>> GetPublishedSitemapAsync(CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(ProductCatalogQuery query, CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<PagedResult<GetCatalogProduct>>> GetPublishedCatalogPageAsync(
                    ProductCatalogQuery query,
                    string? currencyCode,
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetCategoryPage>> GetPublishedCategoryBySlugAsync(
                    string slug,
                    string? currencyCode,
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontProductFilterMetadataResponse>> GetProductFilterMetadataAsync(
                    string? categorySlug = null,
                    string? searchTerm = null,
                    string? currencyCode = null,
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<StorefrontSearchSuggestionResponse>> GetSearchSuggestionsAsync(
                    string? searchTerm,
                    string? categorySlug = null,
                    int? limit = null,
                    string? currencyCode = null,
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(string slug, CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetProduct>> GetPublishedProductBySlugAsync(
                    string slug,
                    string? currencyCode,
                    CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<StorefrontApiResult<GetProduct>> GetProductByIdAsync(
                    Guid id,
                    string? currencyCode,
                    CancellationToken cancellationToken = default);

        Task<StorefrontSubmitResult<StorefrontProductSelectionPreviewResponse>> PreviewProductSelectionAsync(
                    Guid productId,
                    StorefrontProductSelectionPreviewRequest request,
                    CancellationToken cancellationToken = default);
    }
}
