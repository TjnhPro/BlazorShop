namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Components.Ssr.Catalog;
using BlazorShop.Storefront.Presentation.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontCatalogFilterPanelSsrTests
{
    [Fact]
    public async Task HiddenOptionalFieldsDoNotRender()
    {
        var html = await RenderAsync(new Dictionary<string, object?>());

        Assert.Contains("method=\"get\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-root\"", html, StringComparison.Ordinal);
        Assert.Contains(">Apply<", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"category\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"q\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"minPrice\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"inStock\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VisibleFieldsPreserveGetNamesSelectionsValuesAndHostSlots()
    {
        var html = await RenderAsync(new Dictionary<string, object?>
        {
            [nameof(StorefrontCatalogFilterPanel.Action)] = "/search",
            [nameof(StorefrontCatalogFilterPanel.Role)] = "search",
            [nameof(StorefrontCatalogFilterPanel.ShowCategory)] = true,
            [nameof(StorefrontCatalogFilterPanel.Categories)] = (IReadOnlyList<CatalogFilterCategoryOption>)
            [
                new("hats", "Hats"),
                new("shirts", "Shirts"),
            ],
            [nameof(StorefrontCatalogFilterPanel.CategorySlug)] = "shirts",
            [nameof(StorefrontCatalogFilterPanel.ShowSearch)] = true,
            [nameof(StorefrontCatalogFilterPanel.SearchTerm)] = "linen",
            [nameof(StorefrontCatalogFilterPanel.ShowPriceRange)] = true,
            [nameof(StorefrontCatalogFilterPanel.MinPrice)] = 12.5m,
            [nameof(StorefrontCatalogFilterPanel.MaxPrice)] = 50m,
            [nameof(StorefrontCatalogFilterPanel.ShowSort)] = true,
            [nameof(StorefrontCatalogFilterPanel.SortBy)] = ProductCatalogSortBy.PriceLowToHigh,
            [nameof(StorefrontCatalogFilterPanel.ShowPageSize)] = true,
            [nameof(StorefrontCatalogFilterPanel.PageSize)] = 48,
            [nameof(StorefrontCatalogFilterPanel.PageSizeOptions)] = (IReadOnlyList<int>)[12, 24, 48],
            [nameof(StorefrontCatalogFilterPanel.ShowStock)] = true,
            [nameof(StorefrontCatalogFilterPanel.InStock)] = true,
            [nameof(StorefrontCatalogFilterPanel.SubmitIcon)] = SubmitIcon,
        });

        Assert.Contains("action=\"/search\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"search\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"category\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"shirts\" selected", html, StringComparison.Ordinal);
        Assert.Contains("name=\"q\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"linen\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"minPrice\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"maxPrice\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"12.5\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"50\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"sortBy\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"displayOrder\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"updated\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"priceLowToHigh\" selected", html, StringComparison.Ordinal);
        Assert.Contains("value=\"priceHighToLow\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"newest\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"pageSize\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"12\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"24\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"48\" selected", html, StringComparison.Ordinal);
        Assert.Contains("name=\"inStock\"", html, StringComparison.Ordinal);
        Assert.Contains("checked", html, StringComparison.Ordinal);
        Assert.Contains("class=\"input-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"select-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"checkbox-label-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"submit-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("data-test-submit-icon", html, StringComparison.Ordinal);
    }

    [Fact]
    public void FilterSourceHasNoClientSideEventHandler()
    {
        var source = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../")),
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor"));

        Assert.DoesNotContain("onclick", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IJSRuntime", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(Dictionary<string, object?> values)
    {
        values[nameof(StorefrontCatalogFilterPanel.Classes)] = Classes;
        values[nameof(StorefrontCatalogFilterPanel.Labels)] = Labels;

        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<StorefrontCatalogFilterPanel>(ParameterView.FromDictionary(values));
            return component.ToHtmlString();
        });
    }

    private static RenderFragment SubmitIcon => builder => builder.AddMarkupContent(0, "<svg data-test-submit-icon></svg>");

    private static StorefrontCatalogFilterPanelClasses Classes { get; } = new(
        Root: "filter-root",
        Input: "input-slot",
        Select: "select-slot",
        CheckboxLabel: "checkbox-label-slot",
        SubmitButton: "submit-slot",
        Checkbox: "checkbox-slot");

    private static StorefrontCatalogFilterPanelLabels Labels { get; } = new(
        AllCategories: "All",
        SearchPlaceholder: "Search products",
        MinPricePlaceholder: "Min price",
        MaxPricePlaceholder: "Max price",
        SortAriaLabel: "Sort products",
        CategoryAriaLabel: "Filter by category",
        PageSizeAriaLabel: "Products per page",
        FeaturedSort: "Featured",
        RecentlyUpdatedSort: "Recently updated",
        PriceLowSort: "Price low",
        PriceHighSort: "Price high",
        NewestSort: "Newest",
        InStock: "In stock",
        Submit: "Apply",
        PerPageSuffix: "per page");
}
