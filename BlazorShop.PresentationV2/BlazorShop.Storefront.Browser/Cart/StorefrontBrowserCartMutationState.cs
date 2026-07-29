namespace BlazorShop.Storefront.Browser.Cart;

public sealed record StorefrontBrowserCartMutationState(
    Guid? BusyLineId,
    bool Clearing)
{
    public bool IsBusy => BusyLineId.HasValue || Clearing;
}
