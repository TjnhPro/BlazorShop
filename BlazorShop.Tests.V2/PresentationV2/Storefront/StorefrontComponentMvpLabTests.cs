extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Brand;
using BlazorShop.Storefront.Presentation.Services.SystemPages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using StorefrontComponentMvpLab = StorefrontV2::BlazorShop.Storefront.V2.Components.System.StorefrontComponentMvpLab;

using Xunit;

public sealed class StorefrontComponentMvpLabTests
{
    [Fact]
    public async Task RendersBrandLogoInSsrSectionWithRawServerHtml()
    {
        var html = await RenderAsync(new StorefrontComponentMvpPageContext(
            new StorefrontBrandLogoContext(
                "/",
                "Kindred Coast",
                "Coastal goods",
                "/media/assets/kindred-logo.svg",
                "Go to Kindred Coast home")));

        Assert.Contains("data-storefront-component-mvp", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-component-mvp-section=\"ssr\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-component=\"brand-logo\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-brand=\"Kindred Coast\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/\"", html, StringComparison.Ordinal);
        Assert.Contains("src=\"/media/assets/kindred-logo.svg\"", html, StringComparison.Ordinal);
        Assert.Contains("alt=\"Kindred Coast\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Go to Kindred Coast home\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"bs-storefront-component-mvp__brand\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"bs-storefront-component-mvp__brand-image\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-component-mvp-placeholder=\"ssr\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Coastal goods", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(StorefrontComponentMvpPageContext context)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Context"] = context,
            });

            var component = await renderer.RenderComponentAsync<StorefrontComponentMvpLab>(parameters);
            return component.ToHtmlString();
        });
    }
}
