namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductPurchaseLabels(
    string AddToCart,
    string AddedToCart,
    string ViewCart,
    string FreeShipping,
    string Optional)
{
    public static ProductPurchaseLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
