namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Presentation.Services;

public sealed record StorefrontCatalogPageResult<TContext>(
    StorefrontPageState State,
    SeoMetadataDto Metadata,
    StorefrontStructuredDataDocument StructuredData,
    TContext? Context)
{
    public static StorefrontCatalogPageResult<TContext> Empty { get; } = new(
        new StorefrontPageState.LoadingState(),
        new SeoMetadataDto(),
        StorefrontStructuredDataDocument.Empty,
        default);
}
