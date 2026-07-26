namespace BlazorShop.Storefront.Components.Headless.Checkout;

using BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontCheckoutActionDescriptor(
    string CurrentCheckoutRoute,
    string ShippingMethodRoute,
    string PaymentMethodRoute,
    string ReviewRoute,
    string PlaceOrderRoute)
{
    public static StorefrontCheckoutActionDescriptor Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}

public sealed record StorefrontCheckoutViewState(
    bool Loading,
    bool HasCart,
    bool HasError,
    bool PlaceOrderAllowed,
    string CurrentStep,
    int CartVersion,
    int CheckoutVersion)
{
    public static StorefrontCheckoutViewState FromState(StorefrontBrowserCheckoutState state, bool loading, string? error)
    {
        return new StorefrontCheckoutViewState(
            loading,
            state.HasCart,
            !string.IsNullOrWhiteSpace(error),
            state.PlaceOrderAllowed,
            state.CurrentStep,
            state.CartVersion,
            state.CheckoutVersion);
    }
}

// Compatibility visual schema for shared CheckoutShell only. Host storefronts should own visual class options.
public sealed record StorefrontCheckoutViewClasses
{
    public static StorefrontCheckoutViewClasses Empty { get; } = new();

    public string Shell { get; init; } = string.Empty;
    public string HeaderLayout { get; init; } = string.Empty;
    public string Eyebrow { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string BodyText { get; init; } = string.Empty;
    public string RefreshButton { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public string MetricsGrid { get; init; } = string.Empty;
    public string MetricCard { get; init; } = string.Empty;
    public string MetricValue { get; init; } = string.Empty;
    public string IssuePanel { get; init; } = string.Empty;
    public string OptionGrid { get; init; } = string.Empty;
    public string OptionPanel { get; init; } = string.Empty;
    public string OptionList { get; init; } = string.Empty;
    public string OptionLabel { get; init; } = string.Empty;
    public string PrimaryButton { get; init; } = string.Empty;
    public string SecondaryButton { get; init; } = string.Empty;
}
