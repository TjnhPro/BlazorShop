using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3BPreflightTests
{
    [Fact]
    public async Task Preflight_ReadyPhase3AProjectPasses()
    {
        var projectRoot = await CreateReadyProjectAsync("Phase3B Ready");

        var result = await new Phase3BPreflightService(GetRepoRoot())
            .CheckAsync(projectRoot, CancellationToken.None);

        Assert.True(result.Passed);
        Assert.Equal("phase3b-ready", result.ProjectId);
        Assert.Equal("phase3b-preflight", result.LatestRunId);
        Assert.Empty(result.Issues);
        Assert.True(File.Exists(result.ReadinessReportPath));
        Assert.True(File.Exists(result.BlueprintPath));
    }

    [Fact]
    public async Task Preflight_MissingProjectBlocks()
    {
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "missing-phase3b-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(GetRepoRoot(), projectRoot));

        var result = await new Phase3BPreflightService(GetRepoRoot())
            .CheckAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, issue => issue.Code == "missing-project");
    }

    [Fact]
    public async Task Preflight_FailedLatestRunBlocks()
    {
        var projectRoot = await CreateReadyProjectAsync("Phase3B Failed Run");
        await MutateJsonAsync(projectRoot, "runs/phase3b-preflight.json", json =>
        {
            json["status"] = WorkflowRunStatus.Failed.ToString();
        });

        var result = await new Phase3BPreflightService(GetRepoRoot())
            .CheckAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, issue => issue.Code == "failed-latest-run");
    }

    [Fact]
    public async Task Preflight_FailedReadinessBlocks()
    {
        var projectRoot = await CreateReadyProjectAsync("Phase3B Failed Readiness");
        await MutateJsonAsync(projectRoot, "reports/readiness-report.json", json =>
        {
            json["passed"] = false;
        });

        var result = await new Phase3BPreflightService(GetRepoRoot())
            .CheckAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, issue => issue.Code == "phase3a-readiness-not-passed");
    }

    [Fact]
    public async Task Preflight_MissingBlueprintBlocks()
    {
        var projectRoot = await CreateReadyProjectAsync("Phase3B Missing Blueprint");
        File.Delete(Path.Combine(projectRoot, "analysis", "visual-blueprint.draft.json"));

        var result = await new Phase3BPreflightService(GetRepoRoot())
            .CheckAsync(projectRoot, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Contains(result.Issues, issue => issue.Code == "missing-phase3a-blueprint");
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "phase3b-preflight-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "phase3b-preflight");

        Assert.True(summary.ReadinessPassed);
        return summary.ArtifactRoot;
    }

    private static async Task MutateJsonAsync(string projectRoot, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        mutate(json);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
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
