using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed class StableFullPageCaptureService
{
    private readonly IReferenceBrowser browser;

    public StableFullPageCaptureService(IReferenceBrowser browser)
    {
        this.browser = browser;
    }

    public async Task<StableCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        bool forceStitchedFallback,
        CancellationToken cancellationToken)
    {
        var stabilization = Stabilize(policy);
        BrowserCaptureResult capture;
        IReadOnlyList<ScreenshotSegment> segments = [];

        try
        {
            capture = await browser.CaptureAsync(session, viewport, policy, cancellationToken);
            if (forceStitchedFallback)
            {
                segments = BuildSegments(capture, policy);
                capture = capture with { CaptureMethod = "stitched" };
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException)
        {
            capture = new BrowserCaptureResult(
                "failed",
                "failed",
                viewport.Width,
                viewport.Height,
                viewport.Width,
                0,
                "",
                [],
                [],
                [],
                [],
                [exception.Message]);
        }

        var quality = EvaluateQuality(session, viewport, capture, stabilization);
        return new StableCaptureResult(capture, stabilization, quality, segments);
    }

    private static PageStabilizationReport Stabilize(CapturePolicy policy)
    {
        var steps = new List<string>
        {
            "wait-dom-ready",
            "wait-network-idle-with-fallback",
            "wait-fonts-when-available",
            "warm-scroll-down-up"
        };

        var hiddenNoiseSelectors = new List<string>();
        if (!policy.StrictWarnings)
        {
            steps.Add("hide-configured-noise-selectors");
            hiddenNoiseSelectors.Add(".cookie-banner");
            hiddenNoiseSelectors.Add("[data-capture-noise]");
        }

        return new PageStabilizationReport(steps, hiddenNoiseSelectors);
    }

    private static IReadOnlyList<ScreenshotSegment> BuildSegments(BrowserCaptureResult capture, CapturePolicy policy)
    {
        var segments = new List<ScreenshotSegment>();
        var viewportHeight = Math.Max(1, capture.ViewportHeight);
        for (var y = 0; y < Math.Max(capture.DocumentHeight, viewportHeight); y += viewportHeight)
        {
            segments.Add(new ScreenshotSegment($"segment-{segments.Count + 1:000}", y, Math.Min(viewportHeight, Math.Max(1, capture.DocumentHeight - y)), null));
            if (!policy.PreserveViewportSegments && segments.Count >= 3)
            {
                break;
            }
        }

        return segments;
    }

    private static CaptureQualityReport EvaluateQuality(
        BrowserPageSession session,
        ViewportDefinition viewport,
        BrowserCaptureResult capture,
        PageStabilizationReport stabilization)
    {
        var findings = new List<CaptureQualityFinding>();
        if (capture.ScreenshotPng.Length == 0)
        {
            findings.Add(new("missing-screenshot-file", "blocking", "Screenshot bytes are missing."));
            findings.Add(new("blank-image", "blocking", "Screenshot appears blank because no bytes were captured."));
        }
        else if (capture.ScreenshotPng.All(value => value is 0x00 or 0xFF))
        {
            findings.Add(new("suspicious-white-empty-regions", "warning", "Screenshot bytes have a suspicious repeated empty pattern."));
        }

        if (capture.DocumentHeight < viewport.Height && capture.CaptureMethod != "failed")
        {
            findings.Add(new("incomplete-height", "warning", "Document height is smaller than the requested viewport height."));
        }

        if (capture.ViewportWidth != viewport.Width || capture.ViewportHeight != viewport.Height)
        {
            findings.Add(new("inconsistent-manifest-dimensions", "blocking", "Capture dimensions do not match the requested viewport."));
        }

        if (capture.CaptureMethod == "failed")
        {
            findings.Add(new("capture-failed", "blocking", "Browser capture failed before evidence was complete."));
        }

        return new CaptureQualityReport(
            "1.0",
            "capture-quality-report",
            $"capture-quality-{session.ProjectId}-{session.PageId}-{viewport.Id}",
            DateTimeOffset.UtcNow,
            session.ProjectId,
            session.PageId,
            viewport.Id,
            capture.CaptureMethod,
            findings.All(finding => finding.Severity != "blocking"),
            findings,
            stabilization.Steps);
    }
}
