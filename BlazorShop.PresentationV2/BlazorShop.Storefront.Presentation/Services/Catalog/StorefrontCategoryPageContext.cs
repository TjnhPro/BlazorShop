namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Contracts;

public sealed record StorefrontCategoryPageContext(
    GetCategoryPage CategoryPage,
    IReadOnlyList<StorefrontBreadcrumbItem> Breadcrumbs,
    IReadOnlyList<ProductSummaryItem> ProductSummaries,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool InStock,
    ProductCatalogSortBy SortBy,
    string Slug,
    StorefrontLinkContext Links);
