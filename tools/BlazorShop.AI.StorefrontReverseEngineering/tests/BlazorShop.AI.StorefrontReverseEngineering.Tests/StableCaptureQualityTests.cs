using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using ImageMagick;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class StableCaptureQualityTests
{
    [Fact]
    public async Task StableCapture_LazyLoadFixture_AppearsInCapturedOutput()
    {
        var result = await CaptureFixtureAsync(forceStitchedFallback: false);

        Assert.Contains("lazy-section", result.Capture.DomHtml, StringComparison.Ordinal);
        Assert.Equal("native-full-page", result.Capture.CaptureMethod);
        Assert.True(result.QualityReport.Passed);
        Assert.Contains("warm-scroll-down-up", result.Stabilization.Steps);
    }

    [Fact]
    public async Task StableCapture_ForcedFallback_RecordsStitchedMethodAndSegments()
    {
        var (result, viewportRoot) = await CaptureFixtureWithArtifactRootAsync(forceStitchedFallback: true);

        Assert.Equal("stitched", result.Capture.CaptureMethod);
        Assert.NotEmpty(result.Segments);
        Assert.All(result.Segments, segment => Assert.StartsWith("segment-", segment.SegmentId, StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(viewportRoot, "stitch-manifest.json")));
        Assert.All(result.Segments, segment => Assert.True(File.Exists(Path.Combine(GetRepoRoot(), segment.Path!.Replace('/', Path.DirectorySeparatorChar)))));

        using var stitchedInfo = new MagickImage(result.Capture.ScreenshotPng);
        Assert.Equal((uint)result.Capture.ViewportWidth, stitchedInfo.Width);
        Assert.Equal((uint)result.Capture.DocumentHeight, stitchedInfo.Height);
        Assert.Equal(result.Segments.Count, result.QualityReport.SegmentCount);
        Assert.Equal("forced-stitch-proof", result.QualityReport.FallbackReason);
    }

    [Fact]
    public async Task Quality_EmptyScreenshot_BlocksEvidence()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var result = await new StableFullPageCaptureService(new EmptyScreenshotBrowser())
            .CaptureAsync(new BrowserPageSession("quality", "home", "https://example.test"), viewport, new CapturePolicy(), false, CancellationToken.None);

        Assert.False(result.QualityReport.Passed);
        Assert.Contains(result.QualityReport.Findings, finding => finding.Code == "blank-image");
        Assert.Contains(result.QualityReport.Findings, finding => finding.Code == "missing-screenshot-file");
    }

    private static async Task<StableCaptureResult> CaptureFixtureAsync(bool forceStitchedFallback)
    {
        var (result, _) = await CaptureFixtureWithArtifactRootAsync(forceStitchedFallback);
        return result;
    }

    private static async Task<(StableCaptureResult Result, string ViewportRoot)> CaptureFixtureWithArtifactRootAsync(bool forceStitchedFallback)
    {
        var repoRoot = GetRepoRoot();
        var fixturePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        var projectRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "stable-test-" + Guid.NewGuid().ToString("N"));
        var viewportRoot = Path.Combine(projectRoot, "captures", "home", "desktop-1440");
        Directory.CreateDirectory(viewportRoot);
        var result = await new StableFullPageCaptureService(new FixtureReferenceBrowser())
            .CaptureAsync(
                new BrowserPageSession("stable", "home", new Uri(fixturePath).AbsoluteUri),
                ViewportDefinition.Defaults[0],
                new CapturePolicy(),
                forceStitchedFallback,
                CancellationToken.None,
                viewportRoot,
                $"obj/storefront-reverse-engineering/projects/{Path.GetFileName(projectRoot)}/captures/home/desktop-1440");
        return (result, viewportRoot);
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

    private sealed class EmptyScreenshotBrowser : IReferenceBrowser
    {
        public Task<BrowserCaptureResult> CaptureAsync(BrowserPageSession session, ViewportDefinition viewport, CapturePolicy policy, CancellationToken cancellationToken)
        {
            return Task.FromResult(new BrowserCaptureResult(
                "test",
                "native-full-page",
                viewport.Width,
                viewport.Height,
                viewport.Width,
                viewport.Height,
                "<html><body>empty screenshot</body></html>",
                [],
                [],
                [],
                [],
                []));
        }
    }
}
