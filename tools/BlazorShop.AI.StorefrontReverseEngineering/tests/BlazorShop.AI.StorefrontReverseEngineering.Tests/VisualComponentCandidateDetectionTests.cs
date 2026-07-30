using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class VisualComponentCandidateDetectionTests
{
    [Fact]
    public async Task Components_ProductCardsClusterIntoOneFamily()
    {
        var projectRoot = await CreateProjectAsync([
            ProductElement("card-1", ".product-card:nth-child(1)", "Trail Shoe $120"),
            ProductElement("card-2", ".product-card:nth-child(2)", "Runner Shoe $90"),
            ProductElement("card-3", ".product-card:nth-child(3)", "Sandal $60")
        ]);

        var result = await new VisualComponentCandidateDetector(GetRepoRoot()).DetectAsync(projectRoot, CancellationToken.None);

        var card = Assert.Single(result.Candidates.Candidates, candidate => candidate.Family == "product card");
        Assert.Equal(3, card.InstanceIds.Count);
        Assert.True(card.Confidence >= 0.78m);
    }

    [Fact]
    public async Task Components_SimilarVariantsRemainInSameFamily()
    {
        var projectRoot = await CreateProjectAsync([
            ProductElement("card-a", ".product-card.featured", "Featured Shoe $120"),
            ProductElement("card-b", ".product-card.compact", "Compact Shoe $90")
        ]);

        var result = await new VisualComponentCandidateDetector(GetRepoRoot()).DetectAsync(projectRoot, CancellationToken.None);

        var card = Assert.Single(result.Candidates.Candidates, candidate => candidate.Family == "product card");
        Assert.Equal("family-product-card-default", card.VariantId);
        Assert.Equal(2, card.InstanceIds.Count);
    }

    [Fact]
    public async Task Components_DistinctUnrelatedSectionsAreNotMerged()
    {
        var projectRoot = await CreateProjectAsync([
            Element("header", "header.site-header", "semantic-landmark", "Header"),
            Element("footer", "footer.site-footer", "semantic-landmark", "Footer")
        ]);

        var result = await new VisualComponentCandidateDetector(GetRepoRoot()).DetectAsync(projectRoot, CancellationToken.None);

        Assert.Contains(result.Candidates.Candidates, candidate => candidate.Family == "header");
        Assert.Contains(result.Candidates.Candidates, candidate => candidate.Family == "footer");
        Assert.DoesNotContain(result.Candidates.Candidates, candidate => candidate.Family == "header" && candidate.EvidenceIds.Contains("footer"));
    }

    [Fact]
    public async Task Components_SlotDetectionCapturesProductCardSlots()
    {
        var projectRoot = await CreateProjectAsync([
            ProductElement("image", ".product-card .product-image", null),
            ProductElement("title", ".product-card .product-title", "Trail Shoe"),
            ProductElement("price", ".product-card .price", "$120"),
            ProductElement("action", ".product-card button.add-to-cart", "Add to cart")
        ]);

        var result = await new VisualComponentCandidateDetector(GetRepoRoot()).DetectAsync(projectRoot, CancellationToken.None);

        var card = Assert.Single(result.Candidates.Candidates, candidate => candidate.Family == "product card");
        Assert.Contains(card.Slots, slot => slot.SlotName == "image");
        Assert.Contains(card.Slots, slot => slot.SlotName == "title");
        Assert.Contains(card.Slots, slot => slot.SlotName == "price");
        Assert.Contains(card.Slots, slot => slot.SlotName == "action");
    }

    private static EvidenceSnapshotElement ProductElement(string evidenceId, string selector, string? text) =>
        Element(evidenceId, selector, "product-card-candidate", text);

    private static EvidenceSnapshotElement Element(string evidenceId, string selector, string category, string? text) =>
        new(
            evidenceId,
            selector,
            category,
            text,
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["layout"] = new Dictionary<string, string> { ["display"] = "grid" },
                ["typography"] = new Dictionary<string, string> { ["font-size"] = "16px" }
            },
            new ElementBox(0, 100, 240, 320),
            "captures/home/desktop-1440/element-evidence-index.json");

    private static async Task<string> CreateProjectAsync(IReadOnlyList<EvidenceSnapshotElement> elements)
    {
        var repoRoot = GetRepoRoot();
        var root = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "components-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "tokens"));
        var snapshot = new EvidenceSnapshot(
            "1.0",
            "evidence-snapshot",
            "evidence-snapshot-components",
            DateTimeOffset.UtcNow,
            "components",
            "components-run",
            "reports/readiness-report.json",
            ["analysis/evidence-snapshot.json"],
            elements.Select(element => element.EvidenceId).ToArray(),
            [
                new EvidenceSnapshotPage(
                    "home",
                    "https://example.test/",
                    "Home",
                    [
                        new EvidenceSnapshotViewport("desktop-1440", 1440, 900, 1440, 1200, "cap-test", "native", true, elements, Assets: [], SourceArtifactPaths: ["captures/home/desktop-1440/element-evidence-index.json"], Issues: [])
                    ],
                    ["captures/home/capture-manifest.json"])
            ],
            Issues: []);
        var semantic = new SemanticTokenDocument(
            "1.0",
            "semantic-tokens",
            "semantic-tokens-components",
            DateTimeOffset.UtcNow,
            "components",
            "analysis/tokens/raw-design-tokens.json",
            [
                new SemanticToken("text-body", "typography", ["16px"], ["raw-font"], elements.Select(element => element.EvidenceId).ToArray(), 0.6m, ["test"], false)
            ],
            PageLocalOverrides: [],
            ComponentLocalOverrides: [],
            HumanReviewRequired: false,
            ReviewReasons: []);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "evidence-snapshot.json"), JsonSerializer.Serialize(snapshot, VisualJson.Options) + Environment.NewLine);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "tokens", "semantic-tokens.draft.json"), JsonSerializer.Serialize(semantic, VisualJson.Options) + Environment.NewLine);
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
