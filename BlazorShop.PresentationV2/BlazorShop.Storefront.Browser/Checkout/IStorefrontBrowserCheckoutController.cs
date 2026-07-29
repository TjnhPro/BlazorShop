using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Headless.Checkout;

namespace BlazorShop.Storefront.Browser.Checkout;

public interface IStorefrontBrowserCheckoutController
{
    StorefrontBrowserCheckoutControllerState State { get; }

    void Initialize(
        StorefrontBrowserCheckoutState initialState,
        bool showPanel,
        StorefrontFeatureDataMode dataMode,
        StorefrontCheckoutActionDescriptor actions);

    Task<bool> HydrateAsync(CancellationToken cancellationToken = default);

    Task<bool> RefreshAsync(CancellationToken cancellationToken = default);

    Task<bool> SelectShippingAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> SelectPaymentAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ReviewAsync(CancellationToken cancellationToken = default);

    Task<StorefrontBrowserCheckoutPlaceOrderOutcome> PlaceOrderAsync(CancellationToken cancellationToken = default);
}
