using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3CFixtureAndGateTests
{
    [Fact]
    public void Phase3CSiteLevelFixture_CoversRequiredEcommercePages()
    {
        var fixtureRoot = Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "Phase3C");
        var expectedPages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["site-home.html"] = "home",
            ["site-category.html"] = "product-listing",
            ["site-product.html"] = "product-detail",
            ["site-cart.html"] = "cart-shell",
            ["site-checkout.html"] = "checkout-shell",
            ["site-account.html"] = "account-auth-shell",
            ["site-system-state.html"] = "content-page"
        };

        foreach (var (fileName, archetype) in expectedPages)
        {
            var html = File.ReadAllText(Path.Combine(fixtureRoot, fileName));
            Assert.Contains("data-phase3c-fixture=\"site-level\"", html, StringComparison.Ordinal);
            Assert.Contains($"data-page-archetype=\"{archetype}\"", html, StringComparison.Ordinal);
        }

        Assert.Contains("data-gallery-ratio=\"1:1\"", File.ReadAllText(Path.Combine(fixtureRoot, "site-product.html")), StringComparison.Ordinal);
        Assert.Contains("data-runtime-owned=\"cart\"", File.ReadAllText(Path.Combine(fixtureRoot, "site-cart.html")), StringComparison.Ordinal);
        Assert.Contains("data-runtime-owned=\"checkout\"", File.ReadAllText(Path.Combine(fixtureRoot, "site-checkout.html")), StringComparison.Ordinal);
        Assert.Contains("data-runtime-owned=\"account\"", File.ReadAllText(Path.Combine(fixtureRoot, "site-account.html")), StringComparison.Ordinal);
        Assert.Contains("data-system-state=\"service-unavailable\"", File.ReadAllText(Path.Combine(fixtureRoot, "site-system-state.html")), StringComparison.Ordinal);
    }

    [Fact]
    public void Phase3CUnsupportedFixtures_CoverExpectedBlockingCodes()
    {
        var fixtureRoot = Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "Phase3C", "Unsupported");
        var expectedBlockers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["direct-api-mutation.html"] = "unsafe-browser-action",
            ["checkout-payment-visual-script.html"] = "unsafe-browser-action",
            ["protected-file-target.html"] = "protected-path-target",
            ["ambiguous-ecommerce-region.html"] = "ambiguous-presentation-mapping",
            ["missing-required-page.html"] = "missing-required-page",
            ["stale-review-decision.html"] = "stale-review-decision"
        };

        foreach (var (fileName, blocker) in expectedBlockers)
        {
            var html = File.ReadAllText(Path.Combine(fixtureRoot, fileName));
            Assert.Contains("data-unsupported-pattern=", html, StringComparison.Ordinal);
            Assert.Contains($"data-expected-blocker=\"{blocker}\"", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Phase3CSchemaRegistry_RegistersFinalHandoffArtifacts()
    {
        var kinds = new VisualSchemaRegistry().Schemas.Select(schema => schema.ArtifactKind).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("storefront-pattern", kinds);
        Assert.Contains("page-contracts", kinds);
        Assert.Contains("behavior-boundaries", kinds);
        Assert.Contains("generation-zones", kinds);
        Assert.Contains("reviewed-page-compositions", kinds);
        Assert.Contains("reviewed-ecommerce-regions", kinds);
        Assert.Contains("agent-handoff-manifest", kinds);
        Assert.Contains("allowed-files", kinds);
        Assert.Contains("protected-files", kinds);
        Assert.Contains("unresolved-regions", kinds);
        Assert.Contains("agent-handoff-readiness", kinds);
        Assert.Contains("unsupported-pattern-decisions", kinds);
    }

    [Fact]
    public void Phase3CGateScript_CoversFinalFixtureAndBoundaryAssertions()
    {
        var repoRoot = GetRepoRoot();
        var script = File.ReadAllText(Path.Combine(repoRoot, "scripts", "qa", "run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1"));

        Assert.Contains("dotnet", script, StringComparison.Ordinal);
        Assert.Contains("build", script, StringComparison.Ordinal);
        Assert.Contains("--blame-hang-timeout", script, StringComparison.Ordinal);
        Assert.Contains("5m", script, StringComparison.Ordinal);
        Assert.Contains("fixture run for complete multi-page handoff", script, StringComparison.Ordinal);
        Assert.Contains("fixture run for unsupported pattern blockers", script, StringComparison.Ordinal);
        Assert.Contains("schema validation for Phase 3C artifacts", script, StringComparison.Ordinal);
        Assert.Contains("analysis/agent-handoff", script, StringComparison.Ordinal);
        Assert.Contains("BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering", script, StringComparison.Ordinal);
        Assert.Contains("captures/home", script, StringComparison.Ordinal);
        Assert.Contains("plan\\.Pages\\.First\\(", script, StringComparison.Ordinal);
        Assert.Contains("Artifact paths:", script, StringComparison.Ordinal);
        Assert.Contains("Next action:", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentHandoffReadinessValidator_ScansSinglePageHardcodeMutations()
    {
        var repoRoot = GetRepoRoot();
        var validator = File.ReadAllText(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Analysis", "Handoff", "AgentHandoffReadinessValidator.cs"));

        Assert.Contains("single-page-hardcode-detected", validator, StringComparison.Ordinal);
        Assert.Contains("plan.Pages.First()", validator, StringComparison.Ordinal);
        Assert.Contains("captures/home", validator, StringComparison.Ordinal);
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
