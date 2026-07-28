namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Services.Contracts;

public record StorefrontCatalogProductsPageContext(
    IReadOnlyList<ProductSummaryItem> ProductSummaries,
    StorefrontLinkContext Links);

public sealed record StorefrontDealsPageContext(
    IReadOnlyList<ProductSummaryItem> ProductSummaries,
    StorefrontLinkContext Links)
    : StorefrontCatalogProductsPageContext(ProductSummaries, Links);

public sealed record StorefrontNewReleasesPageContext(
    IReadOnlyList<ProductSummaryItem> ProductSummaries,
    StorefrontLinkContext Links)
    : StorefrontCatalogProductsPageContext(ProductSummaries, Links);
