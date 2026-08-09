namespace BlazorShop.Storefront.Components.Contracts.Components;

public sealed record StorefrontComponentDescriptorValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => this.Errors.Count == 0;

    public static StorefrontComponentDescriptorValidationResult Valid { get; } = new(Array.Empty<string>());
}
