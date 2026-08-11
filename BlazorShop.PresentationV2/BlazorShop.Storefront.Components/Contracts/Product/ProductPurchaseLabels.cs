namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductPurchaseLabels(
    string AddToCart,
    string AddedToCart,
    string ViewCart,
    string FreeShipping,
    string Optional,
    string PurchaseHeading,
    string ChooseVariant,
    string SelectVariant,
    string Quantity,
    string SelectOptionFormat)
{
    public static ProductPurchaseLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
