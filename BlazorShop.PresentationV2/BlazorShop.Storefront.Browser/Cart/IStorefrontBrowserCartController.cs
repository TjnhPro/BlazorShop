using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Headless.Cart;

namespace BlazorShop.Storefront.Browser.Cart;

public interface IStorefrontBrowserCartController
{
    StorefrontBrowserCartState State { get; }

    void Initialize(
        StorefrontBrowserCart? initialCart,
        IReadOnlyList<StorefrontBrowserCartAlert> initialAlerts,
        StorefrontFeatureDataMode dataMode,
        StorefrontCartActionDescriptor actions);

    Task<bool> HydrateAsync(CancellationToken cancellationToken = default);

    Task<bool> LoadAsync(CancellationToken cancellationToken = default);

    Task<bool> UpdateQuantityAsync(Guid lineId, object? value, CancellationToken cancellationToken = default);

    Task<bool> RemoveLineAsync(Guid lineId, CancellationToken cancellationToken = default);

    Task<bool> ClearAsync(CancellationToken cancellationToken = default);

    bool IsDisabled(Guid lineId);

    bool IsMutationBusy(Guid lineId);

    bool IsClearDisabled();
}
