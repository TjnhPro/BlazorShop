namespace BlazorShop.Storefront.Starter;

using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Views.Foundation;

public static class StarterFoundationViewRegistration
{
    public static IServiceCollection AddStarterFoundationViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddStorefrontFoundationViews(options =>
        {
            options.ViewSet = StorefrontFoundationViewSet.CreateMinimal(typeof(StorefrontFoundationEmptyView));
        });
    }
}
