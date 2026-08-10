namespace BlazorShop.Storefront.Components.Contracts.Catalog;

public sealed record StorefrontDiscountedProductRailState(
    IReadOnlyList<ProductSummaryItem> Products,
    bool IsLoading,
    string? ErrorCode,
    string? DefaultMessage,
    string? TraceId,
    bool Retryable)
{
    public static StorefrontDiscountedProductRailState Loading { get; } = new(
        Array.Empty<ProductSummaryItem>(),
        IsLoading: true,
        ErrorCode: null,
        DefaultMessage: null,
        TraceId: null,
        Retryable: false);

    public static StorefrontDiscountedProductRailState Empty { get; } = new(
        Array.Empty<ProductSummaryItem>(),
        IsLoading: false,
        ErrorCode: null,
        DefaultMessage: null,
        TraceId: null,
        Retryable: false);
}
