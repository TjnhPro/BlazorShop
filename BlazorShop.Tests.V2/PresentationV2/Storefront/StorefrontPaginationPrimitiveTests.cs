namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Navigation;
using BlazorShop.Storefront.Components.Primitives.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontPaginationPrimitiveTests
{
    [Fact]
    public async Task EmptyItemsRenderNoNavigation()
    {
        var html = await RenderAsync([]);

        Assert.Equal(string.Empty, html.Trim());
    }

    [Fact]
    public async Task OneItemUsesHostSuppliedHrefLabelAndAriaLabel()
    {
        var html = await RenderAsync([new StorefrontPaginationItem(7, "/category/hats?page=7", false, "Seventh")]);

        Assert.Contains("<nav", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-label=\"Catalog pages\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/category/hats?page=7\"", html, StringComparison.Ordinal);
        Assert.Contains(">Seventh<", html, StringComparison.Ordinal);
        Assert.Contains("class=\"page-link inactive\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-current", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MultipleItemsUseCurrentClassAriaAndPageNumberFallback()
    {
        var html = await RenderAsync(
        [
            new StorefrontPaginationItem(1, "/search?q=hat&page=1", false),
            new StorefrontPaginationItem(2, "/search?q=hat&page=2", true),
        ]);

        Assert.Contains("href=\"/search?q=hat&amp;page=1\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/search?q=hat&amp;page=2\"", html, StringComparison.Ordinal);
        Assert.Contains(">1<", html, StringComparison.Ordinal);
        Assert.Contains(">2<", html, StringComparison.Ordinal);
        Assert.Contains("class=\"page-link current\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-current=\"page\"", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(IReadOnlyList<StorefrontPaginationItem> items)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<StorefrontPagination>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StorefrontPagination.Items)] = items,
                [nameof(StorefrontPagination.Classes)] = Classes,
                [nameof(StorefrontPagination.Labels)] = Labels,
            }));
            return component.ToHtmlString();
        });
    }

    private static StorefrontPaginationClasses Classes { get; } = new(
        Root: "pagination-root",
        Link: "page-link",
        CurrentLink: "current",
        InactiveLink: "inactive");

    private static StorefrontPaginationLabels Labels { get; } = new("Catalog pages");
}
