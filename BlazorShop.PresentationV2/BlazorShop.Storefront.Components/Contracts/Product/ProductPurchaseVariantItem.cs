namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductPurchaseVariantItem(
    Guid Id,
    string DisplayName,
    string AttributeText,
    string OptionLabel,
    string? SizeValue,
    string? Sku,
    int Stock,
    bool IsDefault,
    string UnitPriceValue,
    string CurrencyCode,
    string FormattedPrice);
