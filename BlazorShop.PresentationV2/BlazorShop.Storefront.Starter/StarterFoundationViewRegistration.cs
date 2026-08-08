namespace BlazorShop.Storefront.Starter;

using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Views.Foundation;
using BlazorShop.Storefront.Starter.Components.Layout;
using BlazorShop.Storefront.Starter.Components.States;
using BlazorShop.Storefront.Starter.Pages.Hybrid.Commerce;
using BlazorShop.Storefront.Starter.Pages.WasmHost.Account;
using BlazorShop.Storefront.Starter.Pages.Auth;
using BlazorShop.Storefront.Starter.Pages.Catalog;
using BlazorShop.Storefront.Starter.Pages.Content;
using BlazorShop.Storefront.Starter.Pages.System;

public static class StarterFoundationViewRegistration
{
    public static IServiceCollection AddStarterFoundationViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddStorefrontFoundationViews(options =>
        {
            options.ViewSet = new StorefrontFoundationViewSet
            {
                ApplicationHead = typeof(ApplicationHead),
                VisualScripts = typeof(ApplicationScripts),
                MainLayout = typeof(MainLayout),
                ConsentBanner = typeof(StarterConsentBanner),
                HomePage = typeof(HomePage),
                CategoryPage = typeof(CategoryPage),
                ProductPage = typeof(ProductPage),
                SearchPage = typeof(SearchPage),
                ContentPage = typeof(ContentPage),
                CartPage = typeof(CartPage),
                CheckoutPage = typeof(CheckoutPage),
                PaymentResultPage = typeof(PaymentResultPage),
                AuthPage = typeof(AuthShellPage),
                AccountPage = typeof(AccountHostPage),
                MaintenanceState = typeof(MaintenancePage),
                NotFoundState = typeof(NotFoundPage),
                ServiceUnavailableState = typeof(MaintenancePage),
                ErrorState = typeof(ErrorState),
            };
        });
    }
}
