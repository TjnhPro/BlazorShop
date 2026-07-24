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
            ArgumentNullException.ThrowIfNull(services);

            services.AddHttpClient(
                GeneratedClientHttpClientName,
                (serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<StorefrontRuntimeOptions>>().Value;
                    client.BaseAddress = new Uri(options.CommerceNodeBaseUrl, UriKind.Absolute);
                    configureHttpClient?.Invoke(serviceProvider, client);
                });

            services.AddScoped<IStorefrontAddressClient>(CreateClient<StorefrontAddressClient>);
            services.AddScoped<IStorefrontAuthClient>(CreateClient<StorefrontAuthClient>);
            services.AddScoped<IStorefrontCartClient>(CreateClient<StorefrontCartClient>);
            services.AddScoped<IStorefrontCatalogClient>(CreateClient<StorefrontCatalogClient>);
            services.AddScoped<IStorefrontCheckoutClient>(CreateClient<StorefrontCheckoutClient>);
            services.AddScoped<IStorefrontConfigurationClient>(CreateClient<StorefrontConfigurationClient>);
            services.AddScoped<IStorefrontConsentClient>(CreateClient<StorefrontConsentClient>);
            services.AddScoped<IStorefrontContactClient>(CreateClient<StorefrontContactClient>);
            services.AddScoped<IStorefrontCurrencyClient>(CreateClient<StorefrontCurrencyClient>);
            services.AddScoped<IStorefrontCustomerAddressesClient>(CreateClient<StorefrontCustomerAddressesClient>);
            services.AddScoped<IStorefrontCustomerProfileClient>(CreateClient<StorefrontCustomerProfileClient>);
            services.AddScoped<IStorefrontNavigationClient>(CreateClient<StorefrontNavigationClient>);
            services.AddScoped<IStorefrontNewsletterClient>(CreateClient<StorefrontNewsletterClient>);
            services.AddScoped<IStorefrontOrdersClient>(CreateClient<StorefrontOrdersClient>);
            services.AddScoped<IStorefrontPagesClient>(CreateClient<StorefrontPagesClient>);
            services.AddScoped<IStorefrontPaymentsClient>(CreateClient<StorefrontPaymentsClient>);
            services.AddScoped<IStorefrontRecommendationsClient>(CreateClient<StorefrontRecommendationsClient>);
            services.AddScoped<IStorefrontSeoClient>(CreateClient<StorefrontSeoClient>);
            services.AddScoped<IStorefrontStoreClient>(CreateClient<StorefrontStoreClient>);

            return services;
        }

        private static TClient CreateClient<TClient>(IServiceProvider serviceProvider)
            where TClient : class
        {
            var httpClient = serviceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient(GeneratedClientHttpClientName);

            return (TClient)Activator.CreateInstance(typeof(TClient), string.Empty, httpClient)!;
        }
    }
}
