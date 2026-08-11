namespace BlazorShop.Storefront.Components.Contracts.Navigation;

public sealed record StorefrontPaginationClasses(
    string Root = "",
    string Link = "",
    string CurrentLink = "",
    string InactiveLink = "");
