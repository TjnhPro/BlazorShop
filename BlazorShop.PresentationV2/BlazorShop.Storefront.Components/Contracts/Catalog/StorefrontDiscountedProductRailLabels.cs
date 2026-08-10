namespace BlazorShop.Storefront.Components.Contracts.Catalog;

public sealed record StorefrontDiscountedProductRailLabels(
    string Heading,
    string LoadingMessage,
    string EmptyMessage,
    string ErrorMessage,
    string RetryLabel);
