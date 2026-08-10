using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using BlazorShop.Storefront.Browser.Cart;
using BlazorShop.Storefront.Browser.Checkout;
using BlazorShop.Storefront.Browser.Account;
using BlazorShop.Storefront.Browser.Contact;

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
        services.AddStorefrontBrowserControllers();

        return services;
    }

    public static IServiceCollection AddStorefrontBrowserControllers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddStorefrontBrowserCart();
        services.AddStorefrontBrowserCheckout();
        services.AddStorefrontBrowserAccount();
        services.AddStorefrontBrowserContact();
        return services;
    }

    public static IServiceCollection AddStorefrontBrowserCart(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<IStorefrontBrowserCartController, StorefrontBrowserCartController>();
        return services;
    }

    public static IServiceCollection AddStorefrontBrowserCheckout(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<IStorefrontBrowserCheckoutController, StorefrontBrowserCheckoutController>();
        return services;
    }

    public static IServiceCollection AddStorefrontBrowserAccount(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<IStorefrontBrowserAccountController, StorefrontBrowserAccountController>();
        return services;
    }

    public static IServiceCollection AddStorefrontBrowserContact(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<IStorefrontBrowserContactController, StorefrontBrowserContactController>();
        return services;
    }
}
