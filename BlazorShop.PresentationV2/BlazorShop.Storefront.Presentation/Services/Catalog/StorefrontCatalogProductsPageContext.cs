namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;

public sealed record StorefrontCatalogProductsPageContext(
    IReadOnlyList<ProductSummaryItem> ProductSummaries);
