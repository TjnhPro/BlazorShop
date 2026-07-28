namespace BlazorShop.Storefront.Presentation.Views.Foundation;

using System.Reflection;
using BlazorShop.Storefront.Presentation.Services.Account;
using BlazorShop.Storefront.Presentation.Services.Auth;
using BlazorShop.Storefront.Presentation.Services.Cart;
using BlazorShop.Storefront.Presentation.Services.Catalog;
using BlazorShop.Storefront.Presentation.Services.Checkout;
using BlazorShop.Storefront.Presentation.Services.Content;
using BlazorShop.Storefront.Presentation.Services.Product;
using BlazorShop.Storefront.Presentation.Services.System;
using BlazorShop.Storefront.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

public sealed class StorefrontFoundationViewOptionsValidator : IValidateOptions<StorefrontFoundationViewOptions>
{
    private static readonly IReadOnlyDictionary<string, Type> ExpectedContextTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [nameof(StorefrontFoundationViewSet.ApplicationHead)] = typeof(StorefrontShellContext),
        [nameof(StorefrontFoundationViewSet.MainLayout)] = typeof(StorefrontShellContext),
        [nameof(StorefrontFoundationViewSet.HomePage)] = typeof(StorefrontHomePageContext),
        [nameof(StorefrontFoundationViewSet.CategoryPage)] = typeof(StorefrontCategoryPageContext),
        [nameof(StorefrontFoundationViewSet.ProductPage)] = typeof(StorefrontProductPageContext),
        [nameof(StorefrontFoundationViewSet.SearchPage)] = typeof(StorefrontSearchPageContext),
        [nameof(StorefrontFoundationViewSet.DealsPage)] = typeof(StorefrontDealsPageContext),
        [nameof(StorefrontFoundationViewSet.NewReleasesPage)] = typeof(StorefrontNewReleasesPageContext),
        [nameof(StorefrontFoundationViewSet.ContentPage)] = typeof(StorefrontContentPageContext),
        [nameof(StorefrontFoundationViewSet.CartPage)] = typeof(StorefrontCartPageContext),
        [nameof(StorefrontFoundationViewSet.CheckoutPage)] = typeof(StorefrontCheckoutPageContext),
        [nameof(StorefrontFoundationViewSet.PaymentResultPage)] = typeof(StorefrontPaymentResultPageContext),
        [nameof(StorefrontFoundationViewSet.AuthPage)] = typeof(StorefrontAuthPageContext),
        [nameof(StorefrontFoundationViewSet.AccountPage)] = typeof(StorefrontAccountPageContext),
        [nameof(StorefrontFoundationViewSet.MaintenanceState)] = typeof(StorefrontSystemStateContext),
        [nameof(StorefrontFoundationViewSet.NotFoundState)] = typeof(StorefrontSystemStateContext),
        [nameof(StorefrontFoundationViewSet.ServiceUnavailableState)] = typeof(StorefrontSystemStateContext),
        [nameof(StorefrontFoundationViewSet.ErrorState)] = typeof(StorefrontSystemStateContext),
    };

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

            var componentType = slot.ComponentType;

            if (componentType == typeof(StorefrontFoundationEmptyView))
            {
                failures.Add($"Foundation view slot '{slot.Name}' must not use StorefrontFoundationEmptyView.");
            }

            if (slot.Name == nameof(StorefrontFoundationViewSet.VisualScripts)
                && componentType == typeof(StorefrontFoundationCoreScripts))
            {
                failures.Add("Foundation view slot 'VisualScripts' must be host visual script markup and cannot replace Presentation-owned core scripts.");
            }

            if (componentType.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Length > 0)
            {
                failures.Add($"Foundation view slot '{slot.Name}' must be a visual component, not a route component.");
            }

            if (ExpectedContextTypes.TryGetValue(slot.Name, out var expectedContextType))
            {
                ValidateContextParameter(slot.Name, componentType, expectedContextType, failures);
            }

            if (slot.Name == nameof(StorefrontFoundationViewSet.MainLayout))
            {
                ValidateBodyParameter(slot.Name, componentType, failures);
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateContextParameter(
        string slotName,
        Type componentType,
        Type expectedContextType,
        List<string> failures)
    {
        var contextParameter = componentType.GetProperty(
            StorefrontFoundationViewTypeValidator.ContextParameterName,
            BindingFlags.Instance | BindingFlags.Public);

        if (contextParameter is null
            || contextParameter.GetCustomAttribute<ParameterAttribute>() is null)
        {
            failures.Add(
                $"Foundation view slot '{slotName}' must expose a public [Parameter] named '{StorefrontFoundationViewTypeValidator.ContextParameterName}' for '{expectedContextType.FullName}'.");
            return;
        }

        if (!contextParameter.PropertyType.IsAssignableFrom(expectedContextType))
        {
            failures.Add(
                $"Foundation view slot '{slotName}' context parameter expects '{contextParameter.PropertyType.FullName}', which cannot accept '{expectedContextType.FullName}'.");
        }
    }

    private static void ValidateBodyParameter(
        string slotName,
        Type componentType,
        List<string> failures)
    {
        var bodyParameter = componentType.GetProperty(
            nameof(LayoutComponentBase.Body),
            BindingFlags.Instance | BindingFlags.Public);

        if (bodyParameter is null
            || bodyParameter.GetCustomAttribute<ParameterAttribute>() is null)
        {
            failures.Add($"Foundation view slot '{slotName}' must expose a public [Parameter] named '{nameof(LayoutComponentBase.Body)}'.");
            return;
        }

        if (bodyParameter.PropertyType != typeof(RenderFragment))
        {
            failures.Add($"Foundation view slot '{slotName}' Body parameter must be '{typeof(RenderFragment).FullName}'.");
        }
    }
}
