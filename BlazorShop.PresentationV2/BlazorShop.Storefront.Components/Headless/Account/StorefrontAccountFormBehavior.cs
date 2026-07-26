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

public sealed record StorefrontAccountFormClasses
{
    public static StorefrontAccountFormClasses Empty { get; } = new();

    public string Root { get; init; } = string.Empty;

    public string StatusAlert { get; init; } = string.Empty;

    public string ErrorAlert { get; init; } = string.Empty;

    public string MissingProfile { get; init; } = string.Empty;

    public string ProfileForm { get; init; } = string.Empty;

    public string PasswordForm { get; init; } = string.Empty;

    public string Field { get; init; } = string.Empty;

    public string WideField { get; init; } = string.Empty;

    public string LabelText { get; init; } = string.Empty;

    public string Input { get; init; } = string.Empty;

    public string CurrencyInput { get; init; } = string.Empty;

    public string ActionRow { get; init; } = string.Empty;

    public string SubmitButton { get; init; } = string.Empty;
}

public sealed record StorefrontAccountAddressBookClasses
{
    public static StorefrontAccountAddressBookClasses Empty { get; } = new();

    public string Root { get; init; } = string.Empty;

    public string StatusAlert { get; init; } = string.Empty;

    public string ErrorAlert { get; init; } = string.Empty;

    public string AddSection { get; init; } = string.Empty;

    public string AddTitle { get; init; } = string.Empty;

    public string AddForm { get; init; } = string.Empty;

    public string ActionRow { get; init; } = string.Empty;

    public string PrimaryButton { get; init; } = string.Empty;

    public string EmptyState { get; init; } = string.Empty;

    public string ListGrid { get; init; } = string.Empty;

    public string Card { get; init; } = string.Empty;

    public string BadgeRow { get; init; } = string.Empty;

    public string ShippingBadge { get; init; } = string.Empty;

    public string BillingBadge { get; init; } = string.Empty;

    public string CardTitle { get; init; } = string.Empty;

    public string AddressText { get; init; } = string.Empty;

    public string AddressTextSpaced { get; init; } = string.Empty;

    public string EditForm { get; init; } = string.Empty;

    public string EditFieldsGrid { get; init; } = string.Empty;

    public string EditActions { get; init; } = string.Empty;

    public string SecondaryButton { get; init; } = string.Empty;

    public string DangerButton { get; init; } = string.Empty;

    public string CompactField { get; init; } = string.Empty;

    public string CompactWideField { get; init; } = string.Empty;

    public string FullField { get; init; } = string.Empty;

    public string FullWideField { get; init; } = string.Empty;

    public string CompactInput { get; init; } = string.Empty;

    public string FullInput { get; init; } = string.Empty;
}
