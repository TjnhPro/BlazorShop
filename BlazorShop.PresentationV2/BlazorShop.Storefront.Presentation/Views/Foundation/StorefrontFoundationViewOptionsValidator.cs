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

        var failures = options.ViewSet
            .GetRequiredSlots()
            .Where(slot => slot.ComponentType is null || !typeof(IComponent).IsAssignableFrom(slot.ComponentType))
            .Select(slot => $"Foundation view slot '{slot.Name}' must be a Blazor component type.")
            .ToArray();

        return failures.Length == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
