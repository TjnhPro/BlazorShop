namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductPurchaseOptionItem(
    string Name,
    bool IsRequired,
    string? ControlType,
    IReadOnlyList<ProductPurchaseOptionValueItem> Values);
