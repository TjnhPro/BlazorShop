using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using BlazorShop.Storefront.Browser.Cart;

namespace BlazorShop.Storefront.Browser;

public static class StorefrontBrowserServiceCollectionExtensions
{
    public static IServiceCollection AddStorefrontBrowserRuntime(
        this IServiceCollection services,
        IWebAssemblyHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri(hostEnvironment.BaseAddress),
        });
        services.AddScoped<IStorefrontAntiforgeryTokenReader, StorefrontAntiforgeryTokenReader>();
        services.AddScoped<StorefrontLocalApiClient>();
        services.AddScoped<IStorefrontBrowserCartEventPublisher, StorefrontBrowserCartEventPublisher>();
        services.AddStorefrontBrowserCart();
        services.AddStorefrontBrowserCheckout();
        services.AddStorefrontBrowserAccount();

        return services;
    }

    public static IServiceCollection AddStorefrontBrowserCart(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IStorefrontBrowserCartController, StorefrontBrowserCartController>();
        return services;
    }

    public static IServiceCollection AddStorefrontBrowserCheckout(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }

    public static IServiceCollection AddStorefrontBrowserAccount(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
