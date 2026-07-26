namespace BlazorShop.Storefront.Components.Headless.Account;

public sealed record AccountNavigationItem(
    string RouteKey,
    string Label,
    string Href);

// Compatibility visual schema for shared AccountNavigation only. Host storefronts should own visual class options.
public sealed record AccountNavigationClasses
{
    public static AccountNavigationClasses Empty { get; } = new();

    public string Nav { get; init; } = string.Empty;

    public string ActiveLink { get; init; } = string.Empty;

    public string InactiveLink { get; init; } = string.Empty;
}
