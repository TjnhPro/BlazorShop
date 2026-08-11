namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Product;
using BlazorShop.Storefront.Components.Headless.Product;
using BlazorShop.Storefront.Components.Primitives.Product;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontProductPurchasePanelPrimitiveTests
{
    [Fact]
    public async Task RendersPurchaseHooksBoundsLabelsAndAccessibleVariationGroups()
    {
        var html = await RenderAsync(Model(variationOptions:
        [new("Color", false, "color", [new("Red", "#ef4444", true), new("Unknown", null, false)]), new("Size", true, "radio", [new("M", null, true)])]));

        foreach (var hook in new[] { "data-storefront-product-purchase", "data-storefront-product-purchase-panel", "data-storefront-attribute-group", "data-storefront-purchase-attribute", "data-storefront-purchase-attribute-name", "data-storefront-purchase-quantity", "data-storefront-command=\"cart.add-line\"", "data-storefront-product-purchase-submit", "data-storefront-purchase-feedback" })
        {
            Assert.Contains(hook, html, StringComparison.Ordinal);
        }

        Assert.Contains("<fieldset", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<legend", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("min=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("max=\"5\"", html, StringComparison.Ordinal);
        Assert.Contains("step=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-default-label=\"Buy\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"missing-swatch-slot\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("#f5f5f5", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RendersBlockedAndLegacyVariantStatesWithoutSharedDefaults()
    {
        var html = await RenderAsync(Model(canSubmit: false, variationOptions: [], variants: [new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "M", "M", "M", null, null, 1, true, "10.00", "USD", "USD 10.00")]));

        Assert.Contains("Blocked by fixture", html, StringComparison.Ordinal);
        Assert.Contains("disabled", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-purchase-variant", html, StringComparison.Ordinal);
        Assert.Contains("Choose fixture variant", html, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimitiveSourceIsVisualAndCopyNeutral()
    {
        var source = File.ReadAllText(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Product/StorefrontProductPurchasePanel.razor"));

        foreach (var token in new[] { "bg-", "rounded-", "Add to Cart", "#f5f5f5", "IJSRuntime", "HttpClient", "@rendermode" })
        {
            Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<string> RenderAsync(ProductPurchasePanelModel model)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<StorefrontProductPurchasePanel>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Model"] = model,
                ["Actions"] = new ProductPurchaseActionDescriptor("fixture-panel", "/api/product-selection-preview", "fixture-feedback", "fixture-variant", "fixture-quantity"),
                ["Labels"] = new ProductPurchaseLabels("Buy", "Bought", "Cart", "Free", "Optional", "Purchase", "Choose fixture variant", "Select fixture variant", "Amount", "Select {0}"),
                ["Classes"] = new ProductPurchasePanelClasses(Root: "root-slot", MissingColorSwatch: "missing-swatch-slot"),
            }));
            return component.ToHtmlString();
        });
    }

    private static ProductPurchasePanelModel Model(bool canSubmit = true, IReadOnlyList<ProductPurchaseOptionItem>? variationOptions = null, IReadOnlyList<ProductPurchaseVariantItem>? variants = null) => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"), "Fixture product", "USD", "10.00", null, null, null, null, 4, 1, 5, 1, false, null, canSubmit, "Ready", "Blocked by fixture", ["Ready"], variationOptions ?? [], variants ?? [], "/my-cart");

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    private static string RepositoryPath(string relativePath) => Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
}
