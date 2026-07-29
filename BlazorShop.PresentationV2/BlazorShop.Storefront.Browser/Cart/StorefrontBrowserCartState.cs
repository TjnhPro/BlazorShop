using BlazorShop.Storefront.Components.Browser;

namespace BlazorShop.Storefront.Browser.Cart;

public sealed class StorefrontBrowserCartState
{
    public StorefrontBrowserCart? Cart { get; internal set; }

    public IReadOnlyList<StorefrontBrowserCartAlert> Alerts { get; internal set; } = [];

    public Guid? BusyLineId { get; internal set; }

    public bool Clearing { get; internal set; }

    public bool ApiAvailable { get; internal set; }

    public IReadOnlyList<StorefrontBrowserCartLine> Lines => Cart?.Lines ?? [];

    public string GrandTotalDisplay => Cart?.GrandTotalDisplay ?? "$0.00";
}
