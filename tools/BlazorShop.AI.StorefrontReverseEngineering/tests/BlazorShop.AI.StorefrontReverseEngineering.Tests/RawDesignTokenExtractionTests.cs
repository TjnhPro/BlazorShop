using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class RawDesignTokenExtractionTests
{
    [Fact]
    public async Task RawTokens_ExtractColorsTypographyAndSpacingFromFixture()
    {
        var projectRoot = await CreateReadyProjectAsync("Raw Tokens Fixture");

        var tokens = await ReadTokensAsync(projectRoot);

        Assert.Contains(tokens.Tokens, token => token.Group == "color" && token.PropertyName == "background-color" && token.NormalizedValue == "#ffffff");
        Assert.Contains(tokens.Tokens, token => token.Group == "typography" && token.PropertyName == "font-family" && token.LiteralValues.Contains("Inter"));
        Assert.Contains(tokens.Tokens, token => token.Group == "shape" && token.PropertyName == "border-radius" && token.LiteralValues.Contains("8px"));
        Assert.Contains(tokens.Tokens, token => token.Group == "layout" && token.PropertyName == "display" && token.LiteralValues.Contains("grid"));
        Assert.Contains(tokens.Tokens, token => token.Group == "layout" && token.PropertyName == "aspect-ratio");
        Assert.All(tokens.Tokens, token => Assert.NotEmpty(token.SourceEvidenceIds));
    }

    [Fact]
    public async Task RawTokens_SpacingValuesAreCountedByProjectPageAndViewport()
    {
        var projectRoot = await CreateReadyProjectAsync("Raw Tokens Spacing");
        var snapshot = await ReadSnapshotAsync(projectRoot);
        var updated = AddElement(snapshot, "space-1", new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["layout"] = new Dictionary<string, string>
            {
                ["gap"] = "24px",
                ["padding"] = "32px"
            }
        });
        await WriteSnapshotAsync(projectRoot, updated);

        var tokens = await new RawDesignTokenExtractor(GetRepoRoot())
            .ExtractAsync(projectRoot, CancellationToken.None);

        var gap = Assert.Single(tokens.Tokens, token => token.Group == "spacing" && token.PropertyName == "gap" && token.NormalizedValue == "24px");
        Assert.True(gap.ProjectFrequency >= 1);
        Assert.Contains(gap.PageFrequencies, frequency => frequency.ScopeId == "home" && frequency.Count >= 1);
        Assert.Contains(gap.ViewportFrequencies, frequency => frequency.ScopeId == "desktop-1440" && frequency.Count >= 1);
    }

    [Fact]
    public async Task RawTokens_OutliersAreReportedWithoutBeingMerged()
    {
        var projectRoot = await CreateReadyProjectAsync("Raw Tokens Outliers");
        var snapshot = await ReadSnapshotAsync(projectRoot);
        var updated = snapshot;
        foreach (var value in new[] { "8px", "12px", "16px", "999px" })
        {
            updated = AddElement(updated, "radius-" + value.Replace("px", "", StringComparison.Ordinal), new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["borderShadow"] = new Dictionary<string, string>
                {
                    ["border-radius"] = value
                }
            });
        }
        await WriteSnapshotAsync(projectRoot, updated);

        var tokens = await new RawDesignTokenExtractor(GetRepoRoot())
            .ExtractAsync(projectRoot, CancellationToken.None);

        Assert.Contains(tokens.Tokens, token => token.PropertyName == "border-radius" && token.NormalizedValue == "999px" && token.Outlier);
        Assert.Contains(tokens.Tokens, token => token.PropertyName == "border-radius" && token.NormalizedValue == "8px");
        Assert.Contains(tokens.Tokens, token => token.PropertyName == "border-radius" && token.NormalizedValue == "12px");
    }

    [Fact]
    public async Task RawTokens_HiddenAndNoiseElementsAreIgnored()
    {
        var projectRoot = await CreateReadyProjectAsync("Raw Tokens Noise");
        var snapshot = await ReadSnapshotAsync(projectRoot);
        var withHidden = AddElement(snapshot, "hidden-1", new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["layout"] = new Dictionary<string, string>
            {
                ["display"] = "none",
                ["gap"] = "777px"
            }
        });
        var withNoise = AddElement(withHidden, "noise-1", new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["layout"] = new Dictionary<string, string>
            {
                ["gap"] = "778px"
            }
        }, selector: ".cookie-banner");
        await WriteSnapshotAsync(projectRoot, withNoise);

        var tokens = await new RawDesignTokenExtractor(GetRepoRoot())
            .ExtractAsync(projectRoot, CancellationToken.None);

        Assert.DoesNotContain(tokens.Tokens, token => token.NormalizedValue is "777px" or "778px");
        Assert.Contains(tokens.Issues, issue => issue.Code == "ignored-noise-or-hidden-element" && issue.Severity == "info");
    }

    [Fact]
    public async Task RawTokens_PreservesLiteralValuesAndNearDuplicateClusters()
    {
        var projectRoot = await CreateReadyProjectAsync("Raw Tokens Literal");
        var snapshot = await ReadSnapshotAsync(projectRoot);
        var updated = AddElement(snapshot, "literal-1", new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["typography"] = new Dictionary<string, string>
            {
                ["font-size"] = "16px"
            }
        });
        updated = AddElement(updated, "literal-2", new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["typography"] = new Dictionary<string, string>
            {
                ["font-size"] = "16.5px"
            }
        });
        await WriteSnapshotAsync(projectRoot, updated);

        var tokens = await new RawDesignTokenExtractor(GetRepoRoot())
            .ExtractAsync(projectRoot, CancellationToken.None);

        var token16 = Assert.Single(tokens.Tokens, token => token.Group == "typography" && token.PropertyName == "font-size" && token.NormalizedValue == "16px");
        var token165 = Assert.Single(tokens.Tokens, token => token.Group == "typography" && token.PropertyName == "font-size" && token.NormalizedValue == "16.5px");
        Assert.Contains("16px", token16.LiteralValues);
        Assert.Contains("16.5px", token165.LiteralValues);
        Assert.NotNull(token16.NearDuplicateClusterId);
        Assert.Equal(token16.NearDuplicateClusterId, token165.NearDuplicateClusterId);
    }

    [Fact]
    public async Task RawTokens_ExtractsInteractionColorsOnlyFromChangedEvidence()
    {
        var projectRoot = await CreateReadyProjectAsync("Raw Tokens Interaction");
        var interactionRoot = Path.Combine(projectRoot, "interactions", "home", "hover");
        Directory.CreateDirectory(interactionRoot);
        var interaction = new InteractionEvidence(
            "1.0",
            "interaction-evidence",
            "interaction-raw-tokens-interaction-home-desktop-1440-hover",
            DateTimeOffset.UtcNow,
            "raw-tokens-interaction",
            "home",
            "desktop-1440",
            "hover",
            InteractionModel.HoverDriven,
            "interactions/home/hover/before.png",
            "interactions/home/hover/after.png",
            "interactions/home/hover/before.dom.html",
            "interactions/home/hover/after.dom.html",
            "interactions/home/hover/before.styles.json",
            "interactions/home/hover/after.styles.json",
            DomChanged: false,
            StyleChanged: true,
            ScreenshotChanged: true,
            ScreenshotDiffHash: "ABC123",
            ChangedElementEvidenceIds: ["changed-hover"],
            DomDiffSummary: "DOM content did not change after interaction.",
            StyleDiffSummary: "Computed style evidence changed after interaction.",
            Warnings: [],
            Errors: []);
        await File.WriteAllTextAsync(
            Path.Combine(interactionRoot, "interaction-evidence.json"),
            JsonSerializer.Serialize(interaction, VisualJson.Options) + Environment.NewLine);
        await File.WriteAllTextAsync(
            Path.Combine(interactionRoot, "after.styles.json"),
            JsonSerializer.Serialize<IReadOnlyList<ComputedStyleSample>>(
                [
                    new ComputedStyleSample(".cta:hover", new Dictionary<string, string> { ["background-color"] = "#FF3366" }, "changed-hover"),
                    new ComputedStyleSample(".cta", new Dictionary<string, string> { ["background-color"] = "#00FF00" }, "unchanged")
                ],
                VisualJson.Options) + Environment.NewLine);
        await new EvidenceSnapshotAggregator(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);

        var tokens = await new RawDesignTokenExtractor(GetRepoRoot())
            .ExtractAsync(projectRoot, CancellationToken.None);

        Assert.Contains(tokens.Tokens, token =>
            token.Group == "color" &&
            token.NormalizedValue == "#ff3366" &&
            token.Hints.Contains("interaction-proven"));
        Assert.DoesNotContain(tokens.Tokens, token => token.NormalizedValue == "#00ff00");
    }

    private static EvidenceSnapshot AddElement(
        EvidenceSnapshot snapshot,
        string evidenceId,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> styleGroups,
        string selector = ".token-test")
    {
        var page = snapshot.Pages[0];
        var viewport = page.Viewports[0];
        var element = new EvidenceSnapshotElement(
            evidenceId,
            selector,
            "section",
            "Token test",
            styleGroups,
            Box: null,
            SourceArtifactPath: "captures/home/desktop-1440/element-evidence-index.json");
        var updatedViewport = viewport with { Elements = viewport.Elements.Concat([element]).ToArray() };
        var updatedPage = page with { Viewports = page.Viewports.Select(candidate => candidate.ViewportId == viewport.ViewportId ? updatedViewport : candidate).ToArray() };
        return snapshot with { Pages = snapshot.Pages.Select(candidate => candidate.PageId == page.PageId ? updatedPage : candidate).ToArray() };
    }

    private static async Task<RawDesignTokenDocument> ReadTokensAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "tokens", "raw-design-tokens.json"));
        return JsonSerializer.Deserialize<RawDesignTokenDocument>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Raw token artifact did not deserialize.");
    }

    private static async Task<EvidenceSnapshot> ReadSnapshotAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "evidence-snapshot.json"));
        return JsonSerializer.Deserialize<EvidenceSnapshot>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Evidence snapshot did not deserialize.");
    }

    private static async Task WriteSnapshotAsync(string projectRoot, EvidenceSnapshot snapshot)
    {
        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "analysis", "evidence-snapshot.json"),
            JsonSerializer.Serialize(snapshot, VisualJson.Options) + Environment.NewLine);
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "raw-token-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "raw-token-fixture");

        Assert.True(summary.ReadinessPassed);
        Assert.True(File.Exists(Path.Combine(summary.ArtifactRoot, "analysis", "tokens", "raw-design-tokens.json")));
        Assert.True(File.Exists(Path.Combine(summary.ArtifactRoot, "analysis", "tokens", "token-frequency-report.json")));
        return summary.ArtifactRoot;
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
