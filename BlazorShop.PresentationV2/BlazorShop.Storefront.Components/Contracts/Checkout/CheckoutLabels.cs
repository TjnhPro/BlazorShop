namespace BlazorShop.Storefront.Components.Contracts.Checkout;

public sealed record CheckoutLabels(
    string Title,
    string Refresh,
    string Refreshing,
    string ReviewLatest,
    string PlaceOrder,
    string Shipping,
    string Payment)
{
    public static CheckoutLabels Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);
}
