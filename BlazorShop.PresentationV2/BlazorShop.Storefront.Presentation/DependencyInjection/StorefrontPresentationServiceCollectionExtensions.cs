namespace BlazorShop.Storefront.Presentation.DependencyInjection;

using BlazorShop.Storefront.Presentation.Views.Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class StorefrontPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddStorefrontPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }

    public static IServiceCollection AddStorefrontFoundationViews(
        this IServiceCollection services,
        Action<StorefrontFoundationViewOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<StorefrontFoundationViewOptions>()
            .Configure(configure)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorefrontFoundationViewOptions>, StorefrontFoundationViewOptionsValidator>();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<StorefrontFoundationViewOptions>>().Value.ViewSet
            ?? throw new InvalidOperationException("A StorefrontFoundationViewSet must be registered."));

        return services;
    }
}
