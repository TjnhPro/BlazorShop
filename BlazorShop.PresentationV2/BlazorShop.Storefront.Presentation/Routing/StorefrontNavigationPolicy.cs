namespace BlazorShop.Storefront.Presentation.Routing;

public static class StorefrontNavigationPolicy
{
    public static bool IsSearchNoIndexPath(string path)
    {
        return string.Equals(path, StorefrontRoutePatterns.Search, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPrivateNoIndexPath(string path)
    {
        return path.StartsWith("/cart", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/checkout", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/payment-", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/signin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/register", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/forgot-password", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/reset-password", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/maintenance", StringComparison.OrdinalIgnoreCase);
    }
}
