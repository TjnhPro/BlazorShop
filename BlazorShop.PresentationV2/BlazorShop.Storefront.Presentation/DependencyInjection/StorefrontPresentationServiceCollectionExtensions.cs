namespace BlazorShop.Storefront.Presentation.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

public static class StorefrontPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddStorefrontPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
