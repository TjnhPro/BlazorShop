namespace BlazorShop.Storefront.Components.Contracts.Components;

public sealed record StorefrontComponentDescriptor(
    string Key,
    StorefrontComponentMode Mode,
    StorefrontComponentCategory Category,
    Type ComponentType);
