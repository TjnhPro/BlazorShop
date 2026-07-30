using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class SemanticTokenNormalizationTests
{
    [Fact]
    public async Task SemanticTokens_AssignsStableRolesFromFixtureTokens()
    {
        var projectRoot = await CreateReadyProjectAsync("Semantic Tokens Fixture");

        var semantic = await ReadSemanticTokensAsync(projectRoot);

        Assert.Contains(semantic.Tokens, token => token.Role == "surface-page" && token.Group == "color");
        Assert.Contains(semantic.Tokens, token => token.Role == "text-primary" && token.Group == "color");
        Assert.Contains(semantic.Tokens, token => token.Role == "font-body" && token.Group == "typography");
        Assert.Contains(semantic.Tokens, token => token.Role == "radius-small" && token.Group == "shape");
        Assert.Contains(semantic.Tokens, token => token.Role == "shadow-card" && token.Group == "shape");
    }

    [Fact]
    public async Task SemanticTokens_AmbiguousAccentRolesCreateConflictReport()
    {
        var projectRoot = await CreateReadyProjectAsync("Semantic Tokens Accent Conflict");
        var raw = await ReadRawTokensAsync(projectRoot);
        await WriteRawTokensAsync(projectRoot, raw with
        {
            Tokens = raw.Tokens.Concat([
                CreateRawColor("raw-color-accent-a", "#ff3366", "accent-a"),
                CreateRawColor("raw-color-accent-b", "#3366ff", "accent-b")
            ]).ToArray()
        });

        var semantic = await new SemanticTokenNormalizer(GetRepoRoot())
            .NormalizeAsync(projectRoot, CancellationToken.None);
        var conflicts = await ReadConflictsAsync(projectRoot);

        Assert.True(semantic.HumanReviewRequired);
        Assert.Contains(conflicts.Conflicts, conflict => conflict.Role == "accent-primary" && conflict.HumanReviewRequired);
    }

    [Fact]
    public async Task SemanticTokens_HumanReviewFlagAppearsForLowConfidenceCriticalToken()
    {
        var projectRoot = await CreateReadyProjectAsync("Semantic Tokens Review");
        var raw = await ReadRawTokensAsync(projectRoot);
        await WriteRawTokensAsync(projectRoot, raw with
        {
            Tokens = raw.Tokens.Concat([
                CreateRawColor("raw-color-review-a", "#ed1c24", "review-a"),
                CreateRawColor("raw-color-review-b", "#1c7fed", "review-b")
            ]).ToArray()
        });

        var semantic = await new SemanticTokenNormalizer(GetRepoRoot())
            .NormalizeAsync(projectRoot, CancellationToken.None);

        var accent = Assert.Single(semantic.Tokens, token => token.Role == "accent-primary");
        Assert.True(accent.HumanReviewRequired);
        Assert.True(semantic.HumanReviewRequired);
        Assert.Contains(semantic.ReviewReasons, reason => reason.StartsWith("accent-primary:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SemanticTokens_RawValuesRemainTraceable()
    {
        var projectRoot = await CreateReadyProjectAsync("Semantic Tokens Traceability");

        var semantic = await ReadSemanticTokensAsync(projectRoot);

        Assert.All(semantic.Tokens, token =>
        {
            Assert.NotEmpty(token.RawTokenIds);
            Assert.NotEmpty(token.EvidenceIds);
        });
        Assert.Equal("analysis/tokens/raw-design-tokens.json", semantic.SourceRawTokensPath);
    }

    private static RawDesignToken CreateRawColor(string tokenId, string value, string evidenceId) =>
        new(
            tokenId,
            "color",
            "background-color",
            value,
            [value],
            ProjectFrequency: 2,
            PageFrequencies: [new TokenFrequency("home", 2)],
            ViewportFrequencies: [new TokenFrequency("desktop-1440", 2)],
            SourceEvidenceIds: [evidenceId],
            SourceArtifactPaths: ["analysis/evidence-snapshot.json"],
            Outlier: false,
            NearDuplicateClusterId: null,
            Hints: ["accent-like"]);

    private static async Task<RawDesignTokenDocument> ReadRawTokensAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "tokens", "raw-design-tokens.json"));
        return JsonSerializer.Deserialize<RawDesignTokenDocument>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Raw token artifact did not deserialize.");
    }

    private static async Task WriteRawTokensAsync(string projectRoot, RawDesignTokenDocument raw)
    {
        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "analysis", "tokens", "raw-design-tokens.json"),
            JsonSerializer.Serialize(raw, VisualJson.Options) + Environment.NewLine);
    }

    private static async Task<SemanticTokenDocument> ReadSemanticTokensAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "tokens", "semantic-tokens.draft.json"));
        return JsonSerializer.Deserialize<SemanticTokenDocument>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Semantic token artifact did not deserialize.");
    }

    private static async Task<SemanticTokenConflictReport> ReadConflictsAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "tokens", "token-conflicts.json"));
        return JsonSerializer.Deserialize<SemanticTokenConflictReport>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Semantic token conflict artifact did not deserialize.");
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "semantic-token-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "semantic-token-fixture");

        Assert.True(summary.ReadinessPassed);
        Assert.True(File.Exists(Path.Combine(summary.ArtifactRoot, "analysis", "tokens", "semantic-tokens.draft.json")));
        Assert.True(File.Exists(Path.Combine(summary.ArtifactRoot, "analysis", "tokens", "token-conflicts.json")));
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
