using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "Browser")]
public sealed class BrowserCaptureTests
{
    [Fact]
    public async Task BrowserCapture_DesktopFixture_WritesEvidenceFiles()
    {
        var captured = await CaptureAsync(ViewportDefinition.Defaults.Single(viewport => viewport.Id == "desktop-1440"));
        var manifest = captured.Manifest;

        Assert.Equal("desktop-1440", manifest.ViewportId);
        Assert.Equal("native-full-page", manifest.CaptureMethod);
        Assert.True(File.Exists(ToProjectPath(captured.ProjectRoot, manifest.ScreenshotPath)));
        Assert.True(File.Exists(ToProjectPath(captured.ProjectRoot, manifest.DomPath)));
        Assert.True(File.Exists(ToProjectPath(captured.ProjectRoot, manifest.StylesPath)));
        Assert.True(File.Exists(ToProjectPath(captured.ProjectRoot, manifest.BoxesPath)));
        Assert.True(File.Exists(ToProjectPath(captured.ProjectRoot, manifest.AssetsPath)));
        Assert.True(File.Exists(ToProjectPath(captured.ProjectRoot, "captures/home/desktop-1440/capture-quality-report.json")));
    }

    [Fact]
    public async Task BrowserCapture_MobileFixture_WritesEvidenceFiles()
    {
        var captured = await CaptureAsync(ViewportDefinition.Defaults.Single(viewport => viewport.Id == "mobile-390"));
        var manifest = captured.Manifest;

        Assert.Equal("mobile-390", manifest.ViewportId);
        Assert.Equal(390, manifest.ViewportWidth);
        Assert.True(File.Exists(ToProjectPath(captured.ProjectRoot, manifest.ScreenshotPath)));
    }

    [Fact]
    public void PlaywrightCapture_UsesRenderedPageEvidenceInsteadOfPlaceholderBuilders()
    {
        var repoRoot = GetRepoRoot();
        var sourcePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Browser", "PlaywrightReferenceBrowser.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("Build" + "StyleSamples", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Build" + "Boxes", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ImageRegex", source, StringComparison.Ordinal);
        Assert.Contains("getComputedStyle", source, StringComparison.Ordinal);
        Assert.Contains("getBoundingClientRect", source, StringComparison.Ordinal);
        Assert.Contains("currentSrc", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowCapture_DoesNotNormalizeFromSecondBrowserCapture()
    {
        var repoRoot = GetRepoRoot();
        var sourcePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Application", "VisualProjectWorkflowSteps.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("rawCapture = await browser.CaptureAsync", source, StringComparison.Ordinal);
        Assert.Contains("WriteViewportEvidenceAsync(context.ArtifactRoot, session, viewport.Id, captured", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ReferenceBrowserFactory_HttpUrlUsesDotNetPlaywright()
    {
        var browser = ReferenceBrowserFactory.Create(GetRepoRoot(), "https://example.com/");

        Assert.IsType<PlaywrightReferenceBrowser>(browser);
    }

    [Fact]
    public void ReferenceBrowserFactory_TestHostUsesSyntheticBrowser()
    {
        var browser = ReferenceBrowserFactory.Create(GetRepoRoot(), "https://store.test/");

        Assert.IsType<SyntheticReferenceBrowser>(browser);
    }

    [Fact]
    public void ReferenceBrowserFactory_FileUrlUsesFixtureBrowser()
    {
        var fixturePath = Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");

        var browser = ReferenceBrowserFactory.Create(GetRepoRoot(), new Uri(fixturePath).AbsoluteUri);

        Assert.IsType<FixtureReferenceBrowser>(browser);
    }

    private static async Task<CaptureResult> CaptureAsync(ViewportDefinition viewport)
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
        return new CaptureResult(captured.Manifest, Path.Combine(repoRoot, projectRoot));
    }

    private static string ToProjectPath(string projectRoot, string relativeCapturePath)
    {
        return Path.Combine(projectRoot, relativeCapturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private sealed record CaptureResult(CaptureViewportManifest Manifest, string ProjectRoot);

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
