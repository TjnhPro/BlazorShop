namespace BlazorShop.Storefront.Browser.Checkout;

public sealed record StorefrontBrowserCheckoutStateMachine(
    string CurrentStep,
    bool PlaceOrderAllowed,
    bool Loading);
