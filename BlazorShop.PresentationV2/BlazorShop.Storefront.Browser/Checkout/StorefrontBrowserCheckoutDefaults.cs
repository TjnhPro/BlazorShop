using BlazorShop.Storefront.Components.Browser;

namespace BlazorShop.Storefront.Browser.Checkout;

public static class StorefrontBrowserCheckoutDefaults
{
    public static StorefrontBrowserCheckoutState EmptyState(string message)
    {
        return new StorefrontBrowserCheckoutState(
            false,
            message,
            null,
            0,
            0,
            "empty",
            "cart",
            false,
            false,
            false,
            string.Empty,
            [],
            [],
            [],
            []);
    }
}
