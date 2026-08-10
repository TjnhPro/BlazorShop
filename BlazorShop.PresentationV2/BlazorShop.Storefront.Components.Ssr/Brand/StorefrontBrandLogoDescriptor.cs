namespace BlazorShop.Storefront.Components.Ssr.Brand;

using BlazorShop.Storefront.Components.Contracts.Components;

public static class StorefrontBrandLogoDescriptor
{
    public static StorefrontComponentDescriptor Descriptor { get; } = new(
        "brand-logo",
        StorefrontComponentMode.Ssr,
        StorefrontComponentCategory.Brand,
        typeof(StorefrontBrandLogo));
}
