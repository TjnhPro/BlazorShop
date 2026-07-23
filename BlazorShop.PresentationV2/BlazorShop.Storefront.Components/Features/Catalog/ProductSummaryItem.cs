namespace BlazorShop.Storefront.Components.Features.Catalog;

public sealed record ProductSummaryItem(
    Guid Id,
    string Name,
    string? ProductUrl,
    string? CategoryName,
    string? CategoryUrl,
    string? ImageUrl,
    string? Description,
    string PriceDisplay,
    string? ComparePriceDisplay,
    bool HasVariants,
    bool InStock,
    bool IsNewArrival,
    bool Purchasable,
    string? PurchaseUrl,
    bool CanAddDirectly = false,
    string? UnitPriceValue = null,
    string? CurrencyCode = null,
    int DirectAddStockValue = 0,
    string? PurchaseBlockMessage = null);
