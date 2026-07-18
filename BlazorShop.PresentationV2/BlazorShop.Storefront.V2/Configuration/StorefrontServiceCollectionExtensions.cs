namespace BlazorShop.Storefront.Configuration
{
    using System.Threading.RateLimiting;

    using BlazorShop.Application.Diagnostics;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;

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
            services.AddSingleton<ISeoMetadataBuilder, SeoMetadataBuilder>();
            services.AddScoped<IStorefrontClientAppUrlResolver, StorefrontClientAppUrlResolver>();
            services.AddScoped<IStorefrontPublicUrlResolver, StorefrontPublicUrlResolver>();
            services.AddScoped<IStorefrontRobotsService, StorefrontRobotsService>();
            services.AddScoped<IStorefrontSeoSettingsProvider, StorefrontSeoSettingsProvider>();
            services.AddScoped<IStorefrontSeoComposer, StorefrontSeoComposer>();
            services.AddScoped<IStorefrontStructuredDataComposer, StorefrontStructuredDataComposer>();
            services.AddScoped<IStorefrontSitemapService, StorefrontSitemapService>();
            services.AddScoped<IStorefrontCurrentStoreProvider, StorefrontCurrentStoreProvider>();
            services.AddScoped<IStorefrontDisplayContextProvider, StorefrontDisplayContextProvider>();
            services.AddScoped<IStorefrontPageNavigationProvider, StorefrontPageNavigationProvider>();
            services.AddScoped<IStorefrontNavigationProvider, StorefrontNavigationProvider>();
            services.AddScoped<IStorefrontPriceFormatter, StorefrontPriceFormatter>();
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

            return services;
        }
    }
}
