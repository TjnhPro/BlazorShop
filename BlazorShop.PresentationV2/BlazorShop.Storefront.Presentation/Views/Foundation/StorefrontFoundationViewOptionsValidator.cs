namespace BlazorShop.Storefront.Presentation.Views.Foundation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

public sealed class StorefrontFoundationViewOptionsValidator : IValidateOptions<StorefrontFoundationViewOptions>
{
    public ValidateOptionsResult Validate(string? name, StorefrontFoundationViewOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ViewSet is null)
        {
            return ValidateOptionsResult.Fail("A StorefrontFoundationViewSet must be registered.");
        }

        var failures = new List<string>();
        foreach (var slot in options.ViewSet.GetRequiredSlots())
        {
            if (slot.ComponentType is null || !typeof(IComponent).IsAssignableFrom(slot.ComponentType))
            {
                failures.Add($"Foundation view slot '{slot.Name}' must be a Blazor component type.");
                continue;
            }

            if (slot.ComponentType == typeof(StorefrontFoundationEmptyView))
            {
                failures.Add($"Foundation view slot '{slot.Name}' must not use StorefrontFoundationEmptyView.");
            }

            if (slot.ComponentType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Length > 0)
            {
                failures.Add($"Foundation view slot '{slot.Name}' must be a visual component, not a route component.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
