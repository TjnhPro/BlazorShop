namespace BlazorShop.Storefront.Presentation.DependencyInjection;

using BlazorShop.Storefront.Presentation.Configuration;
using BlazorShop.Storefront.Presentation.Options;
using BlazorShop.Storefront.Presentation.Views.Foundation;
using BlazorShop.Storefront.Runtime;
using BlazorShop.Storefront.Presentation.Services.Account;
using BlazorShop.Storefront.Presentation.Services.Auth;
using BlazorShop.Storefront.Presentation.Services.Browser;
using BlazorShop.Storefront.Presentation.Services.Cart;
using BlazorShop.Storefront.Presentation.Services.Catalog;
using BlazorShop.Storefront.Presentation.Services.Checkout;
using BlazorShop.Storefront.Presentation.Services.Content;
using BlazorShop.Storefront.Presentation.Services.Product;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Contracts;
using BlazorShop.Storefront.Presentation.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.TryAddScoped<GeneratedStorefrontConfigurationClient>();
        services.TryAddScoped<GeneratedStorefrontConsentClient>();
        services.TryAddScoped<GeneratedStorefrontCatalogContentClient>();
        services.TryAddScoped<GeneratedStorefrontCartClient>();
        services.TryAddScoped<GeneratedStorefrontCheckoutClient>();
        services.TryAddScoped<GeneratedStorefrontAddressClient>();
        services.TryAddScoped<GeneratedStorefrontPaymentClient>();
        services.TryAddScoped<GeneratedStorefrontCustomerClient>();
        services.TryAddScoped<StorefrontAuthClient>(serviceProvider => new StorefrontAuthClient(
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(StorefrontRuntimeServiceCollectionExtensions.GeneratedClientHttpClientName),
            serviceProvider.GetRequiredService<IStorefrontRuntimeContext>()));
        services.TryAddScoped<StorefrontSessionResolver>(serviceProvider => new StorefrontSessionResolver(
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(StorefrontRuntimeServiceCollectionExtensions.GeneratedClientHttpClientName),
            serviceProvider.GetRequiredService<IHttpContextAccessor>(),
            configuration,
            serviceProvider.GetRequiredService<IStorefrontRuntimeContext>()));
        services.TryAddScoped<IStorefrontAddressClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontAddressClient>());
        services.TryAddScoped<IStorefrontAuthClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontAuthClient>());
        services.TryAddScoped<IStorefrontCartClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCartClient>());
        services.TryAddScoped<IStorefrontCatalogClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCatalogContentClient>());
        services.TryAddScoped<IStorefrontCheckoutClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCheckoutClient>());
        services.TryAddScoped<IStorefrontContentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCatalogContentClient>());
        services.TryAddScoped<IStorefrontCustomerClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCustomerClient>());
        services.TryAddScoped<IStorefrontCurrentStoreProvider, StorefrontCurrentStoreProvider>();
        services.TryAddScoped<IStorefrontDisplayContextProvider, StorefrontDisplayContextProvider>();
        services.TryAddScoped<IStorefrontShellContextService, StorefrontShellContextService>();
        services.TryAddScoped<IStorefrontPaymentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontPaymentClient>());
        services.TryAddScoped<IStorefrontPriceFormatter, StorefrontPriceFormatter>();
        services.TryAddScoped<IStorefrontSessionResolver>(serviceProvider => serviceProvider.GetRequiredService<StorefrontSessionResolver>());
        services.TryAddScoped<IStorefrontStoreConfigurationClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontConfigurationClient>());
        services.TryAddScoped<IStorefrontConsentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontConsentClient>());
        services.AddScoped<IStorefrontSeoSettingsProvider, StorefrontSeoSettingsProvider>();
        services.AddScoped<IStorefrontSeoComposer, StorefrontSeoComposer>();
        services.AddScoped<IStorefrontStructuredDataComposer, StorefrontStructuredDataComposer>();
        services.AddScoped<IStorefrontSitemapService, StorefrontSitemapService>();
        services.AddScoped<IStorefrontPagePresentationResolver, StorefrontPagePresentationResolver>();
        services.AddScoped<StorefrontBrowserActionDescriptorProvider>();
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
        services.AddScoped<StorefrontMediaProxyService>();

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
