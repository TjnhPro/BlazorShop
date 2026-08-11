namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.Catalog;
using BlazorShop.Storefront.Components.Primitives.Catalog;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontProductSummaryPrimitiveComponentTests
{
    [Fact]
    public async Task ImageRendersImageAndHiddenFallbackWhenImageUrlExists()
    {
        var html = await RenderImageAsync(Product(imageUrl: "/media/products/sun-hat.webp"));

        Assert.Contains("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src=\"/media/products/sun-hat.webp\"", html, StringComparison.Ordinal);
        Assert.Contains("alt=\"Sun Hat\"", html, StringComparison.Ordinal);
        Assert.Contains("loading=\"lazy\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"image-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("onerror=\"this.onerror=null;", html, StringComparison.Ordinal);
        Assert.Contains("role=\"img\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Image unavailable for Sun Hat\"", html, StringComparison.Ordinal);
        Assert.Contains("Image unavailable", html, StringComparison.Ordinal);
        Assert.Contains("hidden", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImageRendersAccessibleFallbackWhenImageUrlIsMissing()
    {
        var html = await RenderImageAsync(Product(imageUrl: null));

        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("class=\"image-fallback-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"img\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Image unavailable for Sun Hat\"", html, StringComparison.Ordinal);
        Assert.Contains("Image unavailable", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PurchaseActionsRenderDirectAddSemanticsForDirectAddItem()
    {
        var html = await RenderPurchaseAsync(Product(canAddDirectly: true, currencyCode: "USD"));

        Assert.Contains("<button", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-storefront-product-purchase", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-command=\"cart.add-line\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-product-purchase-submit", html, StringComparison.Ordinal);
        Assert.Contains("data-default-label=\"Add to cart\"", html, StringComparison.Ordinal);
        Assert.Contains("data-success-label=\"Added\"", html, StringComparison.Ordinal);
        Assert.Contains("data-product-id=\"11111111-1111-1111-1111-111111111111\"", html, StringComparison.Ordinal);
        Assert.Contains("data-product-name=\"Sun Hat\"", html, StringComparison.Ordinal);
        Assert.Contains("data-currency-code=\"USD\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"primary-action-slot\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PurchaseActionsRenderVariantLinkWithoutSubmitHook()
    {
        var html = await RenderPurchaseAsync(Product(hasVariants: true, purchaseUrl: "/products/sun-hat#variants"));

        Assert.Contains("<a class=\"primary-action-slot\" href=\"/products/sun-hat#variants\">Add to cart</a>", html, StringComparison.Ordinal);
        Assert.Contains("Select a variant", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-product-purchase-submit", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-command=\"cart.add-line\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PurchaseActionsRenderSecondaryViewProductLinkWhenProductUrlExists()
    {
        var html = await RenderPurchaseAsync(Product());

        Assert.Contains("<a class=\"secondary-action-slot\" href=\"/products/sun-hat\">View product</a>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-product-purchase-submit", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PurchasePausedMessageWinsBeforeOutOfStock()
    {
        var html = await RenderPurchaseAsync(Product(
            inStock: false,
            purchasable: false,
            purchasePaused: true,
            purchaseBlockMessage: "Purchasing is paused"));

        Assert.Contains("Purchasing is paused", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Currently out of stock", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Currently unavailable", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PurchaseActionsRenderOutOfStockAndUnavailableStates()
    {
        var outOfStock = await RenderPurchaseAsync(Product(inStock: false, purchasable: true));
        var unavailable = await RenderPurchaseAsync(Product(purchasable: false));

        Assert.Contains("Currently out of stock", outOfStock, StringComparison.Ordinal);
        Assert.Contains("Currently unavailable", unavailable, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CardRendersSummarySemanticsAndComposedPrimitiveHooks()
    {
        var html = await RenderCardAsync(Product(
            imageUrl: "/media/products/sun-hat.webp",
            description: "Packable coastal sun protection.",
            comparePrice: "USD 29.00",
            hasVariants: true,
            isNewArrival: true));

        Assert.Contains("class=\"root-slot\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-product-summary-card", html, StringComparison.Ordinal);
        Assert.Contains("<a class=\"category-slot\" href=\"/categories/hats\">Hats</a>", html, StringComparison.Ordinal);
        Assert.Contains("<a class=\"title-slot\" href=\"/products/sun-hat\"", html, StringComparison.Ordinal);
        Assert.Contains("<h2>Sun Hat</h2>", html, StringComparison.Ordinal);
        Assert.Contains("badge-slot new-badge-slot", html, StringComparison.Ordinal);
        Assert.Contains("New", html, StringComparison.Ordinal);
        Assert.Contains("badge-slot variants-badge-slot", html, StringComparison.Ordinal);
        Assert.Contains("Variants", html, StringComparison.Ordinal);
        Assert.Contains("<span>From</span>", html, StringComparison.Ordinal);
        Assert.Contains("<strong>USD 24.00</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<del class=\"compare-price-slot\">USD 29.00</del>", html, StringComparison.Ordinal);
        Assert.Contains("Packable coastal sun protection.", html, StringComparison.Ordinal);
        Assert.Contains("onerror=\"this.onerror=null;", html, StringComparison.Ordinal);
        Assert.Contains("Select a variant", html, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimitiveSourceDoesNotContainV2VisualClassTokens()
    {
        var source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives"), "*.razor", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        var classValues = global::System.Text.RegularExpressions.Regex.Matches(source, "class=\"(?<value>[^\"]*)\"")
            .Select(match => match.Groups["value"].Value);

        foreach (var classValue in classValues)
        {
            Assert.DoesNotContain("rounded-", classValue, StringComparison.Ordinal);
            Assert.DoesNotContain("bg-", classValue, StringComparison.Ordinal);
            Assert.DoesNotContain("text-", classValue, StringComparison.Ordinal);
            Assert.DoesNotContain("shadow", classValue, StringComparison.Ordinal);
            Assert.DoesNotContain("group", classValue, StringComparison.Ordinal);
        }
    }

    private static async Task<string> RenderImageAsync(ProductSummaryItem item)
    {
        return await RenderAsync<StorefrontProductSummaryImage>(item);
    }

    private static async Task<string> RenderPurchaseAsync(ProductSummaryItem item)
    {
        return await RenderAsync<StorefrontProductSummaryPurchaseActions>(item);
    }

    private static async Task<string> RenderCardAsync(ProductSummaryItem item)
    {
        return await RenderAsync<StorefrontProductSummaryCard>(item);
    }

    private static async Task<string> RenderAsync<TComponent>(ProductSummaryItem item)
        where TComponent : IComponent
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Item"] = item,
                ["Labels"] = Labels,
                ["Classes"] = Classes,
            });

            var component = await renderer.RenderComponentAsync<TComponent>(parameters);
            return component.ToHtmlString();
        });
    }

    private static ProductSummaryItem Product(
        string? imageUrl = null,
        string? description = null,
        string? comparePrice = null,
        bool hasVariants = false,
        bool inStock = true,
        bool isNewArrival = false,
        bool purchasable = true,
        string? purchaseUrl = null,
        bool canAddDirectly = false,
        string? currencyCode = null,
        string? purchaseBlockMessage = null,
        bool purchasePaused = false)
    {
        return new ProductSummaryItem(
            new Guid("11111111-1111-1111-1111-111111111111"),
            "Sun Hat",
            "/products/sun-hat",
            "Hats",
            "/categories/hats",
            imageUrl,
            description,
            "USD 24.00",
            comparePrice,
            hasVariants,
            inStock,
            isNewArrival,
            purchasable,
            purchaseUrl,
            canAddDirectly,
            CurrencyCode: currencyCode,
            PurchaseBlockMessage: purchaseBlockMessage,
            PurchasePaused: purchasePaused);
    }

    private static ProductSummaryLabels Labels { get; } = new(
        "From",
        "Price",
        "Image unavailable",
        "Image unavailable for {0}",
        "New",
        "Variants",
        "Out of stock",
        "Add to cart",
        "Added",
        "View product",
        "Select a variant",
        "Currently out of stock",
        "Currently unavailable");

    private static ProductSummaryCardClasses Classes { get; } = new(
        Root: "root-slot",
        Body: "body-slot",
        Header: "header-slot",
        Category: "category-slot",
        Title: "title-slot",
        BadgeGroup: "badge-group-slot",
        Badge: "badge-slot",
        NewBadge: "new-badge-slot",
        VariantsBadge: "variants-badge-slot",
        OutOfStockBadge: "out-of-stock-badge-slot",
        Price: "price-slot",
        ComparePrice: "compare-price-slot",
        ImageLink: "image-link-slot",
        ImageFrame: "image-frame-slot",
        Image: "image-slot",
        ImageFallback: "image-fallback-slot",
        Description: "description-slot",
        Footer: "footer-slot",
        ActionGroup: "action-group-slot",
        PrimaryAction: "primary-action-slot",
        SecondaryAction: "secondary-action-slot",
        Status: "status-slot",
        WarningStatus: "warning-status-slot");

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
