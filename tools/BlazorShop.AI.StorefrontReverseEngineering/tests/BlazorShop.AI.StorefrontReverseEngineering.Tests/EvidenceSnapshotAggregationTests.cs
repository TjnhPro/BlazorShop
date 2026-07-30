using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class EvidenceSnapshotAggregationTests
{
    [Fact]
    public async Task Snapshot_MergesConfiguredViewportsAndSourceEvidence()
    {
        var projectRoot = await CreateReadyProjectAsync("Evidence Snapshot Merge");

        var snapshot = await ReadSnapshotAsync(projectRoot);

        Assert.Equal("evidence-snapshot", snapshot.ArtifactKind);
        var page = Assert.Single(snapshot.Pages);
        Assert.Equal(3, page.Viewports.Count);
        Assert.All(page.Viewports, viewport =>
        {
            Assert.NotEmpty(viewport.Elements);
            Assert.NotEmpty(viewport.SourceArtifactPaths);
            Assert.False(string.IsNullOrWhiteSpace(viewport.CaptureCorrelationId));
        });
        Assert.Contains("configuration.json", snapshot.SourceArtifactPaths);
        Assert.Contains("discovery/capture-plan.json", snapshot.SourceArtifactPaths);
        Assert.Contains("reports/readiness-report.json", snapshot.SourceArtifactPaths);
        Assert.NotEmpty(snapshot.SourceEvidenceIds);
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "evidence-snapshot.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "reports", "evidence-snapshot.md")));
    }

    [Fact]
    public async Task Snapshot_MissingViewportArtifactProducesBlockingIssue()
    {
        var projectRoot = await CreateReadyProjectAsync("Evidence Snapshot Missing Viewport");
        File.Delete(Path.Combine(projectRoot, "captures", "home", "mobile-390", "element-evidence-index.json"));

        var snapshot = await new EvidenceSnapshotAggregator(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains(snapshot.Issues, issue =>
            issue.Code == "missing-viewport-artifact" &&
            issue.Severity == "blocking" &&
            issue.PageId == "home" &&
            issue.ViewportId == "mobile-390");
    }

    [Fact]
    public async Task Snapshot_OrphanEvidenceProducesWarning()
    {
        var projectRoot = await CreateReadyProjectAsync("Evidence Snapshot Orphan");
        var orphanRoot = Path.Combine(projectRoot, "captures", "orphan", "desktop-1440");
        Directory.CreateDirectory(orphanRoot);
        File.Copy(
            Path.Combine(projectRoot, "captures", "home", "desktop-1440", "element-evidence-index.json"),
            Path.Combine(orphanRoot, "element-evidence-index.json"));

        var snapshot = await new EvidenceSnapshotAggregator(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains(snapshot.Issues, issue =>
            issue.Code == "orphan-evidence" &&
            issue.Severity == "warning" &&
            issue.ArtifactPath == "captures/orphan/desktop-1440/element-evidence-index.json");
    }

    [Fact]
    public async Task Snapshot_CorrelationMismatchProducesBlockingIssue()
    {
        var projectRoot = await CreateReadyProjectAsync("Evidence Snapshot Correlation");
        await MutateJsonAsync(projectRoot, "captures/home/desktop-1440/asset-inventory.normalized.json", json =>
        {
            json["captureCorrelationId"] = "wrong-correlation";
        });

        var snapshot = await new EvidenceSnapshotAggregator(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains(snapshot.Issues, issue =>
            issue.Code == "capture-correlation-mismatch" &&
            issue.Severity == "blocking" &&
            issue.ArtifactPath == "captures/home/desktop-1440");
    }

    [Fact]
    public async Task Snapshot_LoadsInteractionEvidenceWhenPresent()
    {
        var projectRoot = await CreateReadyProjectAsync("Evidence Snapshot Interaction");
        var interaction = new InteractionEvidence(
            "1.0",
            "interaction-evidence",
            "interaction-evidence-snapshot-interaction-home-desktop-1440-hover",
            DateTimeOffset.UtcNow,
            "evidence-snapshot-interaction",
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
            ChangedElementEvidenceIds: ["interaction-element-1"],
            DomDiffSummary: "DOM content did not change after interaction.",
            StyleDiffSummary: "Computed style evidence changed after interaction.",
            Warnings: [],
            Errors: []);
        var store = CreateStore(projectRoot);
        await store.WriteJsonAsync(
            ArtifactPath.Create("interactions/home/hover/interaction-evidence.json"),
            "interaction-evidence",
            interaction,
            CancellationToken.None);

        var snapshot = await new EvidenceSnapshotAggregator(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains("interactions/home/hover/interaction-evidence.json", snapshot.SourceArtifactPaths);
        Assert.Contains("interaction-element-1", snapshot.SourceEvidenceIds);
    }

    [Fact]
    public async Task Snapshot_SchemaMismatchProducesBlockingIssue()
    {
        var projectRoot = await CreateReadyProjectAsync("Evidence Snapshot Schema");
        await MutateJsonAsync(projectRoot, "captures/home/desktop-1440/element-evidence-index.json", json =>
        {
            json["artifactKind"] = "wrong-kind";
        });

        var snapshot = await new EvidenceSnapshotAggregator(GetRepoRoot())
            .BuildAsync(projectRoot, CancellationToken.None);

        Assert.Contains(snapshot.Issues, issue =>
            issue.Code == "invalid-schema" &&
            issue.Severity == "blocking" &&
            issue.ArtifactPath == "captures/home/desktop-1440/element-evidence-index.json");
    }

    [Fact]
    public async Task Snapshot_ValidatesAgainstRegisteredSchema()
    {
        var projectRoot = await CreateReadyProjectAsync("Evidence Snapshot Schema Validation");
        var repoRoot = GetRepoRoot();
        var resolver = new ApprovedArtifactRootResolver(repoRoot);
        var store = new FileSystemVisualArtifactStore(
            projectRoot,
            resolver,
            new VisualSchemaValidator(new VisualSchemaRegistry()));

        var snapshot = await store.ReadJsonAsync<EvidenceSnapshot>(
            ArtifactPath.Create("analysis/evidence-snapshot.json"),
            "evidence-snapshot",
            CancellationToken.None);

        Assert.Equal("evidence-snapshot", snapshot.ArtifactKind);
        Assert.NotEmpty(snapshot.Pages);
    }

    private static async Task<EvidenceSnapshot> ReadSnapshotAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "evidence-snapshot.json"));
        return JsonSerializer.Deserialize<EvidenceSnapshot>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Evidence snapshot did not deserialize.");
    }

    private static FileSystemVisualArtifactStore CreateStore(string projectRoot)
    {
        var repoRoot = GetRepoRoot();
        var resolver = new ApprovedArtifactRootResolver(repoRoot);
        return new FileSystemVisualArtifactStore(
            projectRoot,
            resolver,
            new VisualSchemaValidator(new VisualSchemaRegistry()));
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "evidence-snapshot-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "evidence-snapshot-fixture");

        Assert.True(summary.ReadinessPassed);
        Assert.Equal(WorkflowRunStatus.Succeeded, summary.RunStatus);
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
