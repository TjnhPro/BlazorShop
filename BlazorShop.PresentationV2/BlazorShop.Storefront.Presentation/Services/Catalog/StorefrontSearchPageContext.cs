namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;

public sealed record StorefrontSearchPageContext(
    string? Q,
    string? Category,
    IReadOnlyList<CatalogFilterCategoryOption> SearchCategories,
    IReadOnlyList<ProductSummaryItem> ProductSummaries,
    bool IsSearchTermTooShort,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool InStock,
    ProductCatalogSortBy SortBy);
