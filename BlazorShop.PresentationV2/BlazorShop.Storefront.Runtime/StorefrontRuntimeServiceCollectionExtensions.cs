namespace BlazorShop.Storefront.Runtime
{
    using BlazorShop.Storefront.Client;
    using Microsoft.Extensions.DependencyInjection;
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

        public static IServiceCollection AddStorefrontGeneratedClients(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            return services.AddStorefrontServerGeneratedClients(configureHttpClient);
        }

        public static IServiceCollection AddStorefrontServerGeneratedClients(
            this IServiceCollection services,
            Action<IServiceProvider, HttpClient>? configureHttpClient = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddHttpClient(
                GeneratedClientHttpClientName,
                (serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<StorefrontRuntimeOptions>>().Value;
                    client.BaseAddress = new Uri(options.CommerceNodeBaseUrl, UriKind.Absolute);
                    configureHttpClient?.Invoke(serviceProvider, client);
                });

            services.AddScoped<IStorefrontAddressClient>(serviceProvider => new StorefrontAddressClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontAuthClient>(serviceProvider => new StorefrontAuthClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontCartClient>(serviceProvider => new StorefrontCartClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontCatalogClient>(serviceProvider => new StorefrontCatalogClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontCheckoutClient>(serviceProvider => new StorefrontCheckoutClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontConfigurationClient>(serviceProvider => new StorefrontConfigurationClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontConsentClient>(serviceProvider => new StorefrontConsentClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontContactClient>(serviceProvider => new StorefrontContactClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontCurrencyClient>(serviceProvider => new StorefrontCurrencyClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontCustomerAddressesClient>(serviceProvider => new StorefrontCustomerAddressesClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontCustomerProfileClient>(serviceProvider => new StorefrontCustomerProfileClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontNavigationClient>(serviceProvider => new StorefrontNavigationClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontNewsletterClient>(serviceProvider => new StorefrontNewsletterClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontOrdersClient>(serviceProvider => new StorefrontOrdersClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontPagesClient>(serviceProvider => new StorefrontPagesClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontPaymentsClient>(serviceProvider => new StorefrontPaymentsClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontRecommendationsClient>(serviceProvider => new StorefrontRecommendationsClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontSeoClient>(serviceProvider => new StorefrontSeoClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontStoreClient>(serviceProvider => new StorefrontStoreClient(CreateGeneratedHttpClient(serviceProvider)));
            services.AddScoped<IStorefrontRuntimeCatalogContentFacade, StorefrontRuntimeCatalogContentFacade>();
            services.AddScoped<IStorefrontRuntimeCartFacade, StorefrontRuntimeCartFacade>();
            services.AddScoped<IStorefrontRuntimeCheckoutFacade, StorefrontRuntimeCheckoutFacade>();
            services.AddScoped<IStorefrontRuntimeConfigurationFacade, StorefrontRuntimeConfigurationFacade>();
            services.AddScoped<IStorefrontRuntimeAddressFacade, StorefrontRuntimeAddressFacade>();
            services.AddScoped<IStorefrontRuntimeConsentFacade, StorefrontRuntimeConsentFacade>();
            services.AddScoped<IStorefrontRuntimePaymentFacade, StorefrontRuntimePaymentFacade>();

            return services;
        }

        private static HttpClient CreateGeneratedHttpClient(IServiceProvider serviceProvider)
        {
            return serviceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(GeneratedClientHttpClientName);
        }
    }
}
