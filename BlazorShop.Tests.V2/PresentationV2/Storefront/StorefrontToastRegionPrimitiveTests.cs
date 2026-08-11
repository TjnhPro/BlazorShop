namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.System;
using BlazorShop.Storefront.Components.Primitives.System;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontToastRegionPrimitiveTests
{
    [Fact]
    public async Task RendersOneSiblingRegionAndTemplateWithAllSemanticHooks()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<StorefrontToastRegion>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Classes"] = new StorefrontToastRegionClasses(Region: "region-slot", Toast: "toast-slot", CloseButton: "close-slot"),
            ["Labels"] = new StorefrontToastRegionLabels("Close fixture"),
        }))).ToHtmlString());

        Assert.Equal(1, Count(html, "data-storefront-toast-region"));
        Assert.Equal(1, Count(html, "data-storefront-toast-template"));
        Assert.True(html.IndexOf("data-storefront-toast-region", StringComparison.Ordinal) < html.IndexOf("data-storefront-toast-template", StringComparison.Ordinal));
        foreach (var icon in new[] { "info", "success", "warning", "error" }) Assert.Contains($"data-storefront-toast-icon=\"{icon}\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Close fixture\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimitiveSourceHasNoFinalVisualOrFallbackCopy()
    {
        var source = File.ReadAllText(Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../")), "BlazorShop.PresentationV2", "BlazorShop.Storefront.Components.Primitives", "System", "StorefrontToastRegion.razor"));
        foreach (var token in new[] { "rounded-", "bg-", "Dismiss notification", "@rendermode" }) Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string text, string value) => text.Split(value, StringSplitOptions.None).Length - 1;
}
