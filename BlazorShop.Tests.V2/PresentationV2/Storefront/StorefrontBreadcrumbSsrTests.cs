namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Ssr.Navigation;
using BlazorShop.Storefront.Presentation.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontBreadcrumbSsrTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ZeroOrOneItemRendersNothing(int itemCount)
    {
        var items = Enumerable.Range(0, itemCount)
            .Select(index => new StorefrontBreadcrumbItem($"Item {index}"))
            .ToArray();

        var html = await RenderAsync(items);

        Assert.Equal(string.Empty, html.Trim());
    }

    [Fact]
    public async Task MultipleItemsRenderAncestorLinkCurrentSpanSeparatorsAndHostClasses()
    {
        var html = await RenderAsync(
        [
            new StorefrontBreadcrumbItem("Home", "/"),
            new StorefrontBreadcrumbItem("Hats", "/category/hats"),
            new StorefrontBreadcrumbItem("Canvas Hat"),
        ]);

        Assert.Contains("<nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-label=\"Product trail\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"breadcrumb-list\"", html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"/\" class=\"breadcrumb-link\">Home</a>", html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"/category/hats\" class=\"breadcrumb-link\">Hats</a>", html, StringComparison.Ordinal);
        Assert.Contains("<span aria-current=\"page\" class=\"breadcrumb-current\">Canvas Hat</span>", html, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(html, "class=\"breadcrumb-separator\""));
        Assert.Equal(2, CountOccurrences(html, ">/</span>"));
    }

    [Fact]
    public void BreadcrumbSourceDoesNotConstructRoutes()
    {
        var source = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../")),
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Navigation/StorefrontBreadcrumb.razor"));

        Assert.DoesNotContain("StorefrontRoutes", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CategoryUrl", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SearchUrl", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(IReadOnlyList<StorefrontBreadcrumbItem> items)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<StorefrontBreadcrumb>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StorefrontBreadcrumb.Items)] = items,
                [nameof(StorefrontBreadcrumb.Classes)] = Classes,
                [nameof(StorefrontBreadcrumb.Labels)] = Labels,
            }));
            return component.ToHtmlString();
        });
    }

    private static StorefrontBreadcrumbClasses Classes { get; } = new(
        Root: "breadcrumb-root",
        List: "breadcrumb-list",
        Item: "breadcrumb-item",
        Link: "breadcrumb-link",
        Current: "breadcrumb-current",
        Separator: "breadcrumb-separator");

    private static StorefrontBreadcrumbLabels Labels { get; } = new("Product trail", "/");

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
