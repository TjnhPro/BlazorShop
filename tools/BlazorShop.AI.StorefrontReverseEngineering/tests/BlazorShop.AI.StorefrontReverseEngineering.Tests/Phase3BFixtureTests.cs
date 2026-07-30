using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3BFixtureTests
{
    [Theory]
    [InlineData("phase3b-home.html", new[] { "site-header", "hero", "category-card", "product-grid", "promo-strip", "newsletter", "site-footer", "mobile-menu" })]
    [InlineData("phase3b-plp.html", new[] { "breadcrumb", "filter-trigger", "sort-selector", "product-grid", "pagination", "mobile-filter-drawer" })]
    [InlineData("phase3b-pdp.html", new[] { "product-gallery", "product-title", "price", "option-selector", "quantity-selector", "add-to-cart", "accordion", "reviews", "related-products" })]
    [InlineData("phase3b-unsupported.html", new[] { "immersive-object", "custom-orbit", "checkout-widget" })]
    public void Phase3BFixtures_ContainRequiredMarkers(string fileName, string[] markers)
    {
        var content = File.ReadAllText(Path.Combine(FixtureRoot(), fileName));

        Assert.All(markers, marker => Assert.Contains(marker, content, StringComparison.Ordinal));
        Assert.Contains("@media", content, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string FixtureRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures");
    }
}
