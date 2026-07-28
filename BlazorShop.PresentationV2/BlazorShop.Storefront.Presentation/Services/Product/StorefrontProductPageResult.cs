namespace BlazorShop.Storefront.Presentation.Services.Product;

using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Presentation.Services;

public sealed record StorefrontProductPageResult(
    StorefrontPageState State,
    SeoMetadataDto Metadata,
    StorefrontStructuredDataDocument StructuredData)
{
    public static StorefrontProductPageResult Empty { get; } = new(
        new StorefrontPageState.LoadingState(),
        new SeoMetadataDto(),
        StorefrontStructuredDataDocument.Empty);
}
