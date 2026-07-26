namespace BlazorShop.Storefront.Presentation.Views.Foundation;

using System.Reflection;
using Microsoft.AspNetCore.Components;

public static class StorefrontFoundationViewTypeValidator
{
    public const string ContextParameterName = "Context";

    public static void Validate(Type componentType, object? context)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new InvalidOperationException($"Foundation view '{componentType.FullName}' must implement {nameof(IComponent)}.");
        }

        if (context is null)
        {
            return;
        }

        var contextParameter = componentType.GetProperty(
            ContextParameterName,
            BindingFlags.Instance | BindingFlags.Public);
        if (contextParameter is null
            || contextParameter.GetCustomAttribute<ParameterAttribute>() is null)
        {
            throw new InvalidOperationException(
                $"Foundation view '{componentType.FullName}' must expose a [Parameter] named '{ContextParameterName}'.");
        }

        var contextType = context.GetType();
        if (!contextParameter.PropertyType.IsAssignableFrom(contextType))
        {
            throw new InvalidOperationException(
                $"Foundation view '{componentType.FullName}' parameter '{ContextParameterName}' expects '{contextParameter.PropertyType.FullName}', but received '{contextType.FullName}'.");
        }
    }
}
