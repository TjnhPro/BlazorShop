using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
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
        Assert.Contains("analysis/agent-handoff/evidence-manifest.json", manifest.ArtifactList);
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "agent-handoff", "task.md")));
    }

    [Fact]
    public async Task AgentHandoff_EvidenceManifestPackagesScreenshotsAndSectionCrops()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Evidence");
        var evidence = await ReadAsync<AgentHandoffEvidenceManifest>(projectRoot, "analysis/agent-handoff/evidence-manifest.json");
        var home = Assert.Single(evidence.Pages, page => page.PageId == "home");

        Assert.Contains(home.Screenshots, screenshot => screenshot.ViewportId == "desktop-1440");
        Assert.Contains(home.Screenshots, screenshot => screenshot.ViewportId == "mobile-390");
        Assert.NotEmpty(home.Sections);
        Assert.Contains(home.Sections, section => section.ViewportId == "desktop-1440");
        Assert.All(home.Screenshots, screenshot =>
        {
            Assert.StartsWith("analysis/agent-handoff/screenshots/", screenshot.HandoffPath, StringComparison.Ordinal);
            Assert.Equal(screenshot.Sha256, Sha256(projectRoot, screenshot.HandoffPath));
            Assert.Contains("evidence-only", screenshot.OriginalityRestrictions);
            Assert.DoesNotContain("production-safe", screenshot.OriginalityRestrictions);
        });
        Assert.All(home.Sections, section =>
        {
            Assert.StartsWith("analysis/agent-handoff/section-screenshots/", section.HandoffPath, StringComparison.Ordinal);
            Assert.Equal(section.Sha256, Sha256(projectRoot, section.HandoffPath));
            Assert.Contains("reference-only", section.OriginalityRestrictions);
            Assert.DoesNotContain("production-safe", section.OriginalityRestrictions);
        });
    }

    [Fact]
    public async Task AgentHandoffReadiness_MissingSectionScreenshotFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Missing Section Screenshot");
        await RewriteGenerationReadinessAsync(projectRoot, passed: true);
        await new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var evidence = await ReadAsync<AgentHandoffEvidenceManifest>(projectRoot, "analysis/agent-handoff/evidence-manifest.json");
        var section = evidence.Pages.SelectMany(page => page.Sections).First();
        File.Delete(Path.Combine(projectRoot, section.HandoffPath.Replace('/', Path.DirectorySeparatorChar)));

        var report = await new AgentHandoffReadinessValidator(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "missing-section-screenshot");
    }

    [Fact]
    public async Task AgentHandoffReadiness_InvalidEvidenceHashFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Evidence Hash");
        await RewriteGenerationReadinessAsync(projectRoot, passed: true);
        await new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var evidence = await ReadAsync<AgentHandoffEvidenceManifest>(projectRoot, "analysis/agent-handoff/evidence-manifest.json");
        var screenshot = evidence.Pages.SelectMany(page => page.Screenshots).First();
        await File.WriteAllBytesAsync(Path.Combine(projectRoot, screenshot.HandoffPath.Replace('/', Path.DirectorySeparatorChar)), [1, 2, 3, 4]);

        var report = await new AgentHandoffReadinessValidator(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "evidence-hash-mismatch");
    }

    [Fact]
    public async Task AgentHandoffEvidence_InvalidSectionBoundsFailsPackaging()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Invalid Bounds");
        await MutateFirstCompositionNodeAsync(projectRoot, node =>
        {
            node["viewportBoundingBoxes"] = new JsonObject { ["base"] = "x=0;y=0;width=0;height=0" };
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None));
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

    [Fact]
    public async Task AgentHandoffReadiness_MissingManifestFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Missing Manifest");
        File.Delete(Path.Combine(projectRoot, "analysis", "agent-handoff", "manifest.json"));

        var report = await new AgentHandoffReadinessValidator(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "missing-agent-handoff-artifact");
    }

    [Fact]
    public async Task AgentHandoffReadiness_StorefrontV2AllowedTargetFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Storefront V2 Target");
        await RewriteGenerationReadinessAsync(projectRoot, passed: true);
        await new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var allowedPath = Path.Combine(projectRoot, "analysis", "agent-handoff", "allowed-files.json");
        var allowed = JsonNode.Parse(await File.ReadAllTextAsync(allowedPath))!;
        allowed["paths"]!.AsArray().Add("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Home.razor");
        await File.WriteAllTextAsync(allowedPath, allowed.ToJsonString(VisualJson.Options));

        var report = await new AgentHandoffReadinessValidator(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "protected-path-target");
    }

    [Fact]
    public async Task AgentHandoffReadiness_PassesForReviewedFixtureWithoutBlockers()
    {
        var projectRoot = await CreateReadyProjectAsync("Agent Handoff Reviewed Pass");
        await RewriteGenerationReadinessAsync(projectRoot, passed: true);
        await new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);

        var report = await new AgentHandoffReadinessValidator(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.True(report.Passed);
        Assert.DoesNotContain(report.Findings, finding => finding.Severity == "blocking");
    }

    [Fact]
    public async Task AgentHandoffReadiness_WorkflowFailsWhenFinalReadinessFails()
    {
        var summary = await RunProjectAsync("Agent Handoff Workflow Failure");

        Assert.Equal(WorkflowRunStatus.Failed, summary.RunStatus);
        Assert.True(File.Exists(Path.Combine(summary.ArtifactRoot, "analysis", "agent-handoff", "handoff-readiness.json")));
    }

    [Fact]
    public async Task AgentHandoffReadiness_CliRunReturnsNonZeroWhenFinalReadinessFails()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "agent-handoff-cli-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(
            ["run", "--url", fixtureUrl, "--name", "Agent Handoff CLI", "--output-root", outputRoot, "--no-ai", "--force"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(3, exitCode);
        Assert.Contains("Run status: Failed", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentHandoffReadiness_InspectReportsFinalHandoffStatus()
    {
        var summary = await RunProjectAsync("Agent Handoff Inspect");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(["inspect", "--project", summary.ArtifactRoot], stdout, stderr, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("Final handoff readiness:", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Final handoff blockers:", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Agent handoff path: analysis/agent-handoff", stdout.ToString(), StringComparison.Ordinal);
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var summary = await RunProjectAsync(name);
        Assert.True(summary.ReadinessPassed);
        return summary.ArtifactRoot;
    }

    private static async Task<RunSummary> RunProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "agent-handoff-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "agent-handoff-fixture");

        return summary;
    }

    private static async Task<T> ReadAsync<T>(string projectRoot, string relativePath)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Deserialize<T>(json, VisualJson.Options)
            ?? throw new InvalidOperationException($"Artifact '{relativePath}' did not deserialize.");
    }

    private static async Task RewriteGenerationReadinessAsync(string projectRoot, bool passed)
    {
        var path = Path.Combine(projectRoot, "reports", "generation-readiness.json");
        var node = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        node["passed"] = passed;
        node["findings"] = new JsonArray();
        await File.WriteAllTextAsync(path, node.ToJsonString(VisualJson.Options));
    }

    private static async Task MutateFirstCompositionNodeAsync(string projectRoot, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, "analysis", "resolved", "page-compositions.reviewed.json");
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))?.AsObject()
            ?? throw new InvalidOperationException("Page compositions did not parse.");
        var composition = json["compositions"]?.AsArray().OfType<JsonObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("Page compositions did not contain a composition.");
        var node = composition["sectionTree"]?.AsArray().OfType<JsonObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("Page composition did not contain a node.");
        mutate(node);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
    }

    private static string Sha256(string projectRoot, string relativePath)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
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
