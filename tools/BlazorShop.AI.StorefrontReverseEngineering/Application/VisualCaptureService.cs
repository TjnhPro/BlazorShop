using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Application;

public sealed class VisualCaptureService
{
    private readonly IReferenceBrowser browser;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public VisualCaptureService(string repoRoot, IReferenceBrowser browser)
    {
        this.browser = browser;
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<CapturedViewportResult> CaptureViewportAsync(
        string projectRoot,
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken,
        string? runId = null)
    {
        var captureCorrelationId = $"cap-{Guid.NewGuid():N}";
        var root = resolver.ResolveRoot(projectRoot);
        var viewportRoot = Path.Combine(root, "captures", session.PageId, viewport.Id);
        Directory.CreateDirectory(viewportRoot);
        var relativeRoot = $"captures/{session.PageId}/{viewport.Id}";
        var stableResult = await new StableFullPageCaptureService(browser)
            .CaptureAsync(session, viewport, policy, forceStitchedFallback: false, cancellationToken, viewportRoot, relativeRoot);
        var result = stableResult.Capture with { CaptureCorrelationId = captureCorrelationId };
        var qualityReport = stableResult.QualityReport with { CaptureCorrelationId = captureCorrelationId };
        var stabilizationReport = stableResult.Stabilization with { CaptureCorrelationId = captureCorrelationId };

        await File.WriteAllBytesAsync(Path.Combine(viewportRoot, "full-page.png"), result.ScreenshotPng, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(viewportRoot, "dom.html"), result.DomHtml, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(viewportRoot, "styles.json"), Serialize(result.Styles), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(viewportRoot, "boxes.json"), Serialize(result.Boxes), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(viewportRoot, "assets.json"), Serialize(result.Assets), cancellationToken);

        var manifest = new CaptureViewportManifest(
            "1.0",
            "capture-manifest",
            $"capture-{session.ProjectId}-{session.PageId}-{viewport.Id}",
            DateTimeOffset.UtcNow,
            session.ProjectId,
            session.PageId,
            viewport.Id,
            session.SourceUrl,
            result.BrowserEngine,
            result.CaptureMethod,
            result.ViewportWidth,
            result.ViewportHeight,
            result.DocumentWidth,
            result.DocumentHeight,
            $"{relativeRoot}/full-page.png",
            $"{relativeRoot}/dom.html",
            $"{relativeRoot}/styles.json",
            $"{relativeRoot}/boxes.json",
            $"{relativeRoot}/assets.json",
            result.Warnings,
            captureCorrelationId,
            runId,
            stableResult.BrowserSessionId);

        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        await store.WriteJsonAsync(ArtifactPath.Create($"{relativeRoot}/manifest.json"), "capture-manifest", manifest, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create($"{relativeRoot}/capture-quality-report.json"), "capture-quality-report", qualityReport, cancellationToken);
        return new CapturedViewportResult(
            manifest,
            result,
            qualityReport,
            stabilizationReport,
            captureCorrelationId,
            runId,
            stableResult.BrowserSessionId);
    }

    private static string Serialize<TValue>(TValue value) =>
        System.Text.Json.JsonSerializer.Serialize(value, VisualJson.Options) + Environment.NewLine;
}

public sealed record CaptureViewportManifest(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string ViewportId,
    string SourceUrl,
    string BrowserEngine,
    string CaptureMethod,
    int ViewportWidth,
    int ViewportHeight,
    int DocumentWidth,
    int DocumentHeight,
    string ScreenshotPath,
    string DomPath,
    string StylesPath,
    string BoxesPath,
    string AssetsPath,
    IReadOnlyList<string> Warnings,
    string? CaptureCorrelationId = null,
    string? RunId = null,
    string? BrowserSessionId = null);

public sealed record CapturedViewportResult(
    CaptureViewportManifest Manifest,
    BrowserCaptureResult Capture,
    CaptureQualityReport QualityReport,
    PageStabilizationReport StabilizationReport,
    string CaptureCorrelationId,
    string? RunId,
    string? BrowserSessionId);
