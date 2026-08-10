namespace BlazorShop.Storefront.Components.WasmHost.Content;

using BlazorShop.Storefront.Components.Contracts.Components;

public static class StorefrontContactFormDescriptor
{
    public static StorefrontComponentDescriptor Descriptor { get; } = new(
        "contact-form",
        StorefrontComponentMode.Hybrid,
        StorefrontComponentCategory.Content,
        typeof(StorefrontContactFormApp));
}
