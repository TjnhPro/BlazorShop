namespace BlazorShop.Storefront.Components.Ssr.Navigation;

public sealed record StorefrontBreadcrumbClasses(
    string Root = "",
    string List = "",
    string Item = "",
    string Link = "",
    string Current = "",
    string Separator = "");
