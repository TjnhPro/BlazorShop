namespace BlazorShop.Storefront.Components.Contracts.Catalog;

public sealed record StorefrontDiscountedProductRailResponse(
    IReadOnlyList<ProductSummaryItem> Products,
    bool Success = true,
    string? Code = null,
    string? DefaultMessage = null,
    string? TraceId = null,
    bool Retryable = false);
