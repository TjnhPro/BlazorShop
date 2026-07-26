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

// Compatibility visual schema for shared CartView only. Host storefronts should own visual class options.
public sealed record StorefrontCartViewClasses
{
    public static StorefrontCartViewClasses Empty { get; } = new();

    public string PageSection { get; init; } = string.Empty;

    public string Layout { get; init; } = string.Empty;

    public string ContentColumn { get; init; } = string.Empty;

    public string HeaderCard { get; init; } = string.Empty;

    public string HeaderLayout { get; init; } = string.Empty;

    public string Eyebrow { get; init; } = string.Empty;

    public string PageTitle { get; init; } = string.Empty;

    public string BodyText { get; init; } = string.Empty;

    public string CountBadge { get; init; } = string.Empty;

    public string Alert { get; init; } = string.Empty;

    public string ErrorAlert { get; init; } = string.Empty;

    public string WarningAlert { get; init; } = string.Empty;

    public string EmptyState { get; init; } = string.Empty;

    public string EmptyTitle { get; init; } = string.Empty;

    public string EmptyActions { get; init; } = string.Empty;

    public string PrimaryLink { get; init; } = string.Empty;

    public string SecondaryLink { get; init; } = string.Empty;

    public string LineList { get; init; } = string.Empty;

    public string LineCard { get; init; } = string.Empty;

    public string LineLayout { get; init; } = string.Empty;

    public string LineImageFrame { get; init; } = string.Empty;

    public string LineImage { get; init; } = string.Empty;

    public string LineTitle { get; init; } = string.Empty;

    public string LineMeta { get; init; } = string.Empty;

    public string LineWarning { get; init; } = string.Empty;

    public string LineControls { get; init; } = string.Empty;

    public string LineMetrics { get; init; } = string.Empty;

    public string MetricLabel { get; init; } = string.Empty;

    public string MetricValue { get; init; } = string.Empty;

    public string QuantityInput { get; init; } = string.Empty;

    public string RemoveButton { get; init; } = string.Empty;

    public string SummaryAside { get; init; } = string.Empty;

    public string SummaryCard { get; init; } = string.Empty;

    public string SummaryRows { get; init; } = string.Empty;

    public string SummaryRow { get; init; } = string.Empty;

    public string CheckoutButton { get; init; } = string.Empty;

    public string DisabledCheckoutButton { get; init; } = string.Empty;

    public string ClearButton { get; init; } = string.Empty;
}
