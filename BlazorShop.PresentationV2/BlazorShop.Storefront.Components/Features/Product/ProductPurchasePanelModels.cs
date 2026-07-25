namespace BlazorShop.Storefront.Components.Features.Product;

public sealed record ProductPurchasePanelModel(
    Guid ProductId,
    string ProductName,
    string CurrencyCode,
    string BaseUnitPriceValue,
    Guid? ResolvedVariantId,
    string? InitialSku,
    string? InitialGtin,
    string? InitialMainImageUrl,
    int InitialStockValue,
    int MinOrderQuantity,
    int? MaxOrderQuantity,
    int QuantityStep,
    bool FreeShipping,
    string? DeliveryEstimateText,
    bool CanSubmitInitialPurchase,
    string PurchaseMessage,
    string PurchaseBlockMessage,
    IReadOnlyList<string> InitialValidationMessages,
    IReadOnlyList<ProductPurchaseOptionItem> VariationOptions,
    IReadOnlyList<ProductPurchaseVariantItem> Variants,
    string CartUrl)
{
    public static ProductPurchasePanelModel Empty { get; } = new(
        Guid.Empty,
        "Product",
        "USD",
        "0.00",
        null,
        null,
        null,
        null,
        0,
        1,
        null,
        1,
        false,
        null,
        false,
        "This product cannot be added to cart right now.",
        "Currently unavailable.",
        ["Currently unavailable."],
        [],
        [],
        "/my-cart");
}

public sealed record ProductPurchaseOptionItem(
    string Name,
    bool IsRequired,
    string? ControlType,
    IReadOnlyList<ProductPurchaseOptionValueItem> Values);

public sealed record ProductPurchaseOptionValueItem(
    string Value,
    string? ColorHex);

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
