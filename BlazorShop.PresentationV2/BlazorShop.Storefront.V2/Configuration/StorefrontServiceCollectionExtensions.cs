namespace BlazorShop.Storefront.Configuration
{
    using System.Threading.RateLimiting;

    using BlazorShop.Application.Diagnostics;
    using BlazorShop.Application.Options;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Storefront.Services.Media;

    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using GeneratedConfigurationClient = BlazorShop.Storefront.Client.IStorefrontConfigurationClient;
    using GeneratedConfigurationClientImplementation = BlazorShop.Storefront.Client.StorefrontConfigurationClient;
    using GeneratedCurrencyClient = BlazorShop.Storefront.Client.IStorefrontCurrencyClient;
    using GeneratedCurrencyClientImplementation = BlazorShop.Storefront.Client.StorefrontCurrencyClient;
    using GeneratedCatalogClient = BlazorShop.Storefront.Client.IStorefrontCatalogClient;
    using GeneratedCatalogClientImplementation = BlazorShop.Storefront.Client.StorefrontCatalogClient;
    using GeneratedNavigationClient = BlazorShop.Storefront.Client.IStorefrontNavigationClient;
    using GeneratedNavigationClientImplementation = BlazorShop.Storefront.Client.StorefrontNavigationClient;
    using GeneratedPagesClient = BlazorShop.Storefront.Client.IStorefrontPagesClient;
    using GeneratedPagesClientImplementation = BlazorShop.Storefront.Client.StorefrontPagesClient;
    using GeneratedSeoClient = BlazorShop.Storefront.Client.IStorefrontSeoClient;
    using GeneratedSeoClientImplementation = BlazorShop.Storefront.Client.StorefrontSeoClient;
    using GeneratedStoreClient = BlazorShop.Storefront.Client.IStorefrontStoreClient;
    using GeneratedStoreClientImplementation = BlazorShop.Storefront.Client.StorefrontStoreClient;

    public static class StorefrontServiceCollectionExtensions
    {
        public static IServiceCollection AddStorefrontV2Services(
            this IServiceCollection services,
            IConfiguration configuration,
            StorefrontRateLimitingOptions rateLimitingOptions,
            Action<RateLimiterOptions, StorefrontRateLimitingOptions> configureRateLimiter,
            Action<HttpClient, IConfiguration> configureHttpClient)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(rateLimitingOptions);
            ArgumentNullException.ThrowIfNull(configureRateLimiter);
            ArgumentNullException.ThrowIfNull(configureHttpClient);

            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
            });
            services.AddSingleton<IValidateOptions<StorefrontApiOptions>, StorefrontApiOptionsValidator>();
            services.AddSingleton<IValidateOptions<ClientAppOptions>, StorefrontClientAppOptionsValidator>();
            services.AddSingleton<IValidateOptions<StorefrontPublicUrlOptions>, StorefrontPublicUrlOptionsValidator>();
            services.AddSingleton<IValidateOptions<StorefrontStoreResolutionOptions>, StorefrontStoreResolutionOptionsValidator>();
            services.ConfigureOptions<StorefrontForwardedHeadersOptionsSetup>();
            services.AddOptions<StorefrontApiOptions>()
                .Bind(configuration.GetSection(StorefrontApiOptions.SectionName))
                .ValidateOnStart();
            services.AddOptions<ClientAppOptions>()
                .Bind(configuration.GetSection(ClientAppOptions.SectionName))
                .ValidateOnStart();
            services.AddOptions<StorefrontPublicUrlOptions>()
                .Bind(configuration.GetSection(StorefrontPublicUrlOptions.SectionName))
                .ValidateOnStart();
            services.AddOptions<StorefrontStoreResolutionOptions>()
                .Bind(configuration.GetSection(StorefrontStoreResolutionOptions.SectionName))
                .ValidateOnStart();
            services.AddOptions<StorefrontRateLimitingOptions>()
                .Bind(configuration.GetSection(StorefrontRateLimitingOptions.SectionName));
            if (rateLimitingOptions.Enabled)
            {
                services.AddRateLimiter(options => configureRateLimiter(options, rateLimitingOptions));
            }

            services
                .AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();
            services.AddScoped<IStorefrontClientAppUrlResolver, StorefrontClientAppUrlResolver>();
            services.AddScoped<IStorefrontPublicUrlResolver, StorefrontPublicUrlResolver>();
            services.AddScoped<IStorefrontRobotsService, StorefrontRobotsService>();
            services.AddScoped<IStorefrontSeoSettingsProvider, StorefrontSeoSettingsProvider>();
            services.AddScoped<IStorefrontSeoComposer, StorefrontSeoComposer>();
            services.AddScoped<IStorefrontStructuredDataComposer, StorefrontStructuredDataComposer>();
            services.AddScoped<IStorefrontPagePresentationResolver, StorefrontPagePresentationResolver>();
            services.AddScoped<IStorefrontSitemapService, StorefrontSitemapService>();
            services.AddScoped<IStorefrontCurrentStoreProvider, StorefrontCurrentStoreProvider>();
            services.AddScoped<IStorefrontDisplayContextProvider, StorefrontDisplayContextProvider>();
            services.AddScoped<IStorefrontPageNavigationProvider, StorefrontPageNavigationProvider>();
            services.AddScoped<IStorefrontNavigationProvider, StorefrontNavigationProvider>();
            services.AddScoped<IStorefrontPriceFormatter, StorefrontPriceFormatter>();
            services.AddScoped<StorefrontMediaProxyService>();
            services.AddScoped<StorefrontCartTokenService>();
            services.AddHttpClient<IStorefrontSessionResolver, StorefrontSessionResolver>()
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    var serviceConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                    configureHttpClient(client, serviceConfiguration);
                });
            services.AddHttpClient<IStorefrontAuthClient, StorefrontAuthClient>()
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    var serviceConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                    configureHttpClient(client, serviceConfiguration);
                });
            services.AddHttpClient<StorefrontApiClient>()
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    var serviceConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                    configureHttpClient(client, serviceConfiguration);
                });
            services.AddHttpClient("StorefrontGenerated", (serviceProvider, client) =>
                {
                    var serviceConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
                    client.BaseAddress = StorefrontApiEndpointResolver.ResolveCommerceNodeBaseAddress(serviceConfiguration);
                    client.Timeout = TimeSpan.FromSeconds(2);
                });
            services.AddScoped<GeneratedStoreClient>(serviceProvider =>
                new GeneratedStoreClientImplementation(
                    string.Empty,
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("StorefrontGenerated")));
            services.AddScoped<GeneratedConfigurationClient>(serviceProvider =>
                new GeneratedConfigurationClientImplementation(
                    string.Empty,
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("StorefrontGenerated")));
            services.AddScoped<GeneratedCurrencyClient>(serviceProvider =>
                new GeneratedCurrencyClientImplementation(
                    string.Empty,
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("StorefrontGenerated")));
            services.AddScoped<GeneratedCatalogClient>(serviceProvider =>
                new GeneratedCatalogClientImplementation(
                    string.Empty,
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("StorefrontGenerated")));
            services.AddScoped<GeneratedPagesClient>(serviceProvider =>
                new GeneratedPagesClientImplementation(
                    string.Empty,
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("StorefrontGenerated")));
            services.AddScoped<GeneratedNavigationClient>(serviceProvider =>
                new GeneratedNavigationClientImplementation(
                    string.Empty,
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("StorefrontGenerated")));
            services.AddScoped<GeneratedSeoClient>(serviceProvider =>
                new GeneratedSeoClientImplementation(
                    string.Empty,
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("StorefrontGenerated")));
            services.AddScoped<GeneratedStorefrontConfigurationClient>();
            services.AddScoped<GeneratedStorefrontCatalogContentClient>();
            services.AddScoped<IStorefrontAddressClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>());
            services.AddScoped<IStorefrontCartClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>());
            services.AddScoped<IStorefrontCatalogClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCatalogContentClient>());
            services.AddScoped<IStorefrontCheckoutClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>());
            services.AddScoped<IStorefrontConsentClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>());
            services.AddScoped<IStorefrontContentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCatalogContentClient>());
            services.AddScoped<IStorefrontCustomerClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>());
            services.AddScoped<IStorefrontPaymentClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>());
            services.AddScoped<IStorefrontStoreConfigurationClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontConfigurationClient>());

            return services;
        }
    }
}
