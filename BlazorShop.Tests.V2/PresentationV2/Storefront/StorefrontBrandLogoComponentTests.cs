namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Brand;
using BlazorShop.Storefront.Components.Ssr.Brand;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontBrandLogoComponentTests
{
    [Fact]
    public async Task RendersOnlyImageWhenLogoUrlIsPresent()
    {
        var html = await RenderAsync(
            new StorefrontBrandLogoContext(
                "/",
                "Kindred Coast",
                "Beach goods",
                "/media/logo.svg",
                "Go to Kindred Coast home"),
            new StorefrontBrandLogoClasses(
                Root: "brand-root",
                Image: "brand-image",
                Mark: "brand-mark",
                Label: "brand-label"));

        Assert.Contains("href=\"/\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Go to Kindred Coast home\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-component=\"brand-logo\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-brand=\"Kindred Coast\"", html, StringComparison.Ordinal);
        Assert.Contains("src=\"/media/logo.svg\"", html, StringComparison.Ordinal);
        Assert.Contains("alt=\"Kindred Coast\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"brand-root\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"brand-image\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"brand-mark\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"brand-label\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Beach goods", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RendersTextFallbackWhenLogoUrlIsBlank()
    {
        var html = await RenderAsync(
            new StorefrontBrandLogoContext(
                "/",
                "Kindred Coast",
                LogoUrl: "   "),
            new StorefrontBrandLogoClasses(Mark: "brand-mark"));

        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-label=\"Kindred Coast\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"brand-mark\"", html, StringComparison.Ordinal);
        Assert.Contains("Kindred Coast", html, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"brand-label\"", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(
        StorefrontBrandLogoContext context,
        StorefrontBrandLogoClasses classes)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StorefrontBrandLogo.Context)] = context,
                [nameof(StorefrontBrandLogo.Classes)] = classes,
            });

            var component = await renderer.RenderComponentAsync<StorefrontBrandLogo>(parameters);
            return component.ToHtmlString();
        });
    }
}
