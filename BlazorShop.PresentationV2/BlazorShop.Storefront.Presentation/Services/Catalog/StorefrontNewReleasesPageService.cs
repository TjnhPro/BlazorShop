namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.Extensions.Logging;

public sealed class StorefrontNewReleasesPageService
{
    private readonly IStorefrontCatalogClient _catalogClient;
    private readonly IStorefrontDisplayContextProvider _displayContextProvider;
    private readonly IStorefrontPriceFormatter _priceFormatter;
    private readonly IStorefrontSeoComposer _seoComposer;
    private readonly IStorefrontStructuredDataComposer _structuredDataComposer;
    private readonly ILogger<StorefrontNewReleasesPageService> _logger;

    public StorefrontNewReleasesPageService(
        IStorefrontCatalogClient catalogClient,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        IStorefrontSeoComposer seoComposer,
        IStorefrontStructuredDataComposer structuredDataComposer,
        ILogger<StorefrontNewReleasesPageService> logger)
    {
        _catalogClient = catalogClient;
        _displayContextProvider = displayContextProvider;
        _priceFormatter = priceFormatter;
        _seoComposer = seoComposer;
        _structuredDataComposer = structuredDataComposer;
        _logger = logger;
    }

    public Task<StorefrontCatalogPageResult<StorefrontCatalogProductsPageContext>> GetAsync(CancellationToken cancellationToken = default)
    {
        return LoadAsync(StorefrontRoutes.NewReleases, "New Releases", "Browse newly published BlazorShop products on stable route-based pages.", "New releases are temporarily unavailable", 24, cancellationToken);
    }

    private async Task<StorefrontCatalogPageResult<StorefrontCatalogProductsPageContext>> LoadAsync(
        string route,
        string title,
        string description,
        string unavailableTitle,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var displayContext = await _displayContextProvider.GetAsync();
        var metadataTask = _seoComposer.ComposeStaticPageAsync(title, route, description, cancellationToken);
        var structuredDataTask = _structuredDataComposer.ComposeCollectionPageAsync(title, route, description, cancellationToken);
        var result = await _catalogClient.GetPublishedCatalogPageAsync(new ProductCatalogQuery
        {
            PageNumber = 1,
            PageSize = pageSize,
            SortBy = ProductCatalogSortBy.Newest,
        }, displayContext.CurrencyCode, cancellationToken);

        var metadata = await metadataTask;

        if (result.IsServiceUnavailable)
        {
            _logger.LogWarning("{Route} is temporarily unavailable.", route);
            metadata = await _seoComposer.ComposeServiceUnavailablePageAsync(unavailableTitle, route, "The storefront is running, but the catalog API is not reachable right now.", cancellationToken);
            return new StorefrontCatalogPageResult<StorefrontCatalogProductsPageContext>(
                StorefrontPageResultMapper.ServiceUnavailable(StorefrontPageKind.NewReleases, unavailableTitle),
                metadata,
                StorefrontStructuredDataDocument.Empty,
                null);
        }

        var products = result.Value?.Items ?? [];
        var productSummaries = products.Select(product => StorefrontProductSummaryMapper.ToProductSummary(product, displayContext, _priceFormatter)).ToArray();
        var context = new StorefrontCatalogProductsPageContext(productSummaries);
        var structuredData = await structuredDataTask;

        return new StorefrontCatalogPageResult<StorefrontCatalogProductsPageContext>(
            StorefrontPageResultMapper.Ready(StorefrontPageKind.NewReleases, context, new StorefrontPageDocument(title, description, route)),
            metadata,
            structuredData,
            context);
    }
}
