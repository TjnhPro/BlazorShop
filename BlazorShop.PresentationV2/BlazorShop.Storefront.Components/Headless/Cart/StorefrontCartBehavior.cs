namespace BlazorShop.Storefront.Components.Headless.Cart;

using BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontCartActionDescriptor(
    string CurrentCartRoute,
    string UpdateLineRouteTemplate,
    string RemoveLineRouteTemplate,
    string ClearCartRoute)
{
    public static StorefrontCartActionDescriptor Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty);

    public string UpdateLineRoute(Guid lineId) => FormatLineRoute(UpdateLineRouteTemplate, lineId);

    public string RemoveLineRoute(Guid lineId) => FormatLineRoute(RemoveLineRouteTemplate, lineId);

    private static string FormatLineRoute(string routeTemplate, Guid lineId)
    {
        return routeTemplate.Replace("{lineId}", lineId.ToString("D"), StringComparison.Ordinal);
    }
}

public sealed record StorefrontCartViewState(
    bool Loading,
    bool Empty,
    bool HasError,
    bool CheckoutAllowed,
    int ItemCount,
    IReadOnlyList<StorefrontBrowserCartAlert> Alerts)
{
    public static StorefrontCartViewState FromCart(StorefrontBrowserCart? cart, IReadOnlyList<StorefrontBrowserCartAlert> alerts)
    {
        var lines = cart?.Lines ?? [];
        return new StorefrontCartViewState(
            cart is null,
            lines.Count == 0,
            alerts.Any(alert => string.Equals(alert.Level, "error", StringComparison.OrdinalIgnoreCase)),
            cart?.CheckoutAllowed ?? lines.All(line => !line.IsUnavailable),
            cart?.Count > 0 ? cart.Count : lines.Sum(line => line.Quantity),
            alerts);
    }
}
