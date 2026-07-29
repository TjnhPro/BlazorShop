namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Presentation.Services.Browser;

using Xunit;

public sealed class StorefrontBrowserActionDescriptorProviderTests
{
    [Fact]
    public void CartActions_UsePresentationOwnedSameOriginRoutes()
    {
        var actions = new StorefrontBrowserActionDescriptorProvider().CreateCartActions();

        Assert.Equal("/api/cart", actions.CurrentCartRoute);
        Assert.Equal("/api/cart/lines/{lineId}", actions.UpdateLineRouteTemplate);
        Assert.Equal("/api/cart/lines/{lineId}", actions.RemoveLineRouteTemplate);
        Assert.Equal("/api/cart", actions.ClearCartRoute);
    }

    [Fact]
    public void CheckoutActions_UsePresentationOwnedSameOriginRoutes()
    {
        var actions = new StorefrontBrowserActionDescriptorProvider().CreateCheckoutActions();

        Assert.Equal("/api/checkout", actions.CurrentCheckoutRoute);
        Assert.Equal("/api/checkout/shipping-method", actions.ShippingMethodRoute);
        Assert.Equal("/api/checkout/payment-method", actions.PaymentMethodRoute);
        Assert.Equal("/api/checkout/review", actions.ReviewRoute);
        Assert.Equal("/api/checkout/place-order", actions.PlaceOrderRoute);
    }

    [Fact]
    public void AccountDescriptors_UsePresentationOwnedRoutesAndNavigation()
    {
        var provider = new StorefrontBrowserActionDescriptorProvider();

        var profileActions = provider.CreateAccountProfileActions();
        var passwordActions = provider.CreateAccountPasswordActions();
        var addressActions = provider.CreateAccountAddressActions();
        var orderActions = provider.CreateAccountOrderActions();
        var routeDescriptor = provider.CreateAccountRouteDescriptor();
        var navigationItems = provider.CreateAccountNavigationItems();

        Assert.Equal("/account/profile", profileActions.FormAction);
        Assert.Equal("/api/account/profile", profileActions.LoadProfileRoute);
        Assert.Equal("/api/account/profile", profileActions.SaveProfileRoute);
        Assert.Equal("/account/change-password", passwordActions.FormAction);
        Assert.Equal("/api/account/change-password", passwordActions.ChangePasswordRoute);
        Assert.Equal("/account/addresses", addressActions.FormAction);
        Assert.Equal("/api/account/addresses", addressActions.CurrentAddressesRoute);
        Assert.Equal("/api/account/addresses", addressActions.CreateAddressRoute);
        Assert.Equal("/api/account/addresses/{addressId}", addressActions.UpdateAddressRouteTemplate);
        Assert.Equal("/api/account/addresses/{addressId}", addressActions.DeleteAddressRouteTemplate);
        Assert.Equal("/api/account/addresses/{addressId}/default-shipping", addressActions.DefaultShippingRouteTemplate);
        Assert.Equal("/api/account/addresses/{addressId}/default-billing", addressActions.DefaultBillingRouteTemplate);
        Assert.Equal("/api/account/orders?page={pageNumber}", orderActions.OrderListRouteTemplate);
        Assert.Equal("/api/account/orders/{orderReference}", orderActions.OrderDetailRouteTemplate);
        Assert.Equal("/api/account/orders/{orderReference}/receipt", orderActions.ReceiptRouteTemplate);
        Assert.Equal("/account/orders/{orderReference}", orderActions.OrderDetailHrefTemplate);
        Assert.Equal("/account/profile", routeDescriptor.ProfileRoute);
        Assert.Equal("/account/addresses", routeDescriptor.AddressesRoute);
        Assert.Equal("/account/orders", routeDescriptor.OrdersRoute);
        Assert.Equal("/account/change-password", routeDescriptor.ChangePasswordRoute);
        Assert.Collection(
            navigationItems,
            item => Assert.Equal(("profile", "Profile", "/account/profile"), (item.RouteKey, item.Label, item.Href)),
            item => Assert.Equal(("orders", "Orders", "/account/orders"), (item.RouteKey, item.Label, item.Href)),
            item => Assert.Equal(("addresses", "Addresses", "/account/addresses"), (item.RouteKey, item.Label, item.Href)),
            item => Assert.Equal(("change-password", "Password", "/account/change-password"), (item.RouteKey, item.Label, item.Href)));
    }

    [Fact]
    public void PresentationPageServices_AssignDescriptorsToPageContexts()
    {
        var cartContext = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Cart/StorefrontCartPageContext.cs");
        var cartService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Cart/StorefrontCartPageService.cs");
        var checkoutContext = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontCheckoutPageContext.cs");
        var checkoutService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontCheckoutPageService.cs");
        var accountContext = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Account/StorefrontAccountPageContext.cs");
        var accountService = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Account/StorefrontAccountPageService.cs");

        Assert.Contains("StorefrontCartActionDescriptor CartActions", cartContext, StringComparison.Ordinal);
        Assert.Contains("CartActions = actionDescriptorProvider.CreateCartActions()", cartService, StringComparison.Ordinal);
        Assert.Contains("StorefrontCheckoutActionDescriptor CheckoutActions", checkoutContext, StringComparison.Ordinal);
        Assert.Contains("CheckoutActions = actionDescriptorProvider.CreateCheckoutActions()", checkoutService, StringComparison.Ordinal);
        Assert.Contains("StorefrontAccountProfileActionDescriptor ProfileActions", accountContext, StringComparison.Ordinal);
        Assert.Contains("StorefrontAccountPasswordActionDescriptor PasswordActions", accountContext, StringComparison.Ordinal);
        Assert.Contains("StorefrontAccountAddressActionDescriptor AddressActions", accountContext, StringComparison.Ordinal);
        Assert.Contains("StorefrontAccountOrderActionDescriptor OrderActions", accountContext, StringComparison.Ordinal);
        Assert.Contains("AccountRouteDescriptor RouteDescriptor", accountContext, StringComparison.Ordinal);
        Assert.Contains("AccountNavigationItem[] NavigationItems", accountContext, StringComparison.Ordinal);
        Assert.Contains("ProfileActions = this.actionDescriptorProvider.CreateAccountProfileActions()", accountService, StringComparison.Ordinal);
        Assert.Contains("PasswordActions = this.actionDescriptorProvider.CreateAccountPasswordActions()", accountService, StringComparison.Ordinal);
        Assert.Contains("AddressActions = this.actionDescriptorProvider.CreateAccountAddressActions()", accountService, StringComparison.Ordinal);
        Assert.Contains("OrderActions = this.actionDescriptorProvider.CreateAccountOrderActions()", accountService, StringComparison.Ordinal);
        Assert.Contains("RouteDescriptor = this.actionDescriptorProvider.CreateAccountRouteDescriptor()", accountService, StringComparison.Ordinal);
        Assert.Contains("NavigationItems = this.actionDescriptorProvider.CreateAccountNavigationItems()", accountService, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
