namespace BlazorShop.Storefront.Components.Contracts.Components;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

public static class StorefrontComponentDescriptorValidator
{
    private static readonly Regex ComponentKeyPattern = new(
        "^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static StorefrontComponentDescriptorValidationResult Validate(StorefrontComponentDescriptor? descriptor)
    {
        if (descriptor is null)
        {
            return new StorefrontComponentDescriptorValidationResult(["Descriptor is required."]);
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(descriptor.Key))
        {
            errors.Add("Key is required.");
        }
        else if (!ComponentKeyPattern.IsMatch(descriptor.Key))
        {
            errors.Add("Key must be lowercase kebab-case.");
        }

        if (!Enum.IsDefined(descriptor.Mode))
        {
            errors.Add("Mode must be a defined StorefrontComponentMode value.");
        }

        if (!Enum.IsDefined(descriptor.Category))
        {
            errors.Add("Category must be a defined StorefrontComponentCategory value.");
        }

        if (descriptor.ComponentType is null)
        {
            errors.Add("ComponentType is required.");
        }
        else if (!typeof(IComponent).IsAssignableFrom(descriptor.ComponentType))
        {
            errors.Add("ComponentType must implement Microsoft.AspNetCore.Components.IComponent.");
        }

        return errors.Count == 0
            ? StorefrontComponentDescriptorValidationResult.Valid
            : new StorefrontComponentDescriptorValidationResult(errors);
    }
}
