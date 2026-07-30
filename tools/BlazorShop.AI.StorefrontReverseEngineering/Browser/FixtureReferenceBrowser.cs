using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using ImageMagick;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed partial class FixtureReferenceBrowser : IReferenceBrowser
{
    public async Task<BrowserCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var uri = new Uri(session.SourceUrl);
        if (uri.Scheme != "file")
        {
            throw new InvalidOperationException($"[SRE-BROWSER-001] Fixture browser only supports local files. Problem: '{session.SourceUrl}' is not a file URL. Cause: automated tests must not depend on internet capture. Fix: use NodePlaywrightReferenceBrowser for non-fixture URLs.");
        }

        var html = await File.ReadAllTextAsync(uri.LocalPath, cancellationToken);
        var documentHeight = Math.Min(policy.MaximumPageHeight, Math.Max(viewport.Height, html.Length / 3));
        var warnings = documentHeight >= policy.MaximumPageHeight
            ? new[] { "Document height was clamped by capture policy." }
            : [];

        return new BrowserCaptureResult(
            "fixture-html",
            "native-full-page",
            viewport.Width,
            viewport.Height,
            viewport.Width,
            documentHeight,
            html,
            CreateFixturePng(viewport.Width, documentHeight),
            BuildStyleSamples(),
            BuildBoxes(viewport),
            ExtractAssets(html),
            warnings);
    }

    public Task<IReferenceBrowserSession> OpenSessionAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReferenceBrowserSession>(new FixtureReferenceBrowserSession(this, session, viewport, policy));
    }

    private static IReadOnlyList<ComputedStyleSample> BuildStyleSamples() =>
    [
        new("header.site-header", new Dictionary<string, string> { ["position"] = "sticky", ["background-color"] = "#ffffff", ["display"] = "flex" }),
        new("section.hero", new Dictionary<string, string> { ["display"] = "grid", ["font-family"] = "Inter", ["color"] = "#13201a" }),
        new(".product-card", new Dictionary<string, string> { ["border-radius"] = "8px", ["box-shadow"] = "0 8px 24px rgba(0,0,0,.12)" }),
        new("footer", new Dictionary<string, string> { ["background-color"] = "#101820", ["color"] = "#ffffff" })
    ];

    private static IReadOnlyList<ElementBoxSample> BuildBoxes(ViewportDefinition viewport) =>
    [
        new("header.site-header", 0, 0, viewport.Width, 72),
        new("section.hero", 0, 72, viewport.Width, viewport.IsMobile ? 520 : 440),
        new(".product-grid", 24, viewport.IsMobile ? 620 : 560, viewport.Width - 48, viewport.IsMobile ? 900 : 420),
        new("footer", 0, viewport.IsMobile ? 1600 : 1100, viewport.Width, 260)
    ];

    private static IReadOnlyList<AssetInventoryItem> ExtractAssets(string html)
    {
        return ImageRegex().Matches(html)
            .Select(match => new AssetInventoryItem(match.Groups["src"].Value, "image", null, null, "img", true))
            .ToArray();
    }

    private static byte[] CreateFixturePng(int width, int height)
    {
        using var image = new MagickImage(new MagickColor("#f5f7f9"), (uint)Math.Max(1, width), (uint)Math.Max(1, height));
        image.Format = MagickFormat.Png;
        return image.ToByteArray();
    }

    private sealed class FixtureReferenceBrowserSession : IReferenceBrowserSession
    {
        private readonly FixtureReferenceBrowser browser;
        private readonly BrowserPageSession session;
        private readonly ViewportDefinition viewport;
        private readonly CapturePolicy policy;
        private BrowserCaptureResult? capture;

        public FixtureReferenceBrowserSession(
            FixtureReferenceBrowser browser,
            BrowserPageSession session,
            ViewportDefinition viewport,
            CapturePolicy policy)
        {
            this.browser = browser;
            this.session = session;
            this.viewport = viewport;
            this.policy = policy;
            SessionId = $"fixture-{Guid.NewGuid():N}";
        }

        public string SessionId { get; }

        public Task NavigateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<PageStabilizationReport> StabilizeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new PageStabilizationReport(
                ["wait-dom-ready", "wait-network-idle-with-fallback", "wait-fonts-when-available", "wait-important-images", "hide-configured-noise-selectors", "warm-scroll-down-up"],
                policy.StrictWarnings ? [] : [".cookie-banner", "[data-capture-noise]"]));

        public async Task<BrowserCaptureResult> CaptureCurrentStateAsync(CancellationToken cancellationToken)
        {
            capture ??= await browser.CaptureAsync(session, viewport, policy, cancellationToken);
            return capture;
        }

        public Task<byte[]> CaptureViewportScreenshotAsync(CancellationToken cancellationToken) =>
            Task.FromResult(CreateFixturePng(viewport.Width, viewport.Height));

        public async Task<BrowserDocumentMetrics> GetMetricsAsync(CancellationToken cancellationToken)
        {
            var current = await CaptureCurrentStateAsync(cancellationToken);
            return new BrowserDocumentMetrics(current.DocumentWidth, current.DocumentHeight, current.ViewportWidth, current.ViewportHeight);
        }

        public Task<BrowserActionResult> ExecuteAsync(BrowserSessionAction action, CancellationToken cancellationToken) =>
            Task.FromResult(new BrowserActionResult(true, []));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [GeneratedRegex("<img[^>]+src=[\"'](?<src>[^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImageRegex();
}
