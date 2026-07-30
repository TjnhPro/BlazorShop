namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed record PageStabilizationReport(
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> HiddenNoiseSelectors,
    IReadOnlyList<string>? Warnings = null);

public sealed record StableCaptureResult(
    BrowserCaptureResult Capture,
    PageStabilizationReport Stabilization,
    CaptureQualityReport QualityReport,
    IReadOnlyList<ScreenshotSegment> Segments);

public sealed record ScreenshotSegment(
    string SegmentId,
    int Y,
    int Height,
    string? Path);

public sealed record StitchManifest(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string ViewportId,
    string OutputPath,
    int OutputWidth,
    int OutputHeight,
    IReadOnlyList<ScreenshotSegment> Segments);

public sealed record CaptureQualityReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string ViewportId,
    string CaptureMethod,
    bool Passed,
    IReadOnlyList<CaptureQualityFinding> Findings,
    IReadOnlyList<string> StabilizationSteps,
    bool? NativeAttemptPassed = null,
    string? FallbackReason = null,
    int? SegmentCount = null,
    int? FinalWidth = null,
    int? FinalHeight = null,
    string? FinalMethod = null,
    IReadOnlyList<string>? Warnings = null);

public sealed record CaptureQualityFinding(
    string Code,
    string Severity,
    string Message);
