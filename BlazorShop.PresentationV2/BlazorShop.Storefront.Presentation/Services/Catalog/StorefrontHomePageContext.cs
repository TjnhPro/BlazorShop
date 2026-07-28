namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;

public sealed record StorefrontHomePageContext(
    IReadOnlyList<GetCategory> Categories,
    IReadOnlyList<ProductSummaryItem> LatestProductSummaries,
    StorefrontDisplayContext DisplayContext,
    IReadOnlyDictionary<string, StorefrontCapability> FeatureCapabilities,
    StorefrontLinkContext Links);
