namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Product;
using BlazorShop.Storefront.Components.Ssr.Product;
using BlazorShop.Storefront.Presentation.Services.Product;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontProductDetailDisplayComponentTests
{
    [Fact]
    public async Task PricingRendersLabelPricesAndSemanticHooks()
    {
        var html = await RenderPricingAsync(new StorefrontProductPricingView("Price", "$24.00", "$34.00", "USD"));

        Assert.Contains("class=\"pricing-root\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"pricing-label\"", html, StringComparison.Ordinal);
        Assert.Contains(">Price<", html, StringComparison.Ordinal);
        Assert.Contains("class=\"pricing-price\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-selection-price", html, StringComparison.Ordinal);
        Assert.Contains("$24.00", html, StringComparison.Ordinal);
        Assert.Contains("class=\"pricing-compare\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-selection-compare", html, StringComparison.Ordinal);
        Assert.Contains("$34.00", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PricingHidesComparePriceWhenAbsent()
    {
        var html = await RenderPricingAsync(new StorefrontProductPricingView("Price", "$24.00", null, "USD"));

        Assert.Contains("class=\"pricing-compare is-hidden\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-selection-compare", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AvailabilityRendersMetadataHooksAndAvailableStateClass()
    {
        var html = await RenderAvailabilityAsync(
            new StorefrontProductAvailabilityView("in-stock", "Available", "12 in stock", "Size M"),
            Purchase(sku: "SKU-1", gtin: "GTIN-1"));

        Assert.Contains("class=\"availability-root\"", html, StringComparison.Ordinal);
        Assert.Contains(">Size M<", html, StringComparison.Ordinal);
        Assert.Contains("class=\"metadata\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-selection-sku", html, StringComparison.Ordinal);
        Assert.Contains("SKU-1", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-selection-gtin", html, StringComparison.Ordinal);
        Assert.Contains("GTIN-1", html, StringComparison.Ordinal);
        Assert.Contains("class=\"stock-available\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-selection-stock", html, StringComparison.Ordinal);
        Assert.Contains("12 in stock", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AvailabilityHidesBlankSkuGtinAndUsesUnavailableClass()
    {
        var html = await RenderAvailabilityAsync(
            new StorefrontProductAvailabilityView("out-of-stock", "Unavailable", "Out of stock", "Default variant"),
            Purchase(sku: "", gtin: " "));

        Assert.Contains("class=\"metadata is-hidden\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"stock-unavailable\"", html, StringComparison.Ordinal);
        Assert.Contains("Out of stock", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VariantListRendersNothingWhenEmpty()
    {
        var html = await RenderVariantListAsync([]);

        Assert.Equal(string.Empty, html.Trim());
    }

    [Fact]
    public async Task VariantListRendersVariantsWithHostLabelAndStateClasses()
    {
        var html = await RenderVariantListAsync(
        [
            Variant("Small", "Size S", "$20.00", "Available", "in-stock"),
            Variant("Large", "Size L", "$22.00", "Out of stock", "out-of-stock"),
        ]);

        Assert.Contains("class=\"variant-list-root\"", html, StringComparison.Ordinal);
        Assert.Contains(">Variant options<", html, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(html, "<li class=\"variant-item\">"));
        Assert.Contains(">Small<", html, StringComparison.Ordinal);
        Assert.Contains(">Large<", html, StringComparison.Ordinal);
        Assert.Contains(">Size S<", html, StringComparison.Ordinal);
        Assert.Contains(">Size L<", html, StringComparison.Ordinal);
        Assert.Contains("$20.00", html, StringComparison.Ordinal);
        Assert.Contains("$22.00", html, StringComparison.Ordinal);
        Assert.Contains("class=\"variant-stock-available\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"variant-stock-unavailable\"", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderPricingAsync(StorefrontProductPricingView model)
    {
        return await RenderAsync<StorefrontProductPricing>(new Dictionary<string, object?>
        {
            [nameof(StorefrontProductPricing.Model)] = model,
            [nameof(StorefrontProductPricing.Classes)] = PricingClasses,
        });
    }

    private static async Task<string> RenderAvailabilityAsync(
        StorefrontProductAvailabilityView availability,
        StorefrontProductPurchaseView purchase)
    {
        return await RenderAsync<StorefrontProductAvailability>(new Dictionary<string, object?>
        {
            [nameof(StorefrontProductAvailability.Availability)] = availability,
            [nameof(StorefrontProductAvailability.Purchase)] = purchase,
            [nameof(StorefrontProductAvailability.Classes)] = AvailabilityClasses,
        });
    }

    private static async Task<string> RenderVariantListAsync(IReadOnlyList<StorefrontProductVariantView> items)
    {
        return await RenderAsync<StorefrontProductVariantList>(new Dictionary<string, object?>
        {
            [nameof(StorefrontProductVariantList.Items)] = items,
            [nameof(StorefrontProductVariantList.Labels)] = VariantListLabels,
            [nameof(StorefrontProductVariantList.Classes)] = VariantListClasses,
        });
    }

    private static async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> values)
        where TComponent : IComponent
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(values));
            return component.ToHtmlString();
        });
    }

    private static StorefrontProductPurchaseView Purchase(string sku, string gtin)
    {
        return new StorefrontProductPurchaseView(true, string.Empty, string.Empty, sku, gtin, 1, null, 10);
    }

    private static StorefrontProductVariantView Variant(
        string displayName,
        string attributeText,
        string price,
        string stock,
        string state)
    {
        return new StorefrontProductVariantView(Guid.NewGuid(), displayName, attributeText, price, stock, state, false);
    }

    private static ProductPricingClasses PricingClasses { get; } = new(
        Root: "pricing-root",
        Label: "pricing-label",
        PriceRow: "pricing-row",
        Price: "pricing-price",
        ComparePrice: "pricing-compare",
        Hidden: "is-hidden");

    private static ProductAvailabilityClasses AvailabilityClasses { get; } = new(
        Root: "availability-root",
        Summary: "summary",
        Metadata: "metadata",
        StockAvailable: "stock-available",
        StockUnavailable: "stock-unavailable",
        Hidden: "is-hidden");

    private static ProductVariantListLabels VariantListLabels { get; } = new("Variant options");

    private static ProductVariantListClasses VariantListClasses { get; } = new(
        Root: "variant-list-root",
        Heading: "variant-heading",
        List: "variant-list",
        Item: "variant-item",
        Name: "variant-name",
        Attribute: "variant-attribute",
        Details: "variant-details",
        Price: "variant-price",
        StockAvailable: "variant-stock-available",
        StockUnavailable: "variant-stock-unavailable");

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
