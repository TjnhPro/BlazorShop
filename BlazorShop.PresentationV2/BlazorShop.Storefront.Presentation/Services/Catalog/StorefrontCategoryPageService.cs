namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Contracts;
using Microsoft.Extensions.Logging;

public sealed class StorefrontCategoryPageService
{
    private readonly IStorefrontCatalogClient _catalogClient;
    private readonly IStorefrontDisplayContextProvider _displayContextProvider;
    private readonly IStorefrontPriceFormatter _priceFormatter;
    private readonly IStorefrontSeoComposer _seoComposer;
    private readonly IStorefrontStructuredDataComposer _structuredDataComposer;
    private readonly ILogger<StorefrontCategoryPageService> _logger;

    public StorefrontCategoryPageService(
        IStorefrontCatalogClient catalogClient,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        IStorefrontSeoComposer seoComposer,
        IStorefrontStructuredDataComposer structuredDataComposer,
        ILogger<StorefrontCategoryPageService> logger)
    {
        _catalogClient = catalogClient;
        _displayContextProvider = displayContextProvider;
        _priceFormatter = priceFormatter;
        _seoComposer = seoComposer;
        _structuredDataComposer = structuredDataComposer;
        _logger = logger;
    }

    public async Task<StorefrontCatalogPageResult<StorefrontCategoryPageContext>> GetAsync(
        string slug,
        decimal? minPrice,
        decimal? maxPrice,
        bool inStock,
        int page,
        int pageSize,
        string? sortByQuery,
        CancellationToken cancellationToken = default)
    {
        var displayContext = await _displayContextProvider.GetAsync();
        var routePath = StorefrontRoutes.Category(slug);
        var sortBy = ProductCatalogSortByExtensions.TryParseApiValue(sortByQuery, out var parsedSortBy)
            ? parsedSortBy
            : ProductCatalogSortBy.DisplayOrder;
        var categoryPageResult = await _catalogClient.GetPublishedCategoryBySlugAsync(slug, displayContext.CurrencyCode, cancellationToken);

        if (categoryPageResult.IsServiceUnavailable)
        {
            _logger.LogWarning("Category {Slug} is temporarily unavailable.", slug);
            var unavailableMetadata = await _seoComposer.ComposeServiceUnavailablePageAsync(
                "Category temporarily unavailable",
                routePath,
                "The storefront is running, but the catalog API is not reachable right now.",
                cancellationToken);
            return new StorefrontCatalogPageResult<StorefrontCategoryPageContext>(
                StorefrontPageResultMapper.ServiceUnavailable(StorefrontPageKind.Category, "Category temporarily unavailable"),
                unavailableMetadata,
                StorefrontStructuredDataDocument.Empty,
                null);
        }

        var categoryPage = categoryPageResult.Value;
        if (categoryPage is null)
        {
            _logger.LogInformation("Category {Slug} was not found.", slug);
            var notFoundMetadata = await _seoComposer.ComposeNotFoundPageAsync(
                "Category not found",
                routePath,
                "We couldn't find a published category for this address.",
                cancellationToken);
            return new StorefrontCatalogPageResult<StorefrontCategoryPageContext>(
                StorefrontPageResultMapper.NotFound(StorefrontPageKind.Category, "Category not found"),
                notFoundMetadata,
                StorefrontStructuredDataDocument.Empty,
                null);
        }

        var breadcrumbs = new List<StorefrontBreadcrumbItem>
        {
            new("Home", StorefrontRoutes.Home),
        };
        breadcrumbs.AddRange(categoryPage.Breadcrumbs.Select((crumb, index) => new StorefrontBreadcrumbItem(
            crumb.Name ?? "Category",
            index == categoryPage.Breadcrumbs.Count - 1 || string.IsNullOrWhiteSpace(crumb.Slug)
                ? null
                : StorefrontRoutes.Category(crumb.Slug))));

        var filteredResult = await _catalogClient.GetPublishedCatalogPageAsync(new ProductCatalogQuery
        {
            PageNumber = Math.Max(1, page),
            PageSize = NormalizePageSize(pageSize),
            CategoryId = categoryPage.Category.Id,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStock = inStock ? true : null,
            SortBy = sortBy,
        }, displayContext.CurrencyCode, cancellationToken);

        var products = filteredResult.IsSuccess && filteredResult.Value is not null
            ? filteredResult.Value.Items
            : categoryPage.Products;
        var summaries = products.Select(product => StorefrontProductSummaryMapper.ToProductSummary(product, displayContext, _priceFormatter)).ToArray();
        var context = new StorefrontCategoryPageContext(
            categoryPage,
            breadcrumbs,
            summaries,
            filteredResult.IsSuccess && filteredResult.Value is not null ? filteredResult.Value.PageNumber : 1,
            filteredResult.IsSuccess && filteredResult.Value is not null ? filteredResult.Value.PageSize : NormalizePageSize(pageSize),
            filteredResult.IsSuccess && filteredResult.Value is not null ? filteredResult.Value.TotalCount : categoryPage.DirectProductCount,
            filteredResult.IsSuccess && filteredResult.Value is not null ? Math.Min(10, filteredResult.Value.TotalPages) : 0,
            minPrice,
            maxPrice,
            inStock,
            sortBy,
            slug,
            StorefrontLinkContext.Default);

        var readyMetadata = await _seoComposer.ComposeCategoryPageAsync(categoryPage.Category, cancellationToken);
        var structuredData = await _structuredDataComposer.ComposeCategoryPageAsync(categoryPage.Category, cancellationToken);

        return new StorefrontCatalogPageResult<StorefrontCategoryPageContext>(
            StorefrontPageResultMapper.Ready(StorefrontPageKind.Category, context, new StorefrontPageDocument(categoryPage.Category.Name, categoryPage.Category.MetaDescription, routePath)),
            readyMetadata,
            structuredData,
            context);
    }

    private static int NormalizePageSize(int pageSize)
    {
        return CatalogSearchPolicy.StorefrontPageSizes.Contains(pageSize)
            ? pageSize
            : 24;
    }
}
