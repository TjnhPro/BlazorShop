namespace BlazorShop.Storefront.Presentation.Services.Content;

using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Presentation.Seo;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;

public sealed class StorefrontContentPageService
{
    private readonly IStorefrontContentClient contentClient;
    private readonly IStorefrontSeoComposer seoComposer;
    private readonly IStorefrontStructuredDataComposer structuredDataComposer;
    private readonly IStorefrontPagePresentationResolver presentationResolver;

    public StorefrontContentPageService(
        IStorefrontContentClient contentClient,
        IStorefrontSeoComposer seoComposer,
        IStorefrontStructuredDataComposer structuredDataComposer,
        IStorefrontPagePresentationResolver presentationResolver)
    {
        this.contentClient = contentClient;
        this.seoComposer = seoComposer;
        this.structuredDataComposer = structuredDataComposer;
        this.presentationResolver = presentationResolver;
    }

    public async Task<StorefrontContentPageResult> GetAsync(string? slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim();
        var routePath = StorefrontRoutes.Page(normalizedSlug);
        var pageResult = await this.contentClient.GetPublishedPageBySlugAsync(normalizedSlug, cancellationToken);

        if (pageResult.IsServiceUnavailable)
        {
            return new StorefrontContentPageResult(
                StorefrontPageResultMapper.ServiceUnavailable(StorefrontPageKind.Content, "Page temporarily unavailable"),
                null,
                await this.seoComposer.ComposeServiceUnavailablePageAsync(
                    "Page temporarily unavailable",
                    routePath,
                    "The storefront is running, but the page API is not reachable right now.",
                    cancellationToken),
                StorefrontStructuredDataDocument.Empty);
        }

        var page = pageResult.Value;
        if (page is null)
        {
            return new StorefrontContentPageResult(
                StorefrontPageResultMapper.NotFound(StorefrontPageKind.Content, "Page not found"),
                null,
                await this.seoComposer.ComposeNotFoundPageAsync(
                    "Page not found",
                    routePath,
                    "We couldn't find a published page for this address.",
                    cancellationToken),
                StorefrontStructuredDataDocument.Empty);
        }

        var presentation = this.presentationResolver.Resolve(page);
        var metadata = await this.seoComposer.ComposeStorefrontPageAsync(page, cancellationToken);
        var context = new StorefrontContentPageContext(
            page,
            presentation,
            [
                new StorefrontBreadcrumbItem("Home", StorefrontRoutes.Home),
                new StorefrontBreadcrumbItem(page.Title),
            ]);

        return new StorefrontContentPageResult(
            StorefrontPageResultMapper.Ready(
                StorefrontPageKind.Content,
                context,
                new StorefrontPageDocument(
                    metadata.Title,
                    metadata.MetaDescription,
                    metadata.CanonicalUrl,
                    metadata.RobotsIndex,
                    metadata.RobotsFollow)),
            context,
            metadata,
            await this.ComposeStructuredDataAsync(routePath, page, presentation, metadata, cancellationToken));
    }

    private Task<StorefrontStructuredDataDocument> ComposeStructuredDataAsync(
        string routePath,
        GetStorefrontPage page,
        StorefrontPagePresentation presentation,
        SeoMetadataDto metadata,
        CancellationToken cancellationToken)
    {
        var description = FirstNonEmpty(metadata.MetaDescription, page.Intro);
        return presentation.StructuredDataKind == StorefrontPageStructuredDataKind.FaqPage
            ? this.structuredDataComposer.ComposeFaqPageAsync(page.Title, routePath, description, presentation.FaqEntries, cancellationToken)
            : this.structuredDataComposer.ComposeWebPageAsync(page.Title, routePath, description, cancellationToken);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
