namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Models;

public sealed record StorefrontHomePageContext(
    IReadOnlyList<GetCategory> Categories,
    IReadOnlyList<ProductSummaryItem> LatestProductSummaries);
