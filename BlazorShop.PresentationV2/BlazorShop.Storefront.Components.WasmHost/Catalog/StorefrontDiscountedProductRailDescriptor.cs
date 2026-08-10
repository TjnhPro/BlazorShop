namespace BlazorShop.Storefront.Components.WasmHost.Catalog;

using BlazorShop.Storefront.Components.Contracts.Components;

public static class StorefrontDiscountedProductRailDescriptor
{
    public static StorefrontComponentDescriptor Descriptor { get; } = new(
        "discounted-product-rail",
        StorefrontComponentMode.WasmHost,
        StorefrontComponentCategory.Catalog,
        typeof(StorefrontDiscountedProductRail));
}
