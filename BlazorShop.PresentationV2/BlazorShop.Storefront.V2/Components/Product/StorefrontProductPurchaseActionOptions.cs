namespace BlazorShop.Storefront.Components.Product;

using BlazorShop.Storefront.Components.Headless.Product;

public static class StorefrontProductPurchaseActionOptions
{
    public static ProductPurchaseActionDescriptor Default { get; } = new(
        "purchase",
        "/api/product-selection-preview",
        "product-cart-feedback",
        "product-variant-select",
        "product-selection-quantity");
}
