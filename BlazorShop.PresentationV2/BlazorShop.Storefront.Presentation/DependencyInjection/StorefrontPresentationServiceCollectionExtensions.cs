namespace BlazorShop.Storefront.Presentation.DependencyInjection;

using System.Reflection;
using BlazorShop.Storefront.Configuration;
using BlazorShop.Storefront.Options;
using BlazorShop.Storefront.Presentation.Views.Foundation;
using BlazorShop.Storefront.Presentation.Routing;
using BlazorShop.Storefront.Presentation.Services.Account;
using BlazorShop.Storefront.Presentation.Services.Auth;
using BlazorShop.Storefront.Presentation.Services.Cart;
using BlazorShop.Storefront.Presentation.Services.Catalog;
using BlazorShop.Storefront.Presentation.Services.Checkout;
using BlazorShop.Storefront.Presentation.Services.Content;
using BlazorShop.Storefront.Presentation.Services.Product;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class StorefrontPresentationServiceCollectionExtensions
{
    public static IServiceCollection AddStorefrontPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton<IValidateOptions<StorefrontPublicUrlOptions>, StorefrontPublicUrlOptionsValidator>();
        services.AddOptions<StorefrontPublicUrlOptions>()
            .Bind(configuration.GetSection(StorefrontPublicUrlOptions.SectionName))
            .ValidateOnStart();

        services.AddScoped<IStorefrontPublicUrlResolver, StorefrontPublicUrlResolver>();
        services.AddScoped<IStorefrontSitemapReader, StorefrontRuntimeSitemapReader>();
        services.AddScoped<IStorefrontSeoSettingsReader, StorefrontRuntimeSeoSettingsReader>();
        services.AddScoped<IStorefrontRobotsService, StorefrontRobotsService>();
        services.AddScoped<IStorefrontSeoSettingsProvider, StorefrontSeoSettingsProvider>();
        services.AddScoped<IStorefrontSeoComposer, StorefrontSeoComposer>();
        services.AddScoped<IStorefrontStructuredDataComposer, StorefrontStructuredDataComposer>();
        services.AddScoped<IStorefrontSitemapService, StorefrontSitemapService>();
        services.AddScoped<IStorefrontPagePresentationResolver, StorefrontPagePresentationResolver>();
        services.AddScoped<StorefrontCartTokenService>();
        services.AddScoped<IStorefrontCartMergeService>(serviceProvider => serviceProvider.GetRequiredService<StorefrontCartTokenService>());
        services.AddScoped<StorefrontAccountPageService>();
        services.AddScoped<StorefrontAuthPageService>();
        services.AddScoped<StorefrontCartPageService>();
        services.AddScoped<StorefrontCheckoutPageService>();
        services.AddScoped<StorefrontContentPageService>();
        services.AddScoped<StorefrontCategoryPageService>();
        services.AddScoped<StorefrontDealsPageService>();
        services.AddScoped<StorefrontHomePageService>();
        services.AddScoped<StorefrontNewReleasesPageService>();
        services.AddScoped<StorefrontPaymentResultPageService>();
        services.AddScoped<StorefrontProductPageService>();
        services.AddScoped<StorefrontSearchPageService>();

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

    public static IServiceCollection AddStorefrontPresentationRoutes(
        this IServiceCollection services,
        params Assembly[] routeAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<StorefrontPresentationRouteOptions>()
            .Configure(options =>
            {
                foreach (var assembly in routeAssemblies.Where(assembly => assembly is not null))
                {
                    if (!options.AdditionalAssemblies.Contains(assembly))
                    {
                        options.AdditionalAssemblies.Add(assembly);
                    }
                }
            })
            .ValidateOnStart();

        return services;
    }
}
