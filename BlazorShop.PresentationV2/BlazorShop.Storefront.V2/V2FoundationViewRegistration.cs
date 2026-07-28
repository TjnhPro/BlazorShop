namespace BlazorShop.Storefront.V2;

using BlazorShop.Storefront.V2.Components.Layout;
using BlazorShop.Storefront.V2.Components.Shared;
using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Views.Foundation;
using BlazorShop.Storefront.V2.Pages.Auth;
using BlazorShop.Storefront.V2.Pages.Catalog;
using BlazorShop.Storefront.V2.Pages.Content;
using BlazorShop.Storefront.V2.Pages.Hybrid.Commerce;
using BlazorShop.Storefront.V2.Pages.Product;
using BlazorShop.Storefront.V2.Pages.System;
using BlazorShop.Storefront.V2.Pages.WasmHost.Account;

public static class V2FoundationViewRegistration
{
    public static IServiceCollection AddV2FoundationViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddStorefrontFoundationViews(options =>
        {
            options.ViewSet = new StorefrontFoundationViewSet
            {
                ApplicationHead = typeof(StorefrontApplicationHead),
                VisualScripts = typeof(StorefrontApplicationScripts),
                MainLayout = typeof(MainLayout),
                HomePage = typeof(Home),
                CategoryPage = typeof(CategoryPage),
                ProductPage = typeof(V2ProductPageView),
                SearchPage = typeof(SearchPage),
                DealsPage = typeof(TodaysDeals),
                NewReleasesPage = typeof(NewReleases),
                ContentPage = typeof(StorefrontPage),
                CartPage = typeof(CartPage),
                CheckoutPage = typeof(CheckoutPage),
                PaymentResultPage = typeof(PaymentResultPage),
                AuthPage = typeof(V2AuthPageView),
                AccountPage = typeof(AccountHostPage),
                MaintenanceState = typeof(MaintenancePage),
                NotFoundState = typeof(NotFoundPage),
                ServiceUnavailableState = typeof(ServiceUnavailableState),
                ErrorState = typeof(ErrorState),
            };
        });
    }
}
