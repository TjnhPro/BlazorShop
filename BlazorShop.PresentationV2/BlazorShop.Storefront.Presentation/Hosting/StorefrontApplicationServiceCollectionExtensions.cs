namespace BlazorShop.Storefront.Presentation.Hosting;

using BlazorShop.Storefront.Presentation.Configuration;
using BlazorShop.Storefront.Presentation.Options;
using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Runtime;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class StorefrontApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddStorefrontApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddStorefrontApplicationOptions(configuration);
        services.AddStorefrontRuntime(options =>
        {
            options.CommerceNodeBaseUrl = StorefrontApiEndpointResolver.ResolveCommerceNodeBaseAddress(configuration).ToString();
            options.StoreKey = StorefrontApiEndpointResolver.ResolveStoreKey(configuration) ?? "default";
            options.PublicBaseUrl = StorefrontApiEndpointResolver.ResolvePublicBaseUrl(configuration);
        });
        services.AddStorefrontPlatformRuntime();
        services.AddStorefrontPresentation(configuration);
        services.AddStorefrontApplicationSecurity(configuration);
        services.AddStorefrontApplicationShell();

        return services;
    }

    private static IServiceCollection AddStorefrontApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddSingleton<IValidateOptions<StorefrontApiOptions>, StorefrontApiOptionsValidator>();
        services.AddSingleton<IValidateOptions<ClientAppOptions>, StorefrontClientAppOptionsValidator>();
        services.AddSingleton<IValidateOptions<StorefrontStoreResolutionOptions>, StorefrontStoreResolutionOptionsValidator>();
        services.AddSingleton<IValidateOptions<StorefrontRuntimeBindingOptions>, StorefrontRuntimeBindingOptionsValidator>();
        services.ConfigureOptions<StorefrontForwardedHeadersOptionsSetup>();
        services.AddOptions<StorefrontApplicationOptions>()
            .Bind(configuration.GetSection(StorefrontApplicationOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<StorefrontApiOptions>()
            .Bind(configuration.GetSection(StorefrontApiOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<ClientAppOptions>()
            .Bind(configuration.GetSection(ClientAppOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<StorefrontStoreResolutionOptions>()
            .Bind(configuration.GetSection(StorefrontStoreResolutionOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<StorefrontRuntimeBindingOptions>()
            .Bind(configuration.GetSection(StorefrontRuntimeBindingOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<StorefrontRateLimitingOptions>()
            .Bind(configuration.GetSection(StorefrontRateLimitingOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddStorefrontApplicationSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
        });
        var rateLimitingOptions = configuration
            .GetSection(StorefrontRateLimitingOptions.SectionName)
            .Get<StorefrontRateLimitingOptions>() ?? new StorefrontRateLimitingOptions();
        services.AddRateLimiter(options => StorefrontRateLimitPolicies.ConfigureStorefrontRateLimiter(options, rateLimitingOptions));

        return services;
    }

    private static IServiceCollection AddStorefrontApplicationShell(this IServiceCollection services)
    {
        services
            .AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();
        services.AddScoped<IStorefrontClientAppUrlResolver, StorefrontClientAppUrlResolver>();
        services.AddScoped<IStorefrontPageNavigationProvider, StorefrontPageNavigationProvider>();
        services.AddScoped<IStorefrontNavigationProvider, StorefrontNavigationProvider>();

        return services;
    }
}
