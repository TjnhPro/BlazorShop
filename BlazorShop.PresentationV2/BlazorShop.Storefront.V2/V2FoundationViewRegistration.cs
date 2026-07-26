namespace BlazorShop.Storefront;

using BlazorShop.Storefront.Components.Layout;
using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Routing;
using BlazorShop.Storefront.Presentation.Views.Foundation;
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
                ContentPage = viewSet.ContentPage,
                CartPage = viewSet.CartPage,
                CheckoutPage = viewSet.CheckoutPage,
                PaymentResultPage = viewSet.PaymentResultPage,
                AuthPage = viewSet.AuthPage,
                AccountPage = viewSet.AccountPage,
                MaintenanceState = viewSet.MaintenanceState,
                NotFoundState = viewSet.NotFoundState,
                ServiceUnavailableState = viewSet.ServiceUnavailableState,
                ErrorState = viewSet.ErrorState,
            };
        }).AddStorefrontPresentationRoutes(typeof(V2FoundationViewRegistration).Assembly);
    }
}
