namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class StorefrontHomePageService
{
    private readonly IStorefrontCatalogClient _catalogClient;
    private readonly IStorefrontContentClient _contentClient;
    private readonly IStorefrontDisplayContextProvider _displayContextProvider;
    private readonly IStorefrontPriceFormatter _priceFormatter;
    private readonly IStorefrontStoreConfigurationClient _storeConfigurationClient;
    private readonly IStorefrontSeoComposer _seoComposer;
    private readonly IStorefrontStructuredDataComposer _structuredDataComposer;
    private readonly ILogger<StorefrontHomePageService> _logger;

    public StorefrontHomePageService(
        IStorefrontCatalogClient catalogClient,
        IStorefrontContentClient contentClient,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        IStorefrontStoreConfigurationClient storeConfigurationClient,
        IStorefrontSeoComposer seoComposer,
        IStorefrontStructuredDataComposer structuredDataComposer,
        ILogger<StorefrontHomePageService> logger)
    {
        _catalogClient = catalogClient;
        _contentClient = contentClient;
        _displayContextProvider = displayContextProvider;
        _priceFormatter = priceFormatter;
        _storeConfigurationClient = storeConfigurationClient;
        _seoComposer = seoComposer;
        _structuredDataComposer = structuredDataComposer;
        _logger = logger;
    }

    public async Task<StorefrontCatalogPageResult<StorefrontHomePageContext>> GetAsync(CancellationToken cancellationToken = default)
    {
        const string fallbackTitle = "Shop Home";
        const string fallbackDescription = "Discover published products, featured categories, and route-based SEO-ready pages across the BlazorShop storefront.";

        var displayContext = await _displayContextProvider.GetAsync();
        var homeMetadataPageTask = _contentClient.GetPublishedPageBySlugAsync(StorefrontRoutes.HomeMetadataSlug);
        var structuredDataTask = _structuredDataComposer.ComposeHomePageAsync(cancellationToken);
        var categoriesTask = _catalogClient.GetPublishedCategoriesAsync(cancellationToken);
        var publicConfigurationTask = _storeConfigurationClient.GetPublicConfigurationAsync(cancellationToken);
        var latestProductsTask = _catalogClient.GetPublishedCatalogPageAsync(new ProductCatalogQuery
        {
            PageNumber = 1,
            PageSize = 6,
            SortBy = ProductCatalogSortBy.Newest,
        }, displayContext.CurrencyCode, cancellationToken);

        await Task.WhenAll(homeMetadataPageTask, structuredDataTask, categoriesTask, publicConfigurationTask, latestProductsTask);

        var homeMetadataPageResult = await homeMetadataPageTask;
        var metadata = await _seoComposer.ComposeHomePageAsync(
            homeMetadataPageResult.IsSuccess ? homeMetadataPageResult.Value : null,
            fallbackTitle,
            fallbackDescription,
            cancellationToken);

        var categoriesResult = await categoriesTask;
        var latestProductsResult = await latestProductsTask;
        var publicConfigurationResult = await publicConfigurationTask;

        if (categoriesResult.IsServiceUnavailable || latestProductsResult.IsServiceUnavailable)
        {
            _logger.LogWarning("Home catalog data is temporarily unavailable.");
            var unavailableMetadata = await _seoComposer.ComposeServiceUnavailablePageAsync(
                "Catalog temporarily unavailable",
                StorefrontRoutes.Home,
                "The storefront is running, but the catalog API is not reachable right now.",
                cancellationToken);

            return new StorefrontCatalogPageResult<StorefrontHomePageContext>(
                StorefrontPageResultMapper.ServiceUnavailable(StorefrontPageKind.Home, "Catalog temporarily unavailable"),
                unavailableMetadata,
                StorefrontStructuredDataDocument.Empty,
                null);
        }

        var categories = categoriesResult.Value ?? [];
        var latestProducts = latestProductsResult.Value?.Items ?? [];
        var latestProductSummaries = latestProducts
            .Select(product => StorefrontProductSummaryMapper.ToProductSummary(product, displayContext, _priceFormatter))
            .ToArray();
        var context = new StorefrontHomePageContext(
            categories,
            latestProductSummaries,
            displayContext,
            MapCapabilities(publicConfigurationResult),
            StorefrontLinkContext.Default);
        var structuredData = await structuredDataTask;

        return new StorefrontCatalogPageResult<StorefrontHomePageContext>(
            StorefrontPageResultMapper.Ready(StorefrontPageKind.Home, context, new StorefrontPageDocument(fallbackTitle, fallbackDescription, StorefrontRoutes.Home)),
            metadata,
            structuredData,
            context);
    }

    private static IReadOnlyDictionary<string, StorefrontCapability> MapCapabilities(
        StorefrontApiResult<StorefrontPublicConfiguration> result)
    {
        if (!result.IsSuccess || result.Value?.Features is not { Count: > 0 } features)
        {
            return new Dictionary<string, StorefrontCapability>(StringComparer.Ordinal);
        }

        return new Dictionary<string, StorefrontCapability>(features, StringComparer.Ordinal);
    }
}
