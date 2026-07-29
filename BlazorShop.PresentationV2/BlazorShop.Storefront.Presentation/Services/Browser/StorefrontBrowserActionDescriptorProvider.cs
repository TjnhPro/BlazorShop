namespace BlazorShop.Storefront.Presentation.Services.Browser;

using BlazorShop.Storefront.Components.Contracts.Account;
using BlazorShop.Storefront.Components.Headless.Account;
using BlazorShop.Storefront.Components.Headless.Cart;
using BlazorShop.Storefront.Components.Headless.Checkout;
using BlazorShop.Storefront.Presentation.Services;

public sealed class StorefrontBrowserActionDescriptorProvider
{
    public StorefrontCartActionDescriptor CreateCartActions()
    {
        return new StorefrontCartActionDescriptor(
            "/api/cart",
            "/api/cart/lines/{lineId}",
            "/api/cart/lines/{lineId}",
            "/api/cart");
    }

    public StorefrontCheckoutActionDescriptor CreateCheckoutActions()
    {
        return new StorefrontCheckoutActionDescriptor(
            "/api/checkout",
            "/api/checkout/shipping-method",
            "/api/checkout/payment-method",
            "/api/checkout/review",
            "/api/checkout/place-order");
    }

    public StorefrontAccountProfileActionDescriptor CreateAccountProfileActions()
    {
        return new StorefrontAccountProfileActionDescriptor(
            StorefrontRoutes.AccountProfile,
            "/api/account/profile",
            "/api/account/profile");
    }

    public StorefrontAccountPasswordActionDescriptor CreateAccountPasswordActions()
    {
        return new StorefrontAccountPasswordActionDescriptor(
            StorefrontRoutes.AccountChangePassword,
            "/api/account/change-password");
    }

    public StorefrontAccountAddressActionDescriptor CreateAccountAddressActions()
    {
        return new StorefrontAccountAddressActionDescriptor(
            StorefrontRoutes.AccountAddresses,
            "/api/account/addresses",
            "/api/account/addresses",
            "/api/account/addresses/{addressId}",
            "/api/account/addresses/{addressId}",
            "/api/account/addresses/{addressId}/default-shipping",
            "/api/account/addresses/{addressId}/default-billing");
    }

    public StorefrontAccountOrderActionDescriptor CreateAccountOrderActions()
    {
        return new StorefrontAccountOrderActionDescriptor(
            "/api/account/orders?page={pageNumber}",
            "/api/account/orders/{orderReference}",
            "/api/account/orders/{orderReference}/receipt",
            StorefrontRoutes.AccountOrderDetail);
    }

    public IReadOnlyList<AccountNavigationItem> CreateAccountNavigationItems()
    {
        return
        [
            new("profile", "Profile", StorefrontRoutes.AccountProfile),
            new("orders", "Orders", StorefrontRoutes.AccountOrders),
            new("addresses", "Addresses", StorefrontRoutes.AccountAddresses),
            new("change-password", "Password", StorefrontRoutes.AccountChangePassword),
        ];
    }

    public AccountRouteDescriptor CreateAccountRouteDescriptor()
    {
        return new AccountRouteDescriptor(
            StorefrontRoutes.AccountProfile,
            StorefrontRoutes.AccountAddresses,
            StorefrontRoutes.AccountOrders,
            StorefrontRoutes.AccountChangePassword,
            "profile",
            "addresses",
            "orders",
            "change-password",
            "receipt");
    }
}
