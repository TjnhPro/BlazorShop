using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class BrowserCaptureTests
{
    [Fact]
    public async Task BrowserCapture_DesktopFixture_WritesEvidenceFiles()
    {
        var manifest = await CaptureAsync(ViewportDefinition.Defaults.Single(viewport => viewport.Id == "desktop-1440"));

        Assert.Equal("desktop-1440", manifest.ViewportId);
        Assert.Equal("native-full-page", manifest.CaptureMethod);
        Assert.True(File.Exists(ToRepoPath(manifest.ScreenshotPath)));
        Assert.True(File.Exists(ToRepoPath(manifest.DomPath)));
        Assert.True(File.Exists(ToRepoPath(manifest.StylesPath)));
        Assert.True(File.Exists(ToRepoPath(manifest.BoxesPath)));
        Assert.True(File.Exists(ToRepoPath(manifest.AssetsPath)));
        Assert.True(File.Exists(ToRepoPath("captures/home/desktop-1440/capture-quality-report.json")));
    }

    [Fact]
    public async Task BrowserCapture_MobileFixture_WritesEvidenceFiles()
    {
        var manifest = await CaptureAsync(ViewportDefinition.Defaults.Single(viewport => viewport.Id == "mobile-390"));

        Assert.Equal("mobile-390", manifest.ViewportId);
        Assert.Equal(390, manifest.ViewportWidth);
        Assert.True(File.Exists(ToRepoPath(manifest.ScreenshotPath)));
    }

    [Fact]
    public void PlaywrightCapture_UsesRenderedPageEvidenceInsteadOfPlaceholderBuilders()
    {
        var repoRoot = GetRepoRoot();
        var sourcePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Browser", "PlaywrightReferenceBrowser.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("BuildStyleSamples", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildBoxes", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ImageRegex", source, StringComparison.Ordinal);
        Assert.Contains("getComputedStyle", source, StringComparison.Ordinal);
        Assert.Contains("getBoundingClientRect", source, StringComparison.Ordinal);
        Assert.Contains("currentSrc", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowCapture_DoesNotNormalizeFromSecondBrowserCapture()
    {
        var repoRoot = GetRepoRoot();
        var sourcePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Application", "VisualProjectWorkflowService.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("rawCapture = await browser.CaptureAsync", source, StringComparison.Ordinal);
        Assert.Contains("WriteViewportEvidenceAsync(root, session, viewport.Id, captured", source, StringComparison.Ordinal);
    }

    private static async Task<CaptureViewportManifest> CaptureAsync(ViewportDefinition viewport)
    {
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "browser-test-" + Guid.NewGuid().ToString("N"));
        var fixturePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        var service = new VisualCaptureService(repoRoot, new FixtureReferenceBrowser());
        var captured = await service.CaptureViewportAsync(
            projectRoot,
            new BrowserPageSession("browser-test", "home", new Uri(fixturePath).AbsoluteUri),
            viewport,
            new CapturePolicy(),
            CancellationToken.None);
        return captured.Manifest;
    }

    private static string ToRepoPath(string relativeCapturePath)
    {
        var repoRoot = GetRepoRoot();
        var project = Directory.GetDirectories(Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects"), "browser-test-*")
            .OrderByDescending(Directory.GetLastWriteTimeUtc)
            .First();
        return Path.Combine(project, relativeCapturePath.Replace('/', Path.DirectorySeparatorChar));
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
