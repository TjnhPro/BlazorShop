namespace BlazorShop.Storefront.Configuration
{
    using System.Threading.RateLimiting;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Presentation.DependencyInjection;
    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Storefront.Services.Media;

    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

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

            services.AddStorefrontHostOptions(configuration);
            services.AddStorefrontRuntimeRegistration(configuration);
            services.AddStorefrontPresentation(configuration);
            services.AddStorefrontAuthSessionAndAntiforgeryPolicies(
                rateLimitingOptions,
                configureRateLimiter,
                configureHttpClient);
            services.AddStorefrontBffEndpointDependencies();
            services.AddStorefrontSeoMediaAndDeploymentServices();
            services.AddStorefrontGeneratedClientRegistration();

            return services;
        }

        private static IServiceCollection AddStorefrontHostOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddSingleton<IValidateOptions<StorefrontApiOptions>, StorefrontApiOptionsValidator>();
            services.AddSingleton<IValidateOptions<ClientAppOptions>, StorefrontClientAppOptionsValidator>();
            services.AddSingleton<IValidateOptions<StorefrontStoreResolutionOptions>, StorefrontStoreResolutionOptionsValidator>();
            services.ConfigureOptions<StorefrontForwardedHeadersOptionsSetup>();
            services.AddOptions<StorefrontApiOptions>()
                .Bind(configuration.GetSection(StorefrontApiOptions.SectionName))
                .ValidateOnStart();
            services.AddOptions<ClientAppOptions>()
                .Bind(configuration.GetSection(ClientAppOptions.SectionName))
                .ValidateOnStart();
            services.AddOptions<StorefrontStoreResolutionOptions>()
                .Bind(configuration.GetSection(StorefrontStoreResolutionOptions.SectionName))
                .ValidateOnStart();
            services.AddOptions<StorefrontRateLimitingOptions>()
                .Bind(configuration.GetSection(StorefrontRateLimitingOptions.SectionName));

            return services;
        }

        private static IServiceCollection AddStorefrontRuntimeRegistration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddStorefrontRuntime(options =>
            {
                options.CommerceNodeBaseUrl = StorefrontApiEndpointResolver.ResolveCommerceNodeBaseAddress(configuration).ToString();
                options.StoreKey = StorefrontApiEndpointResolver.ResolveStoreKey(configuration) ?? "default";
            });

            return services;
        }

        private static IServiceCollection AddStorefrontAuthSessionAndAntiforgeryPolicies(
            this IServiceCollection services,
            StorefrontRateLimitingOptions rateLimitingOptions,
            Action<RateLimiterOptions, StorefrontRateLimitingOptions> configureRateLimiter,
            Action<HttpClient, IConfiguration> configureHttpClient)
        {
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
            });
            if (rateLimitingOptions.Enabled)
            {
                services.AddRateLimiter(options => configureRateLimiter(options, rateLimitingOptions));
            }

            services.AddHttpClient<IStorefrontSessionResolver, StorefrontSessionResolver>()
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    configureHttpClient(client, serviceProvider.GetRequiredService<IConfiguration>());
                });
            services.AddHttpClient<IStorefrontAuthClient, StorefrontAuthClient>()
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    configureHttpClient(client, serviceProvider.GetRequiredService<IConfiguration>());
                });
            services.AddHttpClient<StorefrontApiClient>()
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    configureHttpClient(client, serviceProvider.GetRequiredService<IConfiguration>());
                });

            return services;
        }

        private static IServiceCollection AddStorefrontBffEndpointDependencies(this IServiceCollection services)
        {
            services
                .AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();
            services.AddScoped<IStorefrontClientAppUrlResolver, StorefrontClientAppUrlResolver>();
            services.AddScoped<IStorefrontCurrentStoreProvider, StorefrontCurrentStoreProvider>();
            services.AddScoped<IStorefrontDisplayContextProvider, StorefrontDisplayContextProvider>();
            services.AddScoped<IStorefrontPageNavigationProvider, StorefrontPageNavigationProvider>();
            services.AddScoped<IStorefrontNavigationProvider, StorefrontNavigationProvider>();
            services.AddScoped<IStorefrontPriceFormatter, StorefrontPriceFormatter>();
            services.AddScoped<StorefrontCartTokenService>();
            services.AddScoped<IStorefrontCartMergeService>(serviceProvider => serviceProvider.GetRequiredService<StorefrontCartTokenService>());

            return services;
        }

        private static IServiceCollection AddStorefrontSeoMediaAndDeploymentServices(this IServiceCollection services)
        {
            services.AddScoped<StorefrontMediaProxyService>();

            return services;
        }

        private static IServiceCollection AddStorefrontGeneratedClientRegistration(this IServiceCollection services)
        {
            services.AddStorefrontPlatformRuntime((_, client) =>
            {
                client.Timeout = TimeSpan.FromSeconds(2);
            });
            services.AddScoped<GeneratedStorefrontConfigurationClient>();
            services.AddScoped<GeneratedStorefrontCatalogContentClient>();
            services.AddScoped<GeneratedStorefrontCartClient>();
            services.AddScoped<GeneratedStorefrontCheckoutClient>();
            services.AddScoped<GeneratedStorefrontAddressClient>();
            services.AddScoped<GeneratedStorefrontConsentClient>();
            services.AddScoped<GeneratedStorefrontPaymentClient>();
            services.AddScoped<IStorefrontAddressClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontAddressClient>());
            services.AddScoped<IStorefrontCartClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCartClient>());
            services.AddScoped<IStorefrontCatalogClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCatalogContentClient>());
            services.AddScoped<IStorefrontCheckoutClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCheckoutClient>());
            services.AddScoped<IStorefrontConsentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontConsentClient>());
            services.AddScoped<IStorefrontContentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontCatalogContentClient>());
            services.AddScoped<IStorefrontCustomerClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>());
            services.AddScoped<IStorefrontPaymentClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontPaymentClient>());
            services.AddScoped<IStorefrontStoreConfigurationClient>(serviceProvider => serviceProvider.GetRequiredService<GeneratedStorefrontConfigurationClient>());

            return services;
        }
    }
}
