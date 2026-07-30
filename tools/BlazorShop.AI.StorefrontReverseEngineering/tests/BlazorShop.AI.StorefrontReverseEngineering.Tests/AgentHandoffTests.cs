using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class AgentHandoffTests
{
    [Fact]
    public async Task AgentHandoff_ManifestListsEveryRequiredArtifact()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Manifest");
        var manifest = await ReadAsync<AgentHandoffManifest>(projectRoot, "analysis/agent-handoff/manifest.json");

        Assert.Contains("analysis/agent-handoff/page-compositions.json", manifest.ArtifactList);
        Assert.Contains("analysis/agent-handoff/visual-blueprint.json", manifest.ArtifactList);
        Assert.Contains("analysis/agent-handoff/generation-readiness.json", manifest.ArtifactList);
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "agent-handoff", "task.md")));
    }

    [Fact]
    public async Task AgentHandoff_IsDeterministicAcrossTwoAssemblies()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Deterministic");
        var first = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "agent-handoff", "manifest.json"));

        await new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var second = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "agent-handoff", "manifest.json"));

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task AgentHandoff_ProtectedFileManifestBlocksRuntimeAndBackendTargets()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Protected");
        var protectedFiles = await ReadAsync<AgentHandoffFileManifest>(projectRoot, "analysis/agent-handoff/protected-files.json");

        Assert.Contains(protectedFiles.Paths, path => path.Contains("BlazorShop.Storefront.Presentation", StringComparison.Ordinal));
        Assert.Contains(protectedFiles.Paths, path => path.Contains("BlazorShop.Storefront.Runtime", StringComparison.Ordinal));
        Assert.Contains(protectedFiles.Paths, path => path.Contains("BlazorShop.Storefront.V2", StringComparison.Ordinal));
        Assert.Contains(protectedFiles.Paths, path => path.Contains("CommerceNode", StringComparison.Ordinal));
        Assert.Contains(protectedFiles.Paths, path => path.Contains("ControlPlane", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AgentHandoff_AllowedFilesExcludeProtectedTargets()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Allowed");
        var allowed = await ReadAsync<AgentHandoffFileManifest>(projectRoot, "analysis/agent-handoff/allowed-files.json");

        Assert.NotEmpty(allowed.Paths);
        Assert.DoesNotContain(allowed.Paths, path => path.Contains("BlazorShop.Storefront.V2", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(allowed.Paths, path => path.Contains("CommerceNode", StringComparison.OrdinalIgnoreCase));
        Assert.All(allowed.Paths, path => Assert.Matches("^(Pages|Components)/", path));
    }

    [Fact]
    public async Task AgentHandoff_TaskMarkdownContainsImplementationContext()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Task");
        var task = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "agent-handoff", "task.md"));

        Assert.Contains("Allowed file areas", task, StringComparison.Ordinal);
        Assert.Contains("Protected file areas", task, StringComparison.Ordinal);
        Assert.Contains("StorefrontBuilder must not consume this package", task, StringComparison.Ordinal);
        Assert.Contains("Expected QA", task, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentHandoff_UnresolvedCriticalRegionsReflectReadinessBlockers()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Unresolved");
        var unresolved = await ReadAsync<AgentHandoffUnresolvedRegions>(projectRoot, "analysis/agent-handoff/unresolved-regions.json");
        var manifest = await ReadAsync<AgentHandoffManifest>(projectRoot, "analysis/agent-handoff/manifest.json");

        Assert.False(manifest.ReadinessPassed);
        Assert.NotEmpty(unresolved.BlockingRegions);
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "agent-handoff-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "agent-handoff-fixture");

        Assert.True(summary.ReadinessPassed);
        return summary.ArtifactRoot;
    }

    private static async Task<T> ReadAsync<T>(string projectRoot, string relativePath)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Deserialize<T>(json, VisualJson.Options)
            ?? throw new InvalidOperationException($"Artifact '{relativePath}' did not deserialize.");
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
