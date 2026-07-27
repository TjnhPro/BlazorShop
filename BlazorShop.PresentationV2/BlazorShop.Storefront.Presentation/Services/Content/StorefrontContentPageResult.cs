namespace BlazorShop.Storefront.Presentation.Services.Content;

using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Presentation.Seo;
using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Services;

public sealed record StorefrontContentPageResult(
    StorefrontPageState State,
    StorefrontContentPageContext? Context,
    SeoMetadataDto Metadata,
    StorefrontStructuredDataDocument StructuredData)
{
    public static StorefrontContentPageResult Empty { get; } = new(
        StorefrontPageResultMapper.ServiceUnavailable(StorefrontPageKind.Content),
        null,
        new SeoMetadataDto(),
        StorefrontStructuredDataDocument.Empty);
}
