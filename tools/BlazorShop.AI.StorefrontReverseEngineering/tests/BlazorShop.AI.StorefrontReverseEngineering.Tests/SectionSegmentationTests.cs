using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class SectionSegmentationTests
{
    [Fact]
    public async Task Sections_AreOrderedByPageFlow()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("footer", "footer", "semantic-landmark", 900, 200),
            Element("hero", "section.hero", "section", 100, 300),
            Element("header", "header.site-header", "semantic-landmark", 0, 80)
        ]);

        var document = Assert.Single(await new SectionSegmenter(GetRepoRoot()).SegmentAsync(projectRoot, CancellationToken.None));

        Assert.Equal(["header", "hero", "footer"], document.Sections.Select(section => section.SectionType).ToArray());
        Assert.Equal([1, 2, 3], document.Sections.Select(section => section.Order).ToArray());
    }

    [Fact]
    public async Task Sections_PeerSectionsDoNotOverlapIllegally()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("header", "header", "semantic-landmark", 0, 80),
            Element("hero", "section.hero", "section", 100, 300),
            Element("newsletter", "section.newsletter", "section", 450, 120)
        ]);

        var document = Assert.Single(await new SectionSegmenter(GetRepoRoot()).SegmentAsync(projectRoot, CancellationToken.None));

        Assert.DoesNotContain(document.Issues, issue => issue.Code == "invalid-peer-overlap");
    }

    [Fact]
    public async Task Sections_NestedHeaderControlsDoNotBecomePeerSections()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("header", "header.site-header", "semantic-landmark", 80, 66, width: 1440),
            Element("nav", "nav.menu-list", "semantic-landmark", 92, 44, x: 180, width: 500),
            Element("header-menu", "header-menu.header-menu", "semantic-landmark", 93, 44, x: 180, width: 494),
            Element("header-component", "header-component > svg", "semantic-landmark", 104, 22, x: 1281, width: 22),
            Element("link", "a.menu-list__link", "link", 92, 44, x: 180, width: 72),
            Element("icon", "button.header-actions__action", "button", 92, 44, x: 1270, width: 44),
            Element("hero", "section.hero", "section", 148, 800, width: 1440)
        ]);

        var document = Assert.Single(await new SectionSegmenter(GetRepoRoot()).SegmentAsync(projectRoot, CancellationToken.None));

        Assert.Contains(document.Sections, section => section.SectionType == "header" && section.EvidenceIds.Contains("header"));
        Assert.DoesNotContain(document.Sections, section => section.EvidenceIds.Contains("nav"));
        Assert.DoesNotContain(document.Sections, section => section.EvidenceIds.Contains("header-menu"));
        Assert.DoesNotContain(document.Sections, section => section.EvidenceIds.Contains("header-component"));
        Assert.DoesNotContain(document.Sections, section => section.EvidenceIds.Contains("link"));
        Assert.DoesNotContain(document.Sections, section => section.EvidenceIds.Contains("icon"));
        Assert.DoesNotContain(document.Issues, issue => issue.Code == "invalid-peer-overlap");
    }

    [Fact]
    public async Task Sections_DuplicateHeroWrappersCollapseToSingleSection()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("hero-section", "#shopify-section-template--hero", "element", 148, 800, width: 1440),
            Element("hero-root", "#Hero-template--hero", "element", 148, 800, width: 1440),
            Element("hero-media", "[data-testid=\"hero-media-wrapper\"]", "element", 148, 800, width: 1440),
            Element("products", "section.featured-products", "section", 980, 420, width: 1440)
        ]);

        var document = Assert.Single(await new SectionSegmenter(GetRepoRoot()).SegmentAsync(projectRoot, CancellationToken.None));

        var hero = Assert.Single(document.Sections, section => section.SectionType == "hero");
        Assert.Equal(["hero-section"], hero.EvidenceIds);
        Assert.DoesNotContain(document.Sections, section => section.EvidenceIds.Contains("hero-root"));
        Assert.DoesNotContain(document.Sections, section => section.EvidenceIds.Contains("hero-media"));
        Assert.DoesNotContain(document.Issues, issue => issue.Code == "invalid-peer-overlap");
    }

    [Fact]
    public async Task Sections_RepeatedProductCardsBecomeProductGrid()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("card-1", ".product-card:nth-child(1)", "product-card-candidate", 200, 240, x: 0, width: 250),
            Element("card-2", ".product-card:nth-child(2)", "product-card-candidate", 200, 240, x: 270, width: 250),
            Element("card-3", ".product-card:nth-child(3)", "product-card-candidate", 200, 240, x: 540, width: 250)
        ]);

        var document = Assert.Single(await new SectionSegmenter(GetRepoRoot()).SegmentAsync(projectRoot, CancellationToken.None));

        var grid = Assert.Single(document.Sections, section => section.SectionType == "product grid");
        Assert.Equal(3, grid.EvidenceIds.Count);
        Assert.Contains("repeated-card-group", grid.ReasonCodes);
    }

    [Fact]
    public async Task Sections_UnknownSectionIsEmittedForUnsupportedContent()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("custom", ".immersive-object", "section", 100, 200)
        ]);

        var document = Assert.Single(await new SectionSegmenter(GetRepoRoot()).SegmentAsync(projectRoot, CancellationToken.None));

        var section = Assert.Single(document.Sections);
        Assert.Equal("unknown section", section.SectionType);
        Assert.Contains("unsupported-section-signal", section.ReasonCodes);
    }

    [Fact]
    public async Task Sections_HumanReviewIssueIsCreatedForMergeSplitAmbiguity()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("hero", "section.hero", "section", 100, 200),
            Element("promo", "section.promo", "section", 295, 120)
        ]);

        var document = Assert.Single(await new SectionSegmenter(GetRepoRoot()).SegmentAsync(projectRoot, CancellationToken.None));

        Assert.Contains(document.Issues, issue => issue.Code == "merge-split-ambiguity" && issue.Severity == "warning");
    }

    private static EvidenceSnapshotElement Element(
        string evidenceId,
        string selector,
        string category,
        decimal y,
        decimal height,
        decimal x = 0,
        decimal width = 1200) =>
        new(
            evidenceId,
            selector,
            category,
            null,
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["layout"] = new Dictionary<string, string> { ["display"] = selector.Contains("hero", StringComparison.Ordinal) ? "grid" : "block" },
                ["color"] = new Dictionary<string, string> { ["background-color"] = "#ffffff" },
                ["positioning"] = new Dictionary<string, string>()
            },
            new ElementBox(x, y, width, height),
            "captures/home/desktop-1440/element-evidence-index.json");

    private static async Task<string> CreateProjectWithSnapshotAsync(IReadOnlyList<EvidenceSnapshotElement> elements)
    {
        var repoRoot = GetRepoRoot();
        var root = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "sections-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "analysis"));
        var snapshot = new EvidenceSnapshot(
            "1.0",
            "evidence-snapshot",
            "evidence-snapshot-sections",
            DateTimeOffset.UtcNow,
            "sections",
            "sections-run",
            "reports/readiness-report.json",
            ["analysis/evidence-snapshot.json"],
            elements.Select(element => element.EvidenceId).ToArray(),
            [
                new EvidenceSnapshotPage(
                    "home",
                    "https://example.test/",
                    "Home",
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
