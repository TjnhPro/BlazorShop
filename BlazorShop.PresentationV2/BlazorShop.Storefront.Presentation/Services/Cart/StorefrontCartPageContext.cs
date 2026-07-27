namespace BlazorShop.Storefront.Presentation.Services.Cart;

using BlazorShop.Storefront.Components.Browser;

public sealed record StorefrontCartPageContext(
    StorefrontBrowserCart? Cart,
    IReadOnlyList<StorefrontBrowserCartAlert> Alerts,
    string CheckoutUrl,
    string NewReleasesUrl,
    string TodaysDealsUrl);
