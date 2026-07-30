using BlazorShop.AI.StorefrontReverseEngineering.Analysis;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class AnalysisBlueprintTests
{
    [Fact]
    public async Task Analysis_RuleBasedProvider_WorksWithoutAiSecrets()
    {
        var result = await new RuleBasedVisualAnalysisProvider().AnalyzeAsync(
            new AnalysisContext("analysis", "home", BuildEvidence(), null),
            CancellationToken.None);

        Assert.NotEmpty(result.PageTopology.Sections);
        Assert.NotEmpty(result.VisualBlueprint.EvidenceIds);
        Assert.Null(result.AiInferenceLog);
        Assert.Contains("Do not generate Razor/CSS in Phase 3A.", result.VisualBlueprint.GenerationRestrictions);
    }

    [Fact]
    public async Task Blueprint_ReferencesEvidenceIds()
    {
        var result = await new RuleBasedVisualAnalysisProvider().AnalyzeAsync(
            new AnalysisContext("analysis", "home", BuildEvidence(), null),
            CancellationToken.None);

        Assert.All(result.ComponentSpecifications, component => Assert.NotEmpty(component.EvidenceIds));
        Assert.All(result.PageTopology.Sections, section => Assert.All(section.EvidenceIds, evidenceId => Assert.StartsWith("ev-", evidenceId, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Analysis_AiProviderSettingsAreOptional()
    {
        var result = await new RuleBasedVisualAnalysisProvider().AnalyzeAsync(
            new AnalysisContext("analysis", "home", BuildEvidence(), null, AiEnabled: true),
            CancellationToken.None);

        Assert.Null(result.AiInferenceLog);
        Assert.Contains(result.PageTopology.UnsupportedPatternWarnings, warning => warning.Contains("rule-based fallback", StringComparison.OrdinalIgnoreCase));
    }

    private static ElementEvidenceIndex BuildEvidence() =>
        new(
            "1.0",
            "computed-style-evidence",
            "element-evidence-analysis-home-desktop",
            DateTimeOffset.UtcNow,
            "analysis",
            "home",
            "desktop-1440",
            null,
            [
                new("ev-001", "header", "semantic-landmark", null, new Dictionary<string, IReadOnlyDictionary<string, string>>(), null),
                new("ev-002", "section.hero", "section", "Hero", new Dictionary<string, IReadOnlyDictionary<string, string>>(), null),
                new("ev-003", ".product-card", "product-card-candidate", "Product", new Dictionary<string, IReadOnlyDictionary<string, string>>(), null)
            ]);
}
