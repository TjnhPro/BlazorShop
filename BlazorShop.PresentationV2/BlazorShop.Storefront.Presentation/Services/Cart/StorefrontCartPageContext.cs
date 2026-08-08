namespace BlazorShop.Storefront.Presentation.Services.Cart;

using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Headless.Cart;
using BlazorShop.Storefront.Presentation.Contracts;

public sealed record StorefrontCartPageContext(
    StorefrontBrowserCart? Cart,
    IReadOnlyList<StorefrontBrowserCartAlert> Alerts,
    string CheckoutUrl,
    string ContinueShoppingUrl,
    StorefrontLinkContext Links)
{
    public StorefrontCartActionDescriptor CartActions { get; init; } = StorefrontCartActionDescriptor.Empty;
}
