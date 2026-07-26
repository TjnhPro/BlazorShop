namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using BlazorShop.Storefront.Models;
using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Presentation.PagePatterns;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.Extensions.Logging;

public sealed class StorefrontSearchPageService
{
    private readonly IStorefrontCatalogClient _catalogClient;
    private readonly IStorefrontDisplayContextProvider _displayContextProvider;
    private readonly IStorefrontPriceFormatter _priceFormatter;
    private readonly IStorefrontSeoComposer _seoComposer;
    private readonly ILogger<StorefrontSearchPageService> _logger;

    public StorefrontSearchPageService(
        IStorefrontCatalogClient catalogClient,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        IStorefrontSeoComposer seoComposer,
        ILogger<StorefrontSearchPageService> logger)
    {
        _catalogClient = catalogClient;
        _displayContextProvider = displayContextProvider;
        _priceFormatter = priceFormatter;
        _seoComposer = seoComposer;
        _logger = logger;
    }

    public async Task<StorefrontCatalogPageResult<StorefrontSearchPageContext>> GetAsync(
        string? query,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        bool inStock,
        int page,
        int pageSize,
        string? sortByQuery,
        CancellationToken cancellationToken = default)
    {
        var displayContext = await _displayContextProvider.GetAsync();
        var normalizedQuery = CatalogSearchPolicy.NormalizeSearchTerm(query);
        var isTooShort = CatalogSearchPolicy.IsSearchTermTooShort(normalizedQuery);
        var sortBy = ProductCatalogSortByExtensions.TryParseApiValue(sortByQuery, out var parsedSortBy)
            ? parsedSortBy
            : ProductCatalogSortBy.DisplayOrder;
        var metadataTask = _seoComposer.ComposeStaticPageAsync(
            "Search",
            StorefrontRoutes.Search,
            "Search published products in the BlazorShop catalog.",
            cancellationToken);
        var categoriesTask = _catalogClient.GetPublishedCategoryTreeAsync(cancellationToken);

        StorefrontApiResult<PagedResult<GetCatalogProduct>>? productsResult = null;
        if (!isTooShort)
        {
            productsResult = await _catalogClient.GetPublishedCatalogPageAsync(new ProductCatalogQuery
            {
                PageNumber = Math.Max(1, page),
                PageSize = NormalizePageSize(pageSize),
                CategorySlug = category,
                IncludeSubcategories = !string.IsNullOrWhiteSpace(category),
                SearchTerm = query,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                InStock = inStock ? true : null,
                SortBy = sortBy,
            }, displayContext.CurrencyCode, cancellationToken);
        }

        var metadata = await metadataTask;
        StorefrontIndexingPolicy.ApplySearchMetadata(metadata);

        var categories = await categoriesTask;
        var searchCategories = categories.IsSuccess && categories.Value is not null
            ? FlattenCategories(categories.Value)
            : [];

        if (isTooShort)
        {
            var context = new StorefrontSearchPageContext(normalizedQuery, category, searchCategories, [], true, 1, NormalizePageSize(pageSize), 0, 0, minPrice, maxPrice, inStock, sortBy);
            return new StorefrontCatalogPageResult<StorefrontSearchPageContext>(
                StorefrontPageResultMapper.Ready(StorefrontPageKind.Search, context, new StorefrontPageDocument("Search", "Search published products in the BlazorShop catalog.", StorefrontRoutes.Search, RobotsIndex: false)),
                metadata,
                StorefrontStructuredDataDocument.Empty,
                context);
        }

        if (productsResult!.IsServiceUnavailable)
        {
            _logger.LogWarning("Search results are temporarily unavailable.");
            metadata = await _seoComposer.ComposeServiceUnavailablePageAsync(
                "Search temporarily unavailable",
                StorefrontRoutes.Search,
                "The storefront is running, but the catalog API is not reachable right now.",
                cancellationToken);
            return new StorefrontCatalogPageResult<StorefrontSearchPageContext>(
                StorefrontPageResultMapper.ServiceUnavailable(StorefrontPageKind.Search, "Search temporarily unavailable"),
                metadata,
                StorefrontStructuredDataDocument.Empty,
                null);
        }

        var products = productsResult.Value?.Items ?? [];
        var summaries = products.Select(product => StorefrontProductSummaryMapper.ToProductSummary(product, displayContext, _priceFormatter)).ToArray();
        var totalCount = productsResult.Value?.TotalCount ?? summaries.Length;
        var totalPages = Math.Min(10, productsResult.Value?.TotalPages ?? 0);
        var contextReady = new StorefrontSearchPageContext(normalizedQuery, category, searchCategories, summaries, false, productsResult.Value?.PageNumber ?? Math.Max(1, page), productsResult.Value?.PageSize ?? NormalizePageSize(pageSize), totalCount, totalPages, minPrice, maxPrice, inStock, sortBy);
        var structuredData = StorefrontStructuredDataDocument.Empty;

        return new StorefrontCatalogPageResult<StorefrontSearchPageContext>(
            StorefrontPageResultMapper.Ready(StorefrontPageKind.Search, contextReady, new StorefrontPageDocument("Search", "Search published products in the BlazorShop catalog.", StorefrontRoutes.Search, RobotsIndex: false)),
            metadata,
            structuredData,
            contextReady);
    }

    private static IReadOnlyList<CatalogFilterCategoryOption> FlattenCategories(IReadOnlyList<GetCategoryTreeNode> categories)
    {
        var options = new List<CatalogFilterCategoryOption>();
        foreach (var category in categories)
        {
            AppendCategory(options, category, 0);
        }

        return options;
    }

    private static void AppendCategory(List<CatalogFilterCategoryOption> options, GetCategoryTreeNode category, int depth)
    {
        if (!string.IsNullOrWhiteSpace(category.Slug))
        {
            var prefix = depth <= 0 ? string.Empty : $"{new string('-', depth * 2)} ";
            options.Add(new CatalogFilterCategoryOption(category.Slug, $"{prefix}{category.Name}"));
        }

        foreach (var child in category.Children)
        {
            AppendCategory(options, child, depth + 1);
        }
    }

    private static int NormalizePageSize(int pageSize)
    {
        return CatalogSearchPolicy.StorefrontPageSizes.Contains(pageSize)
            ? pageSize
            : 24;
    }
}
