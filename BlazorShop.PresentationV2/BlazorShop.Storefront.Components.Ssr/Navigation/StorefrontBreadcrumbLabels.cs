namespace BlazorShop.Storefront.Components.Ssr.Navigation;

public sealed record StorefrontBreadcrumbLabels(
    string AriaLabel = "",
    string SeparatorText = "")
{
    public static StorefrontBreadcrumbLabels Empty { get; } = new();
}
