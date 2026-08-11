namespace BlazorShop.Storefront.Components.Contracts.System;

public sealed record StorefrontToastRegionLabels(
    string CloseButton)
{
    public static StorefrontToastRegionLabels Empty { get; } = new(string.Empty);
}
