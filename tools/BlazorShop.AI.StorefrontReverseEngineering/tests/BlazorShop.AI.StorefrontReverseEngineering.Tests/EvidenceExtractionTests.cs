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
