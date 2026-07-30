using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class ResponsiveInteractionAnalysisTests
{
    [Fact]
    public async Task Responsive_GridToStackAndHideShowAreDetected()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("grid-desktop", ".product-grid", "desktop-1440", "grid", "visible"),
            Element("grid-mobile", ".product-grid", "mobile-390", "block", "visible"),
            Element("promo-desktop", ".promo", "desktop-1440", "block", "visible"),
            Element("promo-mobile", ".promo", "mobile-390", "none", "hidden")
        ]);

        var responsive = (await new ResponsiveInteractionAnalyzer(GetRepoRoot()).AnalyzeAsync(projectRoot, CancellationToken.None))[0].Responsive;

        Assert.Contains(responsive.Sections, section => section.CrossViewportIdentityKey == ".product-grid" && section.BehaviorFlags.Contains("multi-column-to-stacked"));
        Assert.Contains(responsive.Sections, section => section.CrossViewportIdentityKey == ".promo" && section.BehaviorFlags.Contains("hidden-on-mobile"));
    }

    [Fact]
    public async Task Responsive_ReplacementAndRestyleAreSeparateOutputs()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([
            Element("nav-desktop", "nav.primary", "desktop-1440", "flex", "visible"),
            Element("nav-mobile", ".mobile-menu", "mobile-390", "block", "visible")
        ]);

        var responsive = (await new ResponsiveInteractionAnalyzer(GetRepoRoot()).AnalyzeAsync(projectRoot, CancellationToken.None))[0].Responsive;

        Assert.Contains(responsive.Sections, section => section.CrossViewportIdentityKey == "navigation" && section.BehaviorFlags.Contains("desktop-navigation-to-mobile-menu-replacement"));
        Assert.Contains(responsive.Sections, section => section.CrossViewportIdentityKey == "navigation-mobile-menu");
    }

    [Fact]
    public async Task Interaction_BeforeAfterEvidenceIsUsed()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([Element("button", ".cta", "desktop-1440", "block", "visible")], interactionState: "hover-card", interactionModel: InteractionModel.HoverDriven);

        var interaction = (await new ResponsiveInteractionAnalyzer(GetRepoRoot()).AnalyzeAsync(projectRoot, CancellationToken.None))[0].Interaction;

        var pattern = Assert.Single(interaction.Interactions);
        Assert.Equal("visual-only", pattern.Classification);
        Assert.Contains("before-after-interaction-evidence", pattern.ReasonCodes);
        Assert.Equal("interactions/home/hover-card/after.styles.json", pattern.AfterStylesPath);
    }

    [Fact]
    public async Task Interaction_ButtonVisualDoesNotBecomeCartLogic()
    {
        var projectRoot = await CreateProjectWithSnapshotAsync([Element("cart-button", ".add-to-cart", "desktop-1440", "block", "visible")], interactionState: "add-to-cart-hover", interactionModel: InteractionModel.HoverDriven);

        var interaction = (await new ResponsiveInteractionAnalyzer(GetRepoRoot()).AnalyzeAsync(projectRoot, CancellationToken.None))[0].Interaction;

        var pattern = Assert.Single(interaction.Interactions);
        Assert.Equal("business behavior required", pattern.Classification);
        Assert.Contains("business behavior required", pattern.ReasonCodes);
    }

    private static EvidenceSnapshotElement Element(
        string evidenceId,
        string selector,
        string viewportId,
        string display,
        string visibility) =>
        new(
            evidenceId,
            selector,
            "section",
            null,
            new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["layout"] = new Dictionary<string, string> { ["display"] = display, ["gap"] = viewportId == "mobile-390" ? "8px" : "24px" },
                ["positioning"] = new Dictionary<string, string>(),
                ["typography"] = new Dictionary<string, string> { ["font-size"] = viewportId == "mobile-390" ? "14px" : "18px" },
                ["color"] = new Dictionary<string, string> { ["visibility"] = visibility }
            },
            new ElementBox(0, 100, viewportId == "mobile-390" ? 390 : 1200, 240),
            $"captures/home/{viewportId}/element-evidence-index.json");

    private static async Task<string> CreateProjectWithSnapshotAsync(
        IReadOnlyList<EvidenceSnapshotElement> elements,
        string? interactionState = null,
        InteractionModel interactionModel = InteractionModel.Static)
    {
        var repoRoot = GetRepoRoot();
        var root = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "responsive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "analysis"));
        var sourcePaths = new List<string> { "analysis/evidence-snapshot.json" };
        if (interactionState is not null)
        {
            var interactionRoot = Path.Combine(root, "interactions", "home", interactionState);
            Directory.CreateDirectory(interactionRoot);
            var interaction = new InteractionEvidence(
                "1.0",
                "interaction-evidence",
                $"interaction-responsive-home-desktop-1440-{interactionState}",
                DateTimeOffset.UtcNow,
                "responsive",
                "home",
                "desktop-1440",
                interactionState,
                interactionModel,
                $"interactions/home/{interactionState}/before.png",
                $"interactions/home/{interactionState}/after.png",
                $"interactions/home/{interactionState}/before.dom.html",
                $"interactions/home/{interactionState}/after.dom.html",
                $"interactions/home/{interactionState}/before.styles.json",
                $"interactions/home/{interactionState}/after.styles.json",
                DomChanged: false,
                StyleChanged: true,
                ScreenshotChanged: true,
                ScreenshotDiffHash: "ABC123",
                ChangedElementEvidenceIds: [elements[0].EvidenceId],
                DomDiffSummary: "DOM content did not change after interaction.",
                StyleDiffSummary: "Computed style evidence changed after interaction.",
                Warnings: [],
                Errors: []);
            await File.WriteAllTextAsync(Path.Combine(interactionRoot, "interaction-evidence.json"), JsonSerializer.Serialize(interaction, VisualJson.Options) + Environment.NewLine);
            sourcePaths.Add($"interactions/home/{interactionState}/interaction-evidence.json");
        }

        var desktop = elements.Where(element => element.SourceArtifactPath.Contains("desktop-1440", StringComparison.Ordinal)).ToArray();
        var mobile = elements.Where(element => element.SourceArtifactPath.Contains("mobile-390", StringComparison.Ordinal)).ToArray();
        var snapshot = new EvidenceSnapshot(
            "1.0",
            "evidence-snapshot",
            "evidence-snapshot-responsive",
            DateTimeOffset.UtcNow,
            "responsive",
            "responsive-run",
            "reports/readiness-report.json",
            sourcePaths,
            elements.Select(element => element.EvidenceId).ToArray(),
            [
                new EvidenceSnapshotPage(
                    "home",
                    "https://example.test/",
                    "Home",
                    [
                        new EvidenceSnapshotViewport("desktop-1440", 1440, 900, 1440, 1200, "cap-desktop", "native", true, desktop, Assets: [], SourceArtifactPaths: ["captures/home/desktop-1440/element-evidence-index.json"], Issues: []),
                        new EvidenceSnapshotViewport("mobile-390", 390, 844, 390, 1400, "cap-mobile", "native", true, mobile, Assets: [], SourceArtifactPaths: ["captures/home/mobile-390/element-evidence-index.json"], Issues: [])
                    ],
                    ["captures/home/capture-manifest.json"])
            ],
            Issues: []);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "evidence-snapshot.json"), JsonSerializer.Serialize(snapshot, VisualJson.Options) + Environment.NewLine);
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
