namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;

public record StorefrontCatalogProductsPageContext(
    IReadOnlyList<ProductSummaryItem> ProductSummaries);

public sealed record StorefrontDealsPageContext(
    IReadOnlyList<ProductSummaryItem> ProductSummaries)
    : StorefrontCatalogProductsPageContext(ProductSummaries);

public sealed record StorefrontNewReleasesPageContext(
    IReadOnlyList<ProductSummaryItem> ProductSummaries)
    : StorefrontCatalogProductsPageContext(ProductSummaries);
