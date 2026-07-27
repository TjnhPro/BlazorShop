namespace BlazorShop.Storefront;

using BlazorShop.Storefront.Components.Layout;
using BlazorShop.Storefront.Components.Shared;
using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Routing;
using BlazorShop.Storefront.Presentation.Views.Foundation;
using BlazorShop.Storefront.Pages.Hybrid.Commerce;
using BlazorShop.Storefront.Pages.Ssr.Content;
using BlazorShop.Storefront.Pages.Ssr.System;
using BlazorShop.Storefront.Theme.Pages.Auth;
using BlazorShop.Storefront.Theme.Pages.Catalog;
using BlazorShop.Storefront.Theme.Pages.Product;

public static class V2FoundationViewRegistration
{
    public static IServiceCollection AddV2FoundationViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddStorefrontFoundationViews(options =>
        {
            var viewSet = StorefrontFoundationViewSet.CreateMinimal(typeof(StorefrontFoundationEmptyView));
            options.ViewSet = new StorefrontFoundationViewSet
            {
                ApplicationHead = typeof(StorefrontApplicationHead),
                ApplicationScripts = typeof(StorefrontApplicationScripts),
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
                AccountPage = viewSet.AccountPage,
                MaintenanceState = typeof(MaintenancePage),
                NotFoundState = typeof(NotFoundPage),
                ServiceUnavailableState = typeof(ServiceUnavailableState),
                ErrorState = viewSet.ErrorState,
            };
        }).AddStorefrontPresentationRoutes(typeof(V2FoundationViewRegistration).Assembly);
    }
}
