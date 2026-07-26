namespace BlazorShop.Storefront;

using BlazorShop.Storefront.Components.Layout;
using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Routing;
using BlazorShop.Storefront.Presentation.Views.Foundation;

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
                MainLayout = viewSet.MainLayout,
                HomePage = viewSet.HomePage,
                CategoryPage = viewSet.CategoryPage,
                ProductPage = viewSet.ProductPage,
                SearchPage = viewSet.SearchPage,
                DealsPage = viewSet.DealsPage,
                NewReleasesPage = viewSet.NewReleasesPage,
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
        }).AddStorefrontPresentationRoutes(typeof(global::Program).Assembly);
    }
}
