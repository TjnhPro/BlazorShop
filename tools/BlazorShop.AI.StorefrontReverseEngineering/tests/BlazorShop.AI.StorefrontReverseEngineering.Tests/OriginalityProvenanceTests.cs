using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class OriginalityProvenanceTests
{
    [Fact]
    public async Task Originality_FlagsFakeBrandAsset()
    {
        var report = await AuditAsync();

        Assert.Contains(report.ReferenceOnlyAssets, asset => asset.LikelyBrandAsset);
        Assert.Contains(report.Warnings, warning => warning.Code == "likely-brand-asset");
        Assert.Contains(report.GenerationRestrictions, restriction => restriction.Code == "avoid-distinctive-brand-expression");
    }

    [Fact]
    public async Task Provenance_AssetsDefaultToReferenceOnly()
    {
        var report = await AuditAsync();

        Assert.All(report.ReferenceOnlyAssets, asset => Assert.Contains("reference-only", asset.Reason, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.GenerationRestrictions, restriction => restriction.Code == "reference-assets-not-reusable");
    }

    [Fact]
    public async Task Originality_WritesMarkdownReport()
    {
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "originality-md-" + Guid.NewGuid().ToString("N"));
        await AuditAsync(projectRoot);

        Assert.True(File.Exists(Path.Combine(repoRoot, projectRoot, "reports", "originality-audit.md")));
    }

    private static async Task<OriginalityAuditReport> AuditAsync(string? projectRoot = null)
    {
        var repoRoot = GetRepoRoot();
        projectRoot ??= Path.Combine("obj", "storefront-reverse-engineering", "projects", "originality-test-" + Guid.NewGuid().ToString("N"));
        var assets = new AssetInventoryEvidence(
            "1.0",
            "asset-inventory",
            "asset-inventory-originality-home-desktop",
            DateTimeOffset.UtcNow,
            "originality",
            "home",
            "desktop-1440",
            null,
            [
                new("asset-001", "/assets/fake-brand-hero.svg", "image", null, null, "img", true),
                new("asset-002", "https://cdn.example.test/product.jpg", "image", 640, 640, "img", true)
            ]);
        var elements = new ElementEvidenceIndex(
            "1.0",
            "computed-style-evidence",
            "element-evidence-originality-home-desktop",
            DateTimeOffset.UtcNow,
            "originality",
            "home",
            "desktop-1440",
            null,
            [
                new("ev-001", "h1", "heading", "Long source copy block that should be reviewed before use", new Dictionary<string, IReadOnlyDictionary<string, string>>(), null)
            ]);

        return await new OriginalityAuditService(repoRoot)
            .WriteAuditAsync(projectRoot, "originality", "home", assets, elements, new OriginalityPolicy(), CancellationToken.None);
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
