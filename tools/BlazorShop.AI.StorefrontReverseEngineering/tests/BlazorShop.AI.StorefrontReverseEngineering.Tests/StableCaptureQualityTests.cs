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
        Assert.True(result.QualityReport.FallbackDecision?.ShouldFallback);
    }

    [Fact]
    public async Task StableCapture_NativeScreenshotException_FallsBackWithRenderedEvidenceFromSameSession()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(throwNativeScreenshot: true);
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "native-failure-" + Guid.NewGuid().ToString("N"));
        var viewportRoot = Path.Combine(projectRoot, "captures", "home", "desktop-1440");
        Directory.CreateDirectory(viewportRoot);

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(
                new BrowserPageSession("native-failure", "home", "https://example.test"),
                viewport,
                new CapturePolicy(PreserveViewportSegments: true),
                forceStitchedFallback: false,
                CancellationToken.None,
                viewportRoot,
                $"obj/storefront-reverse-engineering/projects/{Path.GetFileName(projectRoot)}/captures/home/desktop-1440");

        Assert.Equal("stitched", result.Capture.CaptureMethod);
        Assert.Contains("rendered-before-native-failure", result.Capture.DomHtml, StringComparison.Ordinal);
        Assert.NotEmpty(result.Capture.Styles);
        Assert.NotEmpty(result.Segments);
        Assert.Equal(1, browser.OpenSessionCount);
        Assert.Equal(1, browser.LastSession!.ExtractRenderedEvidenceCount);
        Assert.Equal(1, browser.LastSession.CaptureNativeFullPageScreenshotCount);
        Assert.True(browser.LastSession.CaptureViewportScreenshotCount > 0);
        Assert.Contains("native-capture-exception", result.QualityReport.FallbackReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StableCapture_NativeSuccess_DoesNotUseStitchedFallback()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(throwNativeScreenshot: false);

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(
                new BrowserPageSession("native-success", "home", "https://example.test"),
                viewport,
                new CapturePolicy(),
                forceStitchedFallback: false,
                CancellationToken.None);

        Assert.Equal("native-full-page", result.Capture.CaptureMethod);
        Assert.Empty(result.Segments);
        Assert.Equal(1, browser.OpenSessionCount);
        Assert.Equal(1, browser.LastSession!.ExtractRenderedEvidenceCount);
        Assert.Equal(1, browser.LastSession.CaptureNativeFullPageScreenshotCount);
        Assert.Equal(0, browser.LastSession.CaptureViewportScreenshotCount);
    }

    [Fact]
    public async Task StableCapture_Cancellation_IsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => new StableFullPageCaptureService(new SplitOperationBrowser(false))
            .CaptureAsync(
                new BrowserPageSession("canceled", "home", "https://example.test"),
                ViewportDefinition.Defaults[0],
                new CapturePolicy(),
                forceStitchedFallback: false,
                cancellation.Token));
    }

    [Fact]
    public async Task Quality_BlankNativePng_TriggersAutomaticFallback()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(
            throwNativeScreenshot: false,
            nativeScreenshotFactory: capturedViewport => CreateSolidPng(capturedViewport.Width, capturedViewport.Height + 420, "#ffffff"));

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(new BrowserPageSession("blank-native", "home", "https://example.test"), viewport, new CapturePolicy(), false, CancellationToken.None);

        Assert.Equal("stitched", result.Capture.CaptureMethod);
        Assert.True(result.QualityReport.Passed);
        Assert.True(result.QualityReport.FallbackDecision?.ShouldFallback);
        Assert.Contains("blank-image", result.QualityReport.FallbackDecision!.TriggeringFindingCodes);
        Assert.Contains("blank-image", result.QualityReport.FallbackReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Quality_UndecodableNativePng_TriggersAutomaticFallback()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(false, nativeScreenshotFactory: _ => [1, 2, 3, 4, 5]);

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(new BrowserPageSession("invalid-native", "home", "https://example.test"), viewport, new CapturePolicy(), false, CancellationToken.None);

        Assert.Equal("stitched", result.Capture.CaptureMethod);
        Assert.True(result.QualityReport.FallbackDecision?.ShouldFallback);
        Assert.Contains("png-decode-failed", result.QualityReport.FallbackDecision!.TriggeringFindingCodes);
    }

    [Fact]
    public async Task Quality_UnexpectedNativeWidth_TriggersAutomaticFallback()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(
            false,
            nativeScreenshotFactory: capturedViewport => CreatePatternPng(capturedViewport.Width - 24, capturedViewport.Height + 420, "#f7fbff"));

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(new BrowserPageSession("wrong-width", "home", "https://example.test"), viewport, new CapturePolicy(), false, CancellationToken.None);

        Assert.Equal("stitched", result.Capture.CaptureMethod);
        Assert.Contains("unexpected-image-width", result.QualityReport.FallbackDecision!.TriggeringFindingCodes);
    }

    [Fact]
    public async Task Quality_FallbackDisabled_ReturnsNativeQualityFailure()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(
            false,
            nativeScreenshotFactory: capturedViewport => CreateSolidPng(capturedViewport.Width, capturedViewport.Height + 420, "#ffffff"));

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(
                new BrowserPageSession("fallback-disabled", "home", "https://example.test"),
                viewport,
                new CapturePolicy(EnableAutomaticStitchedFallback: false),
                forceStitchedFallback: false,
                CancellationToken.None);

        Assert.Equal("native-full-page", result.Capture.CaptureMethod);
        Assert.False(result.QualityReport.Passed);
        Assert.Empty(result.Segments);
        Assert.False(result.QualityReport.FallbackDecision?.ShouldFallback);
        Assert.Contains("blank-image", result.QualityReport.FallbackDecision!.TriggeringFindingCodes);
    }

    [Fact]
    public async Task Quality_StitchedFallbackFailure_BlocksFinalQuality()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(
            false,
            nativeScreenshotFactory: _ => [9, 8, 7],
            viewportScreenshotFactory: _ => [6, 5, 4]);

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(new BrowserPageSession("stitch-fails", "home", "https://example.test"), viewport, new CapturePolicy(), false, CancellationToken.None);

        Assert.Equal("failed", result.Capture.CaptureMethod);
        Assert.False(result.QualityReport.Passed);
        Assert.True(result.QualityReport.FallbackDecision?.ShouldFallback);
        Assert.Contains(result.QualityReport.Findings, finding => finding.Code == "capture-failed");
    }

    [Fact]
    public async Task Policy_CustomSegmentCountBlocksOverlyTallStitching()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(
            false,
            nativeScreenshotFactory: _ => [1, 2, 3],
            documentHeight: viewport.Height * 3);

        var result = await new StableFullPageCaptureService(browser)
            .CaptureAsync(
                new BrowserPageSession("segment-limit", "home", "https://example.test"),
                viewport,
                new CapturePolicy(MaximumSegmentCount: 1),
                forceStitchedFallback: false,
                CancellationToken.None);

        Assert.Equal("failed", result.Capture.CaptureMethod);
        Assert.False(result.QualityReport.Passed);
        Assert.Contains(result.Capture.Warnings, warning => warning.Contains("segment limit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Policy_CustomScrollSettleIsUsedForStitchSegments()
    {
        var viewport = ViewportDefinition.Defaults[0];
        var browser = new SplitOperationBrowser(false, nativeScreenshotFactory: _ => [1, 2, 3]);

        await new StableFullPageCaptureService(browser)
            .CaptureAsync(
                new BrowserPageSession("settle", "home", "https://example.test"),
                viewport,
                new CapturePolicy(ScrollSettleMilliseconds: 7),
                forceStitchedFallback: false,
                CancellationToken.None);

        Assert.NotEmpty(browser.LastSession!.ScrollDelays);
        Assert.All(browser.LastSession.ScrollDelays, delay => Assert.Equal(7, delay));
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

    private sealed class SplitOperationBrowser : IReferenceBrowser
    {
        private readonly bool throwNativeScreenshot;
        private readonly Func<ViewportDefinition, byte[]>? nativeScreenshotFactory;
        private readonly Func<ViewportDefinition, byte[]>? viewportScreenshotFactory;
        private readonly int? documentHeight;

        public SplitOperationBrowser(
            bool throwNativeScreenshot,
            Func<ViewportDefinition, byte[]>? nativeScreenshotFactory = null,
            Func<ViewportDefinition, byte[]>? viewportScreenshotFactory = null,
            int? documentHeight = null)
        {
            this.throwNativeScreenshot = throwNativeScreenshot;
            this.nativeScreenshotFactory = nativeScreenshotFactory;
            this.viewportScreenshotFactory = viewportScreenshotFactory;
            this.documentHeight = documentHeight;
        }

        public int OpenSessionCount { get; private set; }

        public SplitOperationSession? LastSession { get; private set; }

        public Task<IReferenceBrowserSession> OpenSessionAsync(
            BrowserPageSession session,
            ViewportDefinition viewport,
            CapturePolicy policy,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenSessionCount++;
            LastSession = new SplitOperationSession(viewport, throwNativeScreenshot, nativeScreenshotFactory, viewportScreenshotFactory, documentHeight);
            return Task.FromResult<IReferenceBrowserSession>(LastSession);
        }
    }

    private sealed class SplitOperationSession : IReferenceBrowserSession
    {
        private readonly ViewportDefinition viewport;
        private readonly bool throwNativeScreenshot;
        private readonly Func<ViewportDefinition, byte[]>? nativeScreenshotFactory;
        private readonly Func<ViewportDefinition, byte[]>? viewportScreenshotFactory;
        private readonly int documentHeight;

        public SplitOperationSession(
            ViewportDefinition viewport,
            bool throwNativeScreenshot,
            Func<ViewportDefinition, byte[]>? nativeScreenshotFactory,
            Func<ViewportDefinition, byte[]>? viewportScreenshotFactory,
            int? documentHeight)
        {
            this.viewport = viewport;
            this.throwNativeScreenshot = throwNativeScreenshot;
            this.nativeScreenshotFactory = nativeScreenshotFactory;
            this.viewportScreenshotFactory = viewportScreenshotFactory;
            this.documentHeight = documentHeight ?? viewport.Height + 420;
        }

        public string SessionId { get; } = "split-session";

        public int ExtractRenderedEvidenceCount { get; private set; }

        public int CaptureNativeFullPageScreenshotCount { get; private set; }

        public int CaptureViewportScreenshotCount { get; private set; }

        public List<int> ScrollDelays { get; } = [];

        public Task NavigateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<PageStabilizationReport> StabilizeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new PageStabilizationReport(["wait-dom-ready"], []));
        }

        public async Task<BrowserCaptureResult> CaptureCurrentStateAsync(CancellationToken cancellationToken)
        {
            var evidence = await ExtractRenderedEvidenceAsync(cancellationToken);
            var screenshot = await CaptureNativeFullPageScreenshotAsync(cancellationToken);
            return new BrowserCaptureResult(
                "split-test",
                "native-full-page",
                viewport.Width,
                viewport.Height,
                evidence.DocumentWidth,
                evidence.DocumentHeight,
                evidence.DomHtml,
                screenshot,
                evidence.Styles,
                evidence.Boxes,
                evidence.Assets,
                evidence.Warnings);
        }

        public Task<RenderedPageEvidence> ExtractRenderedEvidenceAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExtractRenderedEvidenceCount++;
            return Task.FromResult(new RenderedPageEvidence(
                viewport.Width,
                documentHeight,
                "<html><body><main class=\"rendered-before-native-failure\">Rendered evidence survived.</main></body></html>",
                [new ComputedStyleSample("main.rendered-before-native-failure", new Dictionary<string, string> { ["display"] = "grid", ["font-family"] = "Inter" }, "ev-001")],
                [new ElementBoxSample("main.rendered-before-native-failure", 0, 0, viewport.Width, 300, "ev-001")],
                [new AssetInventoryItem("fixture-brand.png", "image", 128, 64, "main.rendered-before-native-failure", true, "ev-001")],
                []));
        }

        public Task<byte[]> CaptureNativeFullPageScreenshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CaptureNativeFullPageScreenshotCount++;
            if (throwNativeScreenshot)
            {
                throw new InvalidOperationException("native screenshot failed after evidence extraction");
            }

            return Task.FromResult(nativeScreenshotFactory?.Invoke(viewport) ?? CreatePatternPng(viewport.Width, documentHeight, "#f7fbff"));
        }

        public Task<byte[]> CaptureViewportScreenshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CaptureViewportScreenshotCount++;
            return Task.FromResult(viewportScreenshotFactory?.Invoke(viewport) ?? CreatePatternPng(viewport.Width, viewport.Height, "#eef6ff"));
        }

        public Task<BrowserDocumentMetrics> GetMetricsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new BrowserDocumentMetrics(viewport.Width, documentHeight, viewport.Width, viewport.Height));
        }

        public Task<BrowserActionResult> ExecuteAsync(BrowserSessionAction action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(action.Type, "scroll-to-y", StringComparison.OrdinalIgnoreCase))
            {
                ScrollDelays.Add(action.DelayMilliseconds ?? -1);
            }

            return Task.FromResult(new BrowserActionResult(true, []));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    }

    private static byte[] CreatePatternPng(int width, int height, string color)
    {
        using var image = new MagickImage(new MagickColor(color), (uint)Math.Max(1, width), (uint)Math.Max(1, height));
        using var stripe = new MagickImage(new MagickColor("#dbeafe"), (uint)Math.Max(1, width / 4), (uint)Math.Max(1, height));
        using var lowerBand = new MagickImage(new MagickColor("#bfdbfe"), (uint)Math.Max(1, width), (uint)Math.Max(1, height / 5));
        image.Composite(stripe, Math.Max(0, width / 3), 0, CompositeOperator.Over);
        image.Composite(lowerBand, 0, Math.Max(0, height - (height / 5)), CompositeOperator.Over);
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    private static byte[] CreateSolidPng(int width, int height, string color)
    {
        using var image = new MagickImage(new MagickColor(color), (uint)Math.Max(1, width), (uint)Math.Max(1, height));
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }
}
