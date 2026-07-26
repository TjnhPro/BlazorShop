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
