namespace BlazorShop.Storefront.Starter;

using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Routing;
using BlazorShop.Storefront.Presentation.Views.Foundation;
using BlazorShop.Storefront.Starter.Components.Layout;
using BlazorShop.Storefront.Starter.Theme.Pages.Auth;
using BlazorShop.Storefront.Starter.Theme.Pages.Catalog;
using BlazorShop.Storefront.Starter.Theme.Pages.Content;
using BlazorShop.Storefront.Starter.Theme.Pages.System;

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
                ContentPage = typeof(ContentPage),
                CartPage = viewSet.CartPage,
                CheckoutPage = viewSet.CheckoutPage,
                PaymentResultPage = viewSet.PaymentResultPage,
                AuthPage = typeof(AuthShellPage),
                AccountPage = viewSet.AccountPage,
                MaintenanceState = typeof(MaintenancePage),
                NotFoundState = typeof(NotFoundPage),
                ServiceUnavailableState = typeof(NotFoundPage),
                ErrorState = viewSet.ErrorState,
            };
        }).AddStorefrontPresentationRoutes(typeof(Program).Assembly);
    }
}
