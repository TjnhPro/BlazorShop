namespace BlazorShop.Storefront.Browser.Checkout;

public sealed record StorefrontBrowserCheckoutPlaceOrderOutcome(
    bool Changed,
    string? RedirectUrl);
