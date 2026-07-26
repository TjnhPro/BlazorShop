namespace BlazorShop.Storefront;

using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Presentation.Views.Foundation;

public static class V2FoundationViewRegistration
{
    public static IServiceCollection AddV2FoundationViews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddStorefrontFoundationViews(options =>
        {
            options.ViewSet = StorefrontFoundationViewSet.CreateMinimal(typeof(StorefrontFoundationEmptyView));
        });
    }
}
