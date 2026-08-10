namespace BlazorShop.Storefront.Components.Contracts.Components;

/// <summary>
/// Describes a reusable Storefront component's public identity and semantic runtime classification.
/// The descriptor is not a registry entry and does not define the component's physical assembly owner.
/// </summary>
public sealed record StorefrontComponentDescriptor(
    string Key,
    StorefrontComponentMode Mode,
    StorefrontComponentCategory Category,
    Type ComponentType);
