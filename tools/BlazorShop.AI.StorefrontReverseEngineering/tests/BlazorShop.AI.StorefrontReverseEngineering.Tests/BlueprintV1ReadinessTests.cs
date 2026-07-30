using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class BlueprintV1ReadinessTests
{
    [Fact]
    public async Task BlueprintV1_AssemblesDraftReviewedAndReadinessArtifacts()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint V1 Artifacts");

        var blueprint = await ReadBlueprintAsync(projectRoot, "analysis/visual-blueprint.v1.draft.json");

        Assert.Contains("analysis/evidence-snapshot.json", blueprint.SourceProvenance);
        Assert.NotEmpty(blueprint.Pages);
        Assert.Equal("analysis/tokens/semantic-tokens.draft.json", blueprint.Tokens);
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "visual-blueprint.v1.reviewed.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "reports", "generation-readiness.md")));
    }

    [Fact]
    public async Task GenerationReadiness_MissingSemanticTokenBaselineBlocks()
    {
        var projectRoot = await CreateReadyProjectAsync("Blueprint Missing Tokens");
        File.Delete(Path.Combine(projectRoot, "analysis", "tokens", "semantic-tokens.draft.json"));

        var result = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Readiness.Passed);
        Assert.Contains(result.Readiness.Findings, finding => finding.Code == "missing-required-artifact" && finding.ArtifactPath == "analysis/tokens/semantic-tokens.draft.json");
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "blueprint-v1-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "blueprint-v1-fixture");

        Assert.True(summary.ReadinessPassed);
        return summary.ArtifactRoot;
    }

    private static async Task<VisualBlueprintV1> ReadBlueprintAsync(string projectRoot, string relativePath)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Deserialize<VisualBlueprintV1>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Blueprint artifact did not deserialize.");
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
