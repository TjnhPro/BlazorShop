using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using ImageMagick;
using Microsoft.Playwright;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed class StableFullPageCaptureService
{
    private const string StitchedCaptureMethod = "stitched";
    private static readonly HashSet<string> AutomaticFallbackFindingCodes = new(StringComparer.Ordinal)
    {
        "missing-screenshot-file",
        "png-decode-failed",
        "unexpected-image-width",
        "unexpected-image-height",
        "native-capture-exception",
        "blank-image",
        "document-height-mismatch",
        "missing-lower-page-content",
        "suspicious-single-color-image"
    };

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
        CancellationToken cancellationToken,
        string? viewportArtifactRoot = null,
        string? relativeViewportRoot = null)
    {
        CapturePolicyDefaults.Validate(policy);
        await using var browserSession = await browser.OpenSessionAsync(session, viewport, policy, cancellationToken);
        RenderedPageEvidence? evidence = null;
        PageStabilizationReport stabilization;
        IReadOnlyList<ScreenshotSegment> segments = [];

        try
        {
            await browserSession.NavigateAsync(cancellationToken);
            stabilization = await browserSession.StabilizeAsync(cancellationToken);
            evidence = await browserSession.ExtractRenderedEvidenceAsync(cancellationToken);

            BrowserCaptureResult nativeCapture;
            CaptureQualityReport nativeQuality;
            try
            {
                var nativeScreenshot = await browserSession.CaptureNativeFullPageScreenshotAsync(cancellationToken);
                nativeCapture = CreateCaptureResult("native-full-page", session, viewport, evidence, nativeScreenshot, evidence.DocumentWidth, evidence.DocumentHeight);
            }
            catch (Exception exception) when (IsRecoverableCaptureException(exception))
            {
                var decision = new CaptureFallbackDecision(
                    policy.EnableAutomaticStitchedFallback,
                    policy.EnableAutomaticStitchedFallback
                        ? "native-capture-exception"
                        : "automatic stitched fallback disabled",
                    ["native-capture-exception"]);
                if (!decision.ShouldFallback)
                {
                    var failedNativeCapture = CreateFailedCapture(session, viewport, evidence, exception.Message);
                    var failedNativeQuality = EvaluateQuality(session, viewport, failedNativeCapture, stabilization, policy, false, "native-capture-exception", 0, [exception.Message], decision);
                    return new StableCaptureResult(failedNativeCapture, stabilization, failedNativeQuality, segments, browserSession.SessionId);
                }

                return await RecoverWithStitchedFallbackAsync(
                    browserSession,
                    session,
                    viewport,
                    policy,
                    viewportArtifactRoot,
                    relativeViewportRoot,
                    stabilization,
                    evidence,
                    nativeAttemptPassed: false,
                    fallbackReason: $"native-capture-exception: {exception.Message}",
                    decision,
                    warnings: [exception.Message],
                    cancellationToken);
            }

            nativeQuality = EvaluateQuality(session, viewport, nativeCapture, stabilization, policy, nativeAttemptPassed: null, null, 0, [], null);
            var fallbackDecision = DecideFallback(nativeQuality, forceStitchedFallback, policy);
            nativeQuality = nativeQuality with { FallbackDecision = fallbackDecision };

            if (fallbackDecision.ShouldFallback)
            {
                return await RecoverWithStitchedFallbackAsync(
                    browserSession,
                    session,
                    viewport,
                    policy,
                    viewportArtifactRoot,
                    relativeViewportRoot,
                    stabilization,
                    evidence,
                    nativeQuality.Passed,
                    fallbackDecision.Reason ?? "capture-quality-fallback",
                    fallbackDecision,
                    nativeCapture.Warnings,
                    cancellationToken);
            }

            return new StableCaptureResult(nativeCapture, stabilization, nativeQuality, segments, browserSession.SessionId);
        }
        catch (Exception exception) when (IsRecoverableCaptureException(exception))
        {
            stabilization = new PageStabilizationReport(["capture-failed"], [], [exception.Message]);

            var failedCapture = CreateFailedCapture(session, viewport, evidence, exception.Message);
            var failedQuality = EvaluateQuality(session, viewport, failedCapture, stabilization, policy, false, exception.Message, 0, [exception.Message], null);
            return new StableCaptureResult(failedCapture, stabilization, failedQuality, segments, browserSession.SessionId);
        }
    }

    private static async Task<StableCaptureResult> RecoverWithStitchedFallbackAsync(
        IReferenceBrowserSession browserSession,
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        string? viewportArtifactRoot,
        string? relativeViewportRoot,
        PageStabilizationReport stabilization,
        RenderedPageEvidence evidence,
        bool? nativeAttemptPassed,
        string fallbackReason,
        CaptureFallbackDecision decision,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ScreenshotSegment> segments = [];
        try
        {
            var stitch = await CaptureStitchedAsync(browserSession, session, viewport, policy, viewportArtifactRoot, relativeViewportRoot, cancellationToken);
            segments = stitch.Segments;
            var stitchedCapture = CreateCaptureResult(
                StitchedCaptureMethod,
                session,
                viewport,
                evidence,
                stitch.Png,
                stitch.Width,
                stitch.Height) with
            {
                Warnings = evidence.Warnings.Concat(warnings).Concat(stitch.Warnings).ToArray()
            };
            var recoveredQuality = EvaluateQuality(session, viewport, stitchedCapture, stabilization, policy, nativeAttemptPassed, fallbackReason, segments.Count, stitch.Warnings, decision);
            return new StableCaptureResult(stitchedCapture, stabilization, recoveredQuality, segments, browserSession.SessionId);
        }
        catch (Exception exception) when (IsRecoverableCaptureException(exception))
        {
            var failedCapture = CreateFailedCapture(session, viewport, evidence, exception.Message);
            var failedQuality = EvaluateQuality(session, viewport, failedCapture, stabilization, policy, false, fallbackReason, segments.Count, warnings.Concat([exception.Message]).ToArray(), decision);
            return new StableCaptureResult(failedCapture, stabilization, failedQuality, segments, browserSession.SessionId);
        }
    }

    private static BrowserCaptureResult CreateCaptureResult(
        string captureMethod,
        BrowserPageSession session,
        ViewportDefinition viewport,
        RenderedPageEvidence evidence,
        byte[] screenshotPng,
        int documentWidth,
        int documentHeight)
    {
        return new BrowserCaptureResult(
            "playwright-compatible",
            captureMethod,
            viewport.Width,
            viewport.Height,
            documentWidth,
            documentHeight,
            evidence.DomHtml,
            screenshotPng,
            evidence.Styles,
            evidence.Boxes,
            evidence.Assets,
            evidence.Warnings);
    }

    private static BrowserCaptureResult CreateFailedCapture(
        BrowserPageSession session,
        ViewportDefinition viewport,
        RenderedPageEvidence? evidence,
        string warning)
    {
        return new BrowserCaptureResult(
            "failed",
            "failed",
            viewport.Width,
            viewport.Height,
            evidence?.DocumentWidth ?? viewport.Width,
            evidence?.DocumentHeight ?? 0,
            evidence?.DomHtml ?? "",
            [],
            evidence?.Styles ?? [],
            evidence?.Boxes ?? [],
            evidence?.Assets ?? [],
            (evidence?.Warnings ?? []).Concat([warning]).ToArray());
    }

    private static bool IsRecoverableCaptureException(Exception exception) =>
        exception is InvalidOperationException or TimeoutException or PlaywrightException;

    private static async Task<StitchCaptureOutput> CaptureStitchedAsync(
        IReferenceBrowserSession browserSession,
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        string? viewportArtifactRoot,
        string? relativeViewportRoot,
        CancellationToken cancellationToken)
    {
        var metrics = await browserSession.GetMetricsAsync(cancellationToken);
        if (metrics.DocumentHeight <= 0)
        {
            throw new InvalidOperationException("[SRE-STITCH-001] Cannot stitch page with empty document height. Problem: browser returned zero document height. Cause: page capture did not render usable content. Fix: inspect browser navigation errors and fixture markup.");
        }

        if (metrics.DocumentHeight > policy.MaximumPageHeight)
        {
            throw new InvalidOperationException($"[SRE-STITCH-002] Cannot stitch page beyond capture policy height. Problem: document height is {metrics.DocumentHeight}px. Cause: policy maximum is {policy.MaximumPageHeight}px. Fix: increase maximum height after review or reduce capture scope.");
        }

        if (policy.SegmentOverlapPixels >= viewport.Height)
        {
            throw new InvalidOperationException($"[SRE-STITCH-007] Segment overlap must be smaller than viewport height. Problem: overlap is {policy.SegmentOverlapPixels}px and viewport height is {viewport.Height}px. Cause: stitching cannot advance scroll positions. Fix: reduce capturePolicy.segmentOverlapPixels below the smallest viewport height.");
        }

        var step = Math.Max(1, viewport.Height - policy.SegmentOverlapPixels);
        var positions = new List<int>();
        for (var y = 0; y < metrics.DocumentHeight; y += step)
        {
            positions.Add(Math.Min(y, Math.Max(0, metrics.DocumentHeight - viewport.Height)));
            if (positions.Count > policy.MaximumSegmentCount)
            {
                throw new InvalidOperationException($"[SRE-STITCH-003] Stitched capture exceeded segment limit. Problem: more than {policy.MaximumSegmentCount} viewport segments are required. Cause: page is too tall for deterministic local capture. Fix: reduce page scope or increase the reviewed policy limit.");
            }

            if (positions[^1] + viewport.Height >= metrics.DocumentHeight)
            {
                break;
            }
        }

        positions = positions.Distinct().Order().ToList();
        var segmentRoot = viewportArtifactRoot is null ? null : Path.Combine(viewportArtifactRoot, "viewport-segments");
        if (segmentRoot is not null)
        {
            Directory.CreateDirectory(segmentRoot);
        }

        var loadedSegments = new List<(ScreenshotSegment Metadata, byte[] Png)>();
        for (var index = 0; index < positions.Count; index++)
        {
            var y = positions[index];
            await browserSession.ExecuteAsync(new BrowserSessionAction("scroll-to-y", ScrollY: y, DelayMilliseconds: policy.ScrollSettleMilliseconds), cancellationToken);
            var png = await browserSession.CaptureViewportScreenshotAsync(cancellationToken);
            EnsurePngHasExpectedDimensions(png, viewport.Width, viewport.Height);

            var fileName = $"segment-{index + 1:000}.png";
            var relativePath = relativeViewportRoot is null ? null : $"{relativeViewportRoot}/viewport-segments/{fileName}";
            if (segmentRoot is not null)
            {
                await File.WriteAllBytesAsync(Path.Combine(segmentRoot, fileName), png, cancellationToken);
            }

            loadedSegments.Add((new ScreenshotSegment($"segment-{index + 1:000}", y, Math.Min(viewport.Height, metrics.DocumentHeight - y), relativePath), png));
        }

        var stitched = ComposeSegments(loadedSegments, viewport.Width, metrics.DocumentHeight);
        if (viewportArtifactRoot is not null)
        {
            await File.WriteAllBytesAsync(Path.Combine(viewportArtifactRoot, "full-page.png"), stitched, cancellationToken);
            var manifest = new StitchManifest(
                "1.0",
                "stitch-manifest",
                $"stitch-{session.ProjectId}-{session.PageId}-{viewport.Id}",
                DateTimeOffset.UtcNow,
                session.ProjectId,
                session.PageId,
                viewport.Id,
                relativeViewportRoot is null ? "full-page.png" : $"{relativeViewportRoot}/full-page.png",
                viewport.Width,
                metrics.DocumentHeight,
                loadedSegments.Select(segment => segment.Metadata).ToArray());
            await File.WriteAllTextAsync(Path.Combine(viewportArtifactRoot, "stitch-manifest.json"), Serialize(manifest), cancellationToken);
        }

        return new StitchCaptureOutput(
            stitched,
            viewport.Width,
            metrics.DocumentHeight,
            loadedSegments.Select(segment => segment.Metadata).ToArray(),
            []);
    }

    private static byte[] ComposeSegments(
        IReadOnlyList<(ScreenshotSegment Metadata, byte[] Png)> segments,
        int width,
        int height)
    {
        if (segments.Count == 0)
        {
            throw new InvalidOperationException("[SRE-STITCH-004] Cannot compose stitched image without segments. Problem: no segment screenshots were captured. Cause: capture plan produced no scroll positions. Fix: inspect viewport and document dimensions.");
        }

        using var canvas = new MagickImage(MagickColors.Transparent, (uint)width, (uint)height);
        foreach (var segment in segments)
        {
            using var image = new MagickImage(segment.Png);
            var drawHeight = Math.Min((int)image.Height, Math.Max(1, height - segment.Metadata.Y));
            image.Crop(new MagickGeometry(0, 0, Math.Min((uint)width, image.Width), (uint)drawHeight));
            canvas.Composite(image, 0, segment.Metadata.Y, CompositeOperator.Over);
        }

        canvas.Format = MagickFormat.Png;
        return canvas.ToByteArray();
    }

    private static CaptureQualityReport EvaluateQuality(
        BrowserPageSession session,
        ViewportDefinition viewport,
        BrowserCaptureResult capture,
        PageStabilizationReport stabilization,
        CapturePolicy policy,
        bool? nativeAttemptPassed,
        string? fallbackReason,
        int segmentCount,
        IReadOnlyList<string> warnings,
        CaptureFallbackDecision? fallbackDecision)
    {
        var findings = new List<CaptureQualityFinding>();
        (int Width, int Height)? imageSize = null;

        if (capture.ScreenshotPng.Length == 0)
        {
            findings.Add(new("missing-screenshot-file", "blocking", "Screenshot bytes are missing."));
            findings.Add(new("blank-image", "blocking", "Screenshot appears blank because no bytes were captured."));
        }
        else
        {
            try
            {
                var analysis = AnalyzePng(capture.ScreenshotPng);
                imageSize = (analysis.Width, analysis.Height);
                if (imageSize.Value.Width != viewport.Width)
                {
                    findings.Add(new("unexpected-image-width", "blocking", $"Screenshot width is {imageSize.Value.Width}px but expected {viewport.Width}px."));
                }

                if (imageSize.Value.Height < viewport.Height && capture.CaptureMethod != "failed")
                {
                    findings.Add(new("unexpected-image-height", "blocking", $"Screenshot height is {imageSize.Value.Height}px but expected at least {viewport.Height}px."));
                }

                if (capture.CaptureMethod == "native-full-page" &&
                    Math.Abs(imageSize.Value.Height - capture.DocumentHeight) > 2)
                {
                    findings.Add(new("document-height-mismatch", "blocking", $"Native screenshot height is {imageSize.Value.Height}px but rendered document height is {capture.DocumentHeight}px."));
                }

                if (capture.DocumentHeight > viewport.Height &&
                    imageSize.Value.Height + 2 < capture.DocumentHeight)
                {
                    findings.Add(new("missing-lower-page-content", "blocking", "Screenshot does not include lower-page content required by the rendered document height."));
                }

                if (analysis.DominantColorRatio >= policy.MaximumSingleColorRatio)
                {
                    findings.Add(new("blank-image", "blocking", $"Screenshot appears blank because one color covers {analysis.DominantColorRatio:P1} of sampled pixels."));
                    findings.Add(new("suspicious-single-color-image", "blocking", "Screenshot has very low visual entropy and may be blank."));
                }
                else if (analysis.LowerBandDominantColorRatio >= policy.MaximumSingleColorRatio &&
                         analysis.DominantColorRatio > 0.90 &&
                         capture.DocumentHeight > viewport.Height)
                {
                    findings.Add(new("missing-lower-page-content", "blocking", "Lower-page screenshot band appears blank after full-page capture."));
                }
            }
            catch (Exception exception) when (exception is MagickException or ArgumentException)
            {
                findings.Add(new("png-decode-failed", "blocking", "Screenshot is not a decodable PNG."));
            }
        }

        if (capture.DocumentHeight < viewport.Height && capture.CaptureMethod != "failed")
        {
            findings.Add(new("incomplete-height", "warning", "Document height is smaller than the requested viewport height."));
        }

        if (capture.ViewportWidth != viewport.Width || capture.ViewportHeight != viewport.Height)
        {
            findings.Add(new("inconsistent-manifest-dimensions", "blocking", "Capture dimensions do not match the requested viewport."));
        }

        if (capture.CaptureMethod == StitchedCaptureMethod && segmentCount == 0)
        {
            findings.Add(new("stitched-output-missing-segments", "blocking", "Capture method is stitched but no real segment screenshots were recorded."));
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
            stabilization.Steps,
            nativeAttemptPassed,
            fallbackReason,
            segmentCount,
            imageSize?.Width,
            imageSize?.Height,
            capture.CaptureMethod,
            warnings.Concat(stabilization.Warnings ?? []).ToArray(),
            CaptureCorrelationId: null,
            fallbackDecision);
    }

    private static void EnsurePngHasExpectedDimensions(byte[] png, int expectedWidth, int expectedHeight)
    {
        (int Width, int Height) info;
        try
        {
            info = ReadPngDimensions(png);
        }
        catch (Exception exception) when (exception is MagickException or ArgumentException)
        {
            throw new InvalidOperationException("[SRE-STITCH-005] Segment screenshot is not a PNG. Problem: browser returned undecodable bytes. Cause: viewport capture failed. Fix: inspect browser runtime setup.", exception);
        }

        if (info.Width != expectedWidth || info.Height != expectedHeight)
        {
            throw new InvalidOperationException($"[SRE-STITCH-006] Segment screenshot dimensions are invalid. Problem: segment is {info.Width}x{info.Height}; expected {expectedWidth}x{expectedHeight}. Cause: viewport capture did not use the configured dimensions. Fix: inspect browser context viewport setup.");
        }
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] png)
    {
        using var image = new MagickImage(png);
        return ((int)image.Width, (int)image.Height);
    }

    private static PngQualityAnalysis AnalyzePng(byte[] png)
    {
        using var image = new MagickImage(png);
        return new PngQualityAnalysis(
            (int)image.Width,
            (int)image.Height,
            CalculateDominantColorRatio(image),
            CalculateLowerBandDominantColorRatio(image));
    }

    private static double CalculateDominantColorRatio(IMagickImage<byte> image)
    {
        var histogram = image.Histogram();
        if (histogram.Count == 0 || image.Width == 0 || image.Height == 0)
        {
            return 1;
        }

        var dominant = histogram.Values.Max();
        return dominant / (double)(image.Width * image.Height);
    }

    private static double CalculateLowerBandDominantColorRatio(IMagickImage<byte> image)
    {
        if (image.Height < 4)
        {
            return CalculateDominantColorRatio(image);
        }

        using var lowerBand = image.Clone();
        var y = (int)Math.Floor(image.Height * 0.8);
        var height = Math.Max(1, (int)image.Height - y);
        lowerBand.Crop(new MagickGeometry(0, y, image.Width, (uint)height));
        return CalculateDominantColorRatio(lowerBand);
    }

    private static CaptureFallbackDecision DecideFallback(
        CaptureQualityReport nativeQuality,
        bool forceStitchedFallback,
        CapturePolicy policy)
    {
        if (forceStitchedFallback)
        {
            return new CaptureFallbackDecision(true, "forced-stitch-proof", ["forced-stitch-proof"]);
        }

        var triggeringCodes = nativeQuality.Findings
            .Where(finding => finding.Severity == "blocking" && AutomaticFallbackFindingCodes.Contains(finding.Code))
            .Select(finding => finding.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (triggeringCodes.Length == 0)
        {
            return new CaptureFallbackDecision(false, null, []);
        }

        return new CaptureFallbackDecision(
            policy.EnableAutomaticStitchedFallback,
            policy.EnableAutomaticStitchedFallback
                ? string.Join("; ", triggeringCodes)
                : "automatic stitched fallback disabled",
            triggeringCodes);
    }

    private sealed record PngQualityAnalysis(
        int Width,
        int Height,
        double DominantColorRatio,
        double LowerBandDominantColorRatio);

    private static string Serialize<TValue>(TValue value) =>
        System.Text.Json.JsonSerializer.Serialize(value, VisualJson.Options) + Environment.NewLine;

    private sealed record StitchCaptureOutput(
        byte[] Png,
        int Width,
        int Height,
        IReadOnlyList<ScreenshotSegment> Segments,
        IReadOnlyList<string> Warnings);
}
