namespace BlazorShop.Storefront.Presentation.Services.Product;

using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.Extensions.Logging;

public sealed class StorefrontProductPageService
{
    private readonly IStorefrontCatalogClient _catalogClient;
    private readonly IStorefrontSeoComposer _seoComposer;
    private readonly IStorefrontStructuredDataComposer _structuredDataComposer;
    private readonly IStorefrontDisplayContextProvider _displayContextProvider;
    private readonly IStorefrontPriceFormatter _priceFormatter;
    private readonly ILogger<StorefrontProductPageService> _logger;

    public StorefrontProductPageService(
        IStorefrontCatalogClient catalogClient,
        IStorefrontSeoComposer seoComposer,
        IStorefrontStructuredDataComposer structuredDataComposer,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        ILogger<StorefrontProductPageService> logger)
    {
        _catalogClient = catalogClient;
        _seoComposer = seoComposer;
        _structuredDataComposer = structuredDataComposer;
        _displayContextProvider = displayContextProvider;
        _priceFormatter = priceFormatter;
        _logger = logger;
    }

    public async Task<StorefrontProductPageResult> ResolveAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var displayContext = await _displayContextProvider.GetAsync(cancellationToken);
        var routePath = StorefrontRoutes.Product(slug);
        var productResult = await _catalogClient.GetPublishedProductBySlugAsync(slug, displayContext.CurrencyCode, cancellationToken);

        if (productResult.IsServiceUnavailable)
        {
            SeoRuntimeLogger.PublicProductServiceUnavailable(_logger, routePath, slug);
            return new StorefrontProductPageResult(
                StorefrontPageResultMapper.ServiceUnavailable(
                    StorefrontPageKind.Product,
                    "The storefront is running, but the catalog API is not reachable right now."),
                await _seoComposer.ComposeServiceUnavailablePageAsync(
                    "Product temporarily unavailable",
                    routePath,
                    "The storefront is running, but the catalog API is not reachable right now."),
                StorefrontStructuredDataDocument.Empty);
        }

        var product = productResult.Value;
        if (product is null)
        {
            SeoRuntimeLogger.PublicProductNotFound(_logger, routePath, slug);
            return new StorefrontProductPageResult(
                StorefrontPageResultMapper.NotFound(
                    StorefrontPageKind.Product,
                    "We couldn't find a published product for this address."),
                await _seoComposer.ComposeNotFoundPageAsync(
                    "Product not found",
                    routePath,
                    "We couldn't find a published product for this address."),
                StorefrontStructuredDataDocument.Empty);
        }

        SeoRuntimeLogger.PublicProductResolved(_logger, routePath, slug, product.Id);

        var metadataTask = _seoComposer.ComposeProductPageAsync(product);
        var structuredDataTask = _structuredDataComposer.ComposeProductPageAsync(product);
        var relatedProductsTask = GetRelatedProductsAsync(product, displayContext, cancellationToken);

        await Task.WhenAll(metadataTask, structuredDataTask, relatedProductsTask);

        var metadata = await metadataTask;
        var structuredData = await structuredDataTask;
        var relatedProducts = await relatedProductsTask;
        var context = StorefrontProductPageMapper.Map(product, relatedProducts, displayContext, _priceFormatter);

        return new StorefrontProductPageResult(
            StorefrontPageResultMapper.Ready(
                StorefrontPageKind.Product,
                context,
                new StorefrontPageDocument(
                    metadata.Title,
                    metadata.MetaDescription,
                    metadata.CanonicalUrl,
                    metadata.RobotsIndex,
                    metadata.RobotsFollow)),
            metadata,
            structuredData);
    }

    private async Task<IReadOnlyList<GetCatalogProduct>> GetRelatedProductsAsync(
        GetProduct product,
        StorefrontDisplayContext displayContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(product.Category?.Slug))
        {
            return [];
        }

        var categoryPageResult = await _catalogClient.GetPublishedCategoryBySlugAsync(
            product.Category.Slug,
            displayContext.CurrencyCode,
            cancellationToken);
        if (!categoryPageResult.IsSuccess || categoryPageResult.Value is null)
        {
            return [];
        }

        return categoryPageResult.Value.Products
            .Where(relatedProduct => relatedProduct.Id != product.Id)
            .Where(relatedProduct => !string.IsNullOrWhiteSpace(relatedProduct.Slug))
            .Take(3)
            .ToArray();
    }
}
