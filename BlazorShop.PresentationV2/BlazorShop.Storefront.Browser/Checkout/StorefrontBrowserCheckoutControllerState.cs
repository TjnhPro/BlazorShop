using BlazorShop.Storefront.Components.Browser;

namespace BlazorShop.Storefront.Browser.Checkout;

public sealed class StorefrontBrowserCheckoutControllerState
{
    public StorefrontBrowserCheckoutState Checkout { get; internal set; } = StorefrontBrowserCheckoutDefaults.EmptyState("Checkout is not available yet.");

    public string? Error { get; internal set; }

    public bool Loading { get; internal set; }

    public bool ApiAvailable { get; internal set; }
}
