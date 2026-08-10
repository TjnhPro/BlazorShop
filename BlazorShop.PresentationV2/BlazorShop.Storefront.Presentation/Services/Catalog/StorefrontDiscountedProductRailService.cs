namespace BlazorShop.Storefront.Presentation.Services.Catalog;

using System.Diagnostics;
using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Presentation.Contracts;
using BlazorShop.Storefront.Presentation.Models;
using BlazorShop.Storefront.Presentation.Services;

public sealed class StorefrontDiscountedProductRailService
{
    public const string LocalRoute = "/api/catalog/discounted-products";
    public const int DefaultLimit = 6;
    public const int MaxLimit = 24;
    private const int CandidatePageSize = 48;

    private readonly IStorefrontCatalogClient catalogClient;
    private readonly IStorefrontDisplayContextProvider displayContextProvider;
    private readonly IStorefrontPriceFormatter priceFormatter;

    public StorefrontDiscountedProductRailService(
        IStorefrontCatalogClient catalogClient,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter)
    {
        this.catalogClient = catalogClient;
        this.displayContextProvider = displayContextProvider;
        this.priceFormatter = priceFormatter;
    }

    public async Task<StorefrontDiscountedProductRailResponse> GetAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var displayContext = await this.displayContextProvider.GetAsync(cancellationToken);
        var result = await this.catalogClient.GetPublishedCatalogPageAsync(
            new ProductCatalogQuery
            {
                PageNumber = 1,
                PageSize = ResolveCandidatePageSize(limit),
                SortBy = ProductCatalogSortBy.DisplayOrder,
            },
            displayContext.CurrencyCode,
            cancellationToken);

        if (result.IsServiceUnavailable)
        {
            return Error("service_unavailable", "Catalog data is temporarily unavailable.", retryable: true);
        }

        if (!result.IsSuccess)
        {
            return Error("catalog_unavailable", "Catalog data could not be loaded.", retryable: true);
        }

        var products = result.Value?.Items ?? [];
        var discountedProducts = products
            .Select(product => StorefrontProductSummaryMapper.ToProductSummary(product, displayContext, this.priceFormatter))
            .Where(product => !string.IsNullOrWhiteSpace(product.ComparePriceDisplay))
            .Take(limit)
            .ToArray();

        return new StorefrontDiscountedProductRailResponse(discountedProducts);
    }

    public static bool TryNormalizeLimit(
        int? requestedLimit,
        out int limit,
        out StorefrontDiscountedProductRailResponse? error)
    {
        limit = requestedLimit.GetValueOrDefault(DefaultLimit);
        if (limit is < 1 or > MaxLimit)
        {
            error = Error(
                "validation_error",
                $"Limit must be between 1 and {MaxLimit}.",
                retryable: false);
            return false;
        }

        error = null;
        return true;
    }

    private static int ResolveCandidatePageSize(int limit)
    {
        return Math.Clamp(limit * 4, DefaultLimit, CandidatePageSize);
    }

    private static StorefrontDiscountedProductRailResponse Error(
        string code,
        string message,
        bool retryable)
    {
        return new StorefrontDiscountedProductRailResponse(
            Array.Empty<ProductSummaryItem>(),
            Success: false,
            Code: code,
            DefaultMessage: message,
            TraceId: Activity.Current?.TraceId.ToString(),
            Retryable: retryable);
    }
}
