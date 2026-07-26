namespace BlazorShop.Storefront.Components.Contracts.Cart;

public sealed record CartLabels(
    string PageTitle,
    string EmptyTitle,
    string UnitPrice,
    string Quantity,
    string LineTotal,
    string ViewProduct,
    string Checkout,
    string ClearCart)
{
    public static CartLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
