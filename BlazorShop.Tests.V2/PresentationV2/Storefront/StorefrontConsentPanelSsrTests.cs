namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Ssr.Security;
using BlazorShop.Storefront.Presentation.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontConsentPanelSsrTests
{
    [Fact]
    public async Task RendersNativeHiddenPreparedActionsAndAllConsentControls()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<StorefrontConsentPanel>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            ["Context"] = new StorefrontConsentContext(true, "/pages/privacy", new StorefrontConsentActionContext("/api/current", "/api/save", "/api/revoke", "GET", "POST", "DELETE"), new StorefrontConsentBrowserEvents("changed", "manage")),
            ["Labels"] = new StorefrontConsentPanelLabels("Consent settings", "Privacy", "Fixture copy", "Policy", "Preferences", "Analytics", "Marketing", "Essential", "Revoke", "Save", "Accept"),
            ["Classes"] = new StorefrontConsentPanelClasses(Root: "root-slot", PrimaryButton: "primary-slot"),
        }))).ToHtmlString());

        foreach (var value in new[] { "data-storefront-consent-banner", "data-storefront-consent-current-url=\"/api/current\"", "data-storefront-consent-accept-url=\"/api/save\"", "data-storefront-consent-revoke-url=\"/api/revoke\"", "data-storefront-consent-preferences", "data-storefront-consent-analytics", "data-storefront-consent-marketing", "data-storefront-consent-essential", "data-storefront-consent-revoke", "data-storefront-consent-selected", "data-storefront-consent-all", "hidden" })
        {
            Assert.Contains(value, html, StringComparison.Ordinal);
        }
        Assert.Contains("class=\"root-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Consent settings\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void SsrSourceHasNoVisualCopyRuntimeOrRenderModeDependencies()
    {
        var source = File.ReadAllText(Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../")), "BlazorShop.PresentationV2", "BlazorShop.Storefront.Components.Ssr", "Security", "StorefrontConsentPanel.razor"));
        foreach (var token in new[] { "bg-", "Cookie consent", "HttpClient", "IJSRuntime", "@rendermode" }) Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase);
    }
}
