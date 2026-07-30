using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class PageArchetypeClassificationTests
{
    [Fact]
    public async Task PageArchetype_HomeFixtureClassifiesAsHome()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "archetype-home-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, "Archetype Home", outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "archetype-home-fixture");

        var archetype = await ReadArchetypeAsync(summary.ArtifactRoot, "home");

        Assert.Equal("home", archetype.PrimaryArchetype);
        Assert.True(archetype.Confidence >= 0.50m);
        Assert.Contains("home-route-signal", archetype.ReasonCodes);
    }

    [Fact]
    public async Task PageArchetype_PlpSnapshotClassifiesAsProductListing()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync(
            "plp",
            "https://example.test/collections/shoes",
            [
                Element("card-1", ".product-card:nth-child(1)", "product-card-candidate", "Trail shoe $120"),
                Element("card-2", ".product-card:nth-child(2)", "product-card-candidate", "Runner shoe $90"),
                Element("card-3", ".product-card:nth-child(3)", "product-card-candidate", "Sandal $60")
            ]);

        var result = await new PageArchetypeClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None);

        var archetype = Assert.Single(result);
        Assert.Equal("product-listing", archetype.PrimaryArchetype);
        Assert.Contains("listing-route-signal", archetype.ReasonCodes);
        Assert.Contains("repeated-product-card-signals", archetype.ReasonCodes);
    }

    [Fact]
    public async Task PageArchetype_PdpSnapshotClassifiesAsProductDetail()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync(
            "pdp",
            "https://example.test/product/trail-shoe",
            [
                Element("gallery", ".product-gallery", "section", "Gallery"),
                Element("title", "h1.product-title", "heading", "Trail Shoe"),
                Element("price", ".price", "section", "$120"),
                Element("cart", "button.add-to-cart", "section", "Add to cart")
            ]);

        var result = await new PageArchetypeClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None);

        var archetype = Assert.Single(result);
        Assert.Equal("product-detail", archetype.PrimaryArchetype);
        Assert.Contains("product-detail-route-signal", archetype.ReasonCodes);
        Assert.Contains("gallery-price-cart-signals", archetype.ReasonCodes);
    }

    [Fact]
    public async Task PageArchetype_UnsupportedSnapshotClassifiesAsUnknown()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync(
            "custom",
            "https://example.test/brand-story",
            [Element("custom-1", ".immersive", "section", "A custom visual essay")]);

        var result = await new PageArchetypeClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None);

        var archetype = Assert.Single(result);
        Assert.Equal("unknown", archetype.PrimaryArchetype);
        Assert.Contains("below-confidence-threshold", archetype.ReasonCodes);
    }

    [Fact]
    public async Task PageArchetype_LowConfidenceDoesNotForceContent()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync(
            "low",
            "https://example.test/story",
            [Element("article-1", "article", "article", "Editorial story")]);

        var result = await new PageArchetypeClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None);

        var archetype = Assert.Single(result);
        Assert.Equal("unknown", archetype.PrimaryArchetype);
        Assert.Contains(archetype.Alternatives, alternative => alternative.Archetype == "content" && alternative.Confidence == 0.45m);
    }

    private static EvidenceSnapshotElement Element(string evidenceId, string selector, string category, string text) =>
        new(
            evidenceId,
            selector,
            category,
            text,
            new Dictionary<string, IReadOnlyDictionary<string, string>>(),
            Box: null,
            SourceArtifactPath: "captures/home/desktop-1440/element-evidence-index.json");

    private static async Task<string> CreateProjectWithSnapshotAsync(
        string pageId,
        string url,
        IReadOnlyList<EvidenceSnapshotElement> elements)
    {
        var repoRoot = GetRepoRoot();
        var root = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "archetype-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "analysis"));
        var snapshot = new EvidenceSnapshot(
            "1.0",
            "evidence-snapshot",
            "evidence-snapshot-archetype",
            DateTimeOffset.UtcNow,
            "archetype",
            "archetype-run",
            "reports/readiness-report.json",
            ["analysis/evidence-snapshot.json"],
            elements.Select(element => element.EvidenceId).ToArray(),
            [
                new EvidenceSnapshotPage(
                    pageId,
                    url,
                    pageId,
                    [
                        new EvidenceSnapshotViewport(
                            "desktop-1440",
                            1440,
                            900,
                            1440,
                            1200,
                            "cap-test",
                            "native",
                            QualityPassed: true,
                            elements,
                            Assets: [],
                            SourceArtifactPaths: ["captures/home/desktop-1440/element-evidence-index.json"],
                            Issues: [])
                    ],
                    ["captures/home/capture-manifest.json"])
            ],
            Issues: []);
        await File.WriteAllTextAsync(
            Path.Combine(root, "analysis", "evidence-snapshot.json"),
            JsonSerializer.Serialize(snapshot, VisualJson.Options) + Environment.NewLine);
        return root;
    }

    private static async Task<PageArchetypeDocument> ReadArchetypeAsync(string projectRoot, string pageId)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "pages", pageId, "page-archetype.json"));
        return JsonSerializer.Deserialize<PageArchetypeDocument>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Page archetype artifact did not deserialize.");
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
