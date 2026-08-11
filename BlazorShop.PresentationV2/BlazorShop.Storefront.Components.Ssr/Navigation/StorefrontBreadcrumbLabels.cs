namespace BlazorShop.Storefront.Components.Ssr.Navigation;

public sealed record StorefrontBreadcrumbLabels(
    string AriaLabel = "Breadcrumb",
    string SeparatorText = "/");
