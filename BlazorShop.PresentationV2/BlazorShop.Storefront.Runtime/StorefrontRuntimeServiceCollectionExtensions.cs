namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;

    public static class StorefrontRuntimeServiceCollectionExtensions
    {
        public const string GeneratedClientHttpClientName = "StorefrontGenerated";

        public static IServiceCollection AddStorefrontRuntime(
            this IServiceCollection services,
            Action<StorefrontRuntimeOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureOptions);

            services
                .AddOptions<StorefrontRuntimeOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<IStorefrontRuntimeContext, OptionsStorefrontRuntimeContext>();
            services.AddSingleton<IStorefrontCapabilityReader, StorefrontCapabilityReader>();

            return services;
        }

        public static IServiceCollection AddStorefrontPlatformRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddStorefrontCatalogRuntime(configureHttpClient);
            services.AddStorefrontContentRuntime(configureHttpClient);
            services.AddStorefrontNavigationRuntime(configureHttpClient);
            services.AddStorefrontSeoRuntime(configureHttpClient);
            services.AddStorefrontCartRuntime(configureHttpClient);
            services.AddStorefrontCheckoutRuntime(configureHttpClient);
            services.AddStorefrontAccountRuntime(configureHttpClient);
            services.AddStorefrontConfigurationRuntime(configureHttpClient);
            services.AddStorefrontPaymentRuntime(configureHttpClient);
            services.AddStorefrontConsentRuntime(configureHttpClient);
            services.AddStorefrontAddressRuntime(configureHttpClient);
            services.AddStorefrontContactRuntime(configureHttpClient);
            services.AddStorefrontNewsletterRuntime(configureHttpClient);
            services.AddStorefrontRecommendationsRuntime(configureHttpClient);

            return services;
        }

        public static IServiceCollection AddStorefrontCatalogRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddCatalogContentRuntimeCore(services, configureHttpClient);
            services.TryAddScoped<IStorefrontRuntimeCatalogFacade, StorefrontRuntimeCatalogFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontContentRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddCatalogContentRuntimeCore(services, configureHttpClient);
            services.TryAddScoped<IStorefrontRuntimeContentFacade, StorefrontRuntimeContentFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontNavigationRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddCatalogContentRuntimeCore(services, configureHttpClient);
            services.TryAddScoped<IStorefrontRuntimeNavigationFacade, StorefrontRuntimeNavigationFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontSeoRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddCatalogContentRuntimeCore(services, configureHttpClient);
            services.TryAddScoped<IStorefrontRuntimeSeoFacade, StorefrontRuntimeSeoFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontCartRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontCartClient>(serviceProvider => new StorefrontCartClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontRuntimeCartFacade, StorefrontRuntimeCartFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontCheckoutRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontCheckoutClient>(serviceProvider => new StorefrontCheckoutClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontRuntimeCheckoutFacade, StorefrontRuntimeCheckoutFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontAccountRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontAuthClient>(serviceProvider => new StorefrontAuthClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontCustomerAddressesClient>(serviceProvider => new StorefrontCustomerAddressesClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontCustomerProfileClient>(serviceProvider => new StorefrontCustomerProfileClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontOrdersClient>(serviceProvider => new StorefrontOrdersClient(CreateGeneratedHttpClient(serviceProvider)));
            return services;
        }

        public static IServiceCollection AddStorefrontConfigurationRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontConfigurationClient>(serviceProvider => new StorefrontConfigurationClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontCurrencyClient>(serviceProvider => new StorefrontCurrencyClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontStoreClient>(serviceProvider => new StorefrontStoreClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontRuntimeConfigurationFacade, StorefrontRuntimeConfigurationFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontPaymentRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontPaymentsClient>(serviceProvider => new StorefrontPaymentsClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontRuntimePaymentFacade, StorefrontRuntimePaymentFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontConsentRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontConsentClient>(serviceProvider => new StorefrontConsentClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontRuntimeConsentFacade, StorefrontRuntimeConsentFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontAddressRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontAddressClient>(serviceProvider => new StorefrontAddressClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontRuntimeAddressFacade, StorefrontRuntimeAddressFacade>();
            return services;
        }

        public static IServiceCollection AddStorefrontContactRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontContactClient>(serviceProvider => new StorefrontContactClient(CreateGeneratedHttpClient(serviceProvider)));
            return services;
        }

        public static IServiceCollection AddStorefrontNewsletterRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontNewsletterClient>(serviceProvider => new StorefrontNewsletterClient(CreateGeneratedHttpClient(serviceProvider)));
            return services;
        }

        public static IServiceCollection AddStorefrontRecommendationsRuntime(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontRecommendationsClient>(serviceProvider => new StorefrontRecommendationsClient(CreateGeneratedHttpClient(serviceProvider)));
            return services;
        }

        private static void AddCatalogContentRuntimeCore(
            IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient)
        {
            AddGeneratedClientHttpClient(services, configureHttpClient);
            services.TryAddScoped<IStorefrontCatalogClient>(serviceProvider => new StorefrontCatalogClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontNavigationClient>(serviceProvider => new StorefrontNavigationClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontPagesClient>(serviceProvider => new StorefrontPagesClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<IStorefrontSeoClient>(serviceProvider => new StorefrontSeoClient(CreateGeneratedHttpClient(serviceProvider)));
            services.TryAddScoped<StorefrontRuntimeCatalogContentFacade>();
            services.TryAddScoped<IStorefrontRuntimeCatalogContentFacade>(serviceProvider => serviceProvider.GetRequiredService<StorefrontRuntimeCatalogContentFacade>());
        }

        private static void AddGeneratedClientHttpClient(
            IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient)
        {
            services.AddHttpClient(
                GeneratedClientHttpClientName,
                (serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<StorefrontRuntimeOptions>>().Value;
                    client.BaseAddress = new Uri(options.CommerceNodeBaseUrl, UriKind.Absolute);
                    configureHttpClient?.Invoke(serviceProvider, client);
                });
        }

        private static HttpClient CreateGeneratedHttpClient(IServiceProvider serviceProvider)
        {
            return serviceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(GeneratedClientHttpClientName);
        }
    }
}
