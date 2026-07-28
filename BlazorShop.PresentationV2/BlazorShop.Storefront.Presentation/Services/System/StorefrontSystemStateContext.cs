namespace BlazorShop.Storefront.Presentation.Services.System;

using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;

public sealed record StorefrontSystemStateContext(
    string Title,
    string Eyebrow,
    string Message,
    StorefrontCurrentStore? Store = null,
    string? Reason = null,
    bool AutoRefresh = false,
    StorefrontLinkContext? Links = null)
{
    public static StorefrontSystemStateContext NotFound(string title, string message)
    {
        return new StorefrontSystemStateContext(title, "Not Found", message);
    }

    public static StorefrontSystemStateContext ServiceUnavailable(string title, string message)
    {
        return new StorefrontSystemStateContext(title, "Service Unavailable", message);
    }
}
