using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class EvidenceExtractionTests
{
    [Fact]
    public async Task Evidence_OutputIsBounded()
    {
        var index = await ExtractAsync(new EvidenceExtractionOptions(MaximumElements: 3));

        Assert.True(index.Elements.Count <= 3);
    }

    [Fact]
    public async Task Evidence_LinksProjectPageViewportAndRun()
    {
        var index = await ExtractAsync(new EvidenceExtractionOptions(), runId: "run-123");

        Assert.Equal("evidence", index.ProjectId);
        Assert.Equal("home", index.PageId);
        Assert.Equal("desktop-1440", index.ViewportId);
        Assert.Equal("run-123", index.RunId);
        Assert.All(index.Elements, element => Assert.StartsWith("ev-desktop-1440-", element.EvidenceId, StringComparison.Ordinal));
    }

    [Fact]
    public void Evidence_ValidatorFailsMissingReferencedFiles()
    {
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "evidence-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(repoRoot, projectRoot));
        var extractor = new VisualEvidenceExtractor(repoRoot);
        var manifest = new PageCaptureManifest(
            "1.0",
            "capture-manifest",
            "capture-page-evidence-home",
            DateTimeOffset.UtcNow,
            "evidence",
            "home",
            null,
            ["captures/home/desktop-1440/manifest.json"],
            ["captures/home/desktop-1440/element-evidence-index.json"]);

        var exception = Assert.Throws<InvalidOperationException>(() => extractor.ValidateReferencedFiles(projectRoot, manifest));
        Assert.Contains("SRE-EVIDENCE-001", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Consistency_CapturedViewportAndEvidenceShareCorrelationId()
    {
        var (projectRoot, captured, index) = await CaptureAndExtractViewportAsync(ViewportDefinition.Defaults[0]);

        Assert.Equal(captured.CaptureCorrelationId, captured.Manifest.CaptureCorrelationId);
        Assert.Equal(captured.CaptureCorrelationId, captured.Capture.CaptureCorrelationId);
        Assert.Equal(captured.CaptureCorrelationId, captured.QualityReport.CaptureCorrelationId);
        Assert.Equal(captured.CaptureCorrelationId, index.CaptureCorrelationId);

        var manifest = ReadJson<PageCaptureManifest>(projectRoot, "captures/home/capture-manifest.json");
        Assert.Equal(captured.CaptureCorrelationId, manifest.CaptureCorrelationIds!["desktop-1440"]);
    }

    [Fact]
    public async Task Manifest_PageCaptureManifestAggregatesViewports()
    {
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "manifest-aggregate-" + Guid.NewGuid().ToString("N"));
        var fixturePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        var browser = new FixtureReferenceBrowser();
        var captureService = new VisualCaptureService(repoRoot, browser);
        var extractor = new VisualEvidenceExtractor(repoRoot);
        var session = new BrowserPageSession("manifest", "home", new Uri(fixturePath).AbsoluteUri);

        foreach (var viewport in ViewportDefinition.Defaults.Take(2))
        {
            var captured = await captureService.CaptureViewportAsync(projectRoot, session, viewport, new CapturePolicy(), CancellationToken.None);
            await extractor.WriteViewportEvidenceAsync(projectRoot, session, viewport.Id, captured, new EvidenceExtractionOptions(), CancellationToken.None);
        }

        var manifest = ReadJson<PageCaptureManifest>(projectRoot, "captures/home/capture-manifest.json");
        Assert.Contains("captures/home/desktop-1440/manifest.json", manifest.ViewportManifestPaths);
        Assert.Contains("captures/home/tablet-768/manifest.json", manifest.ViewportManifestPaths);
        Assert.Contains("desktop-1440", manifest.CaptureCorrelationIds!.Keys);
        Assert.Contains("tablet-768", manifest.CaptureCorrelationIds.Keys);
    }

    [Fact]
    public async Task Consistency_ValidatorFailsCorrelationMismatch()
    {
        var (projectRoot, _, _) = await CaptureAndExtractViewportAsync(ViewportDefinition.Defaults[0]);
        var manifest = ReadJson<PageCaptureManifest>(projectRoot, "captures/home/capture-manifest.json") with
        {
            CaptureCorrelationIds = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["desktop-1440"] = "wrong-correlation"
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => new VisualEvidenceExtractor(GetRepoRoot()).ValidateReferencedFiles(projectRoot, manifest));
        Assert.Contains("SRE-EVIDENCE-002", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<ElementEvidenceIndex> ExtractAsync(EvidenceExtractionOptions options, string? runId = null)
    {
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "evidence-test-" + Guid.NewGuid().ToString("N"));
        var fixturePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        var session = new BrowserPageSession("evidence", "home", new Uri(fixturePath).AbsoluteUri);
        var capture = await new FixtureReferenceBrowser()
            .CaptureAsync(session, ViewportDefinition.Defaults[0], new CapturePolicy(), CancellationToken.None);

        return await new VisualEvidenceExtractor(repoRoot)
            .WriteViewportEvidenceAsync(projectRoot, session, "desktop-1440", capture, runId, options, CancellationToken.None);
    }

    private static async Task<(string ProjectRoot, CapturedViewportResult Captured, ElementEvidenceIndex Index)> CaptureAndExtractViewportAsync(ViewportDefinition viewport)
    {
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "consistency-test-" + Guid.NewGuid().ToString("N"));
        var fixturePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        var session = new BrowserPageSession("consistency", "home", new Uri(fixturePath).AbsoluteUri);
        var captured = await new VisualCaptureService(repoRoot, new FixtureReferenceBrowser())
            .CaptureViewportAsync(projectRoot, session, viewport, new CapturePolicy(), CancellationToken.None, runId: "run-consistency");
        var index = await new VisualEvidenceExtractor(repoRoot)
            .WriteViewportEvidenceAsync(projectRoot, session, viewport.Id, captured, new EvidenceExtractionOptions(), CancellationToken.None);
        return (projectRoot, captured, index);
    }

    private static TArtifact ReadJson<TArtifact>(string projectRoot, string relativePath)
    {
        var repoRoot = GetRepoRoot();
        var fullPath = Path.Combine(repoRoot, projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = File.ReadAllText(fullPath);
        return System.Text.Json.JsonSerializer.Deserialize<TArtifact>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Artifact did not deserialize.");
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
