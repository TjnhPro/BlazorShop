namespace BlazorShop.Storefront.Starter;

using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Routing;
using BlazorShop.Storefront.Presentation.Views.Foundation;
using BlazorShop.Storefront.Starter.Components.Layout;
using BlazorShop.Storefront.Starter.Theme.Pages.Catalog;

public static class StarterFoundationViewRegistration
{
    public static IServiceCollection AddStarterFoundationViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddStorefrontFoundationViews(options =>
        {
            var viewSet = StorefrontFoundationViewSet.CreateMinimal(typeof(StorefrontFoundationEmptyView));
            options.ViewSet = new StorefrontFoundationViewSet
            {
                ApplicationHead = viewSet.ApplicationHead,
                ApplicationScripts = viewSet.ApplicationScripts,
                MainLayout = typeof(MainLayout),
                HomePage = typeof(HomePage),
                CategoryPage = typeof(CategoryPage),
                ProductPage = viewSet.ProductPage,
                SearchPage = typeof(SearchPage),
                DealsPage = typeof(DealsPage),
                NewReleasesPage = typeof(NewReleasesPage),
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
        }).AddStorefrontPresentationRoutes(typeof(Program).Assembly);
    }
}
