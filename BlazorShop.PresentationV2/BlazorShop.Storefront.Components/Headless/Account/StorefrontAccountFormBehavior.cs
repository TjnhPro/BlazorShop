namespace BlazorShop.Storefront.Components.Headless.Account;

public sealed record StorefrontAccountProfileActionDescriptor(
    string FormAction,
    string LoadProfileRoute,
    string SaveProfileRoute)
{
    public static StorefrontAccountProfileActionDescriptor Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty);
}

public sealed record StorefrontAccountPasswordActionDescriptor(
    string FormAction,
    string ChangePasswordRoute)
{
    public static StorefrontAccountPasswordActionDescriptor Empty { get; } = new(
        string.Empty,
        string.Empty);
}

public sealed record StorefrontAccountAddressActionDescriptor(
    string FormAction,
    string CurrentAddressesRoute,
    string CreateAddressRoute,
    string UpdateAddressRouteTemplate,
    string DeleteAddressRouteTemplate,
    string DefaultShippingRouteTemplate,
    string DefaultBillingRouteTemplate)
{
    public static StorefrontAccountAddressActionDescriptor Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);

    public string UpdateAddressRoute(Guid addressId) => FormatRoute(UpdateAddressRouteTemplate, addressId);

    public string DeleteAddressRoute(Guid addressId) => FormatRoute(DeleteAddressRouteTemplate, addressId);

    public string DefaultShippingRoute(Guid addressId) => FormatRoute(DefaultShippingRouteTemplate, addressId);

    public string DefaultBillingRoute(Guid addressId) => FormatRoute(DefaultBillingRouteTemplate, addressId);

    private static string FormatRoute(string routeTemplate, Guid addressId)
    {
        return routeTemplate.Replace("{addressId}", addressId.ToString("D"), StringComparison.Ordinal);
    }
}

public sealed record StorefrontAccountOrderActionDescriptor(
    string OrderListRouteTemplate,
    string OrderDetailRouteTemplate,
    string ReceiptRouteTemplate,
    string OrderDetailHrefTemplate)
{
    public static StorefrontAccountOrderActionDescriptor Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty);

    public string OrderListRoute(int pageNumber)
    {
        return OrderListRouteTemplate.Replace("{pageNumber}", Math.Max(1, pageNumber).ToString(), StringComparison.Ordinal);
    }

    public string OrderDetailRoute(string orderReference) => FormatOrderRoute(OrderDetailRouteTemplate, orderReference);

    public string ReceiptRoute(string orderReference) => FormatOrderRoute(ReceiptRouteTemplate, orderReference);

    public string OrderDetailHref(string orderReference) => FormatOrderRoute(OrderDetailHrefTemplate, orderReference);

    private static string FormatOrderRoute(string routeTemplate, string orderReference)
    {
        return routeTemplate.Replace("{orderReference}", Uri.EscapeDataString(orderReference), StringComparison.Ordinal);
    }
}
