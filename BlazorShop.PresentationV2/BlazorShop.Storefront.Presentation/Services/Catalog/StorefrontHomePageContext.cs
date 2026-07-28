namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Contracts;

public sealed record StorefrontHomePageContext(
    IReadOnlyList<GetCategory> Categories,
    IReadOnlyList<ProductSummaryItem> LatestProductSummaries,
    StorefrontDisplayContext DisplayContext,
    IReadOnlyDictionary<string, StorefrontCapability> FeatureCapabilities,
    StorefrontLinkContext Links);
