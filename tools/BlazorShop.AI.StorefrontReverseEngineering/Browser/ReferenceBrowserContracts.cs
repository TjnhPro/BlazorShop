using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public interface IReferenceBrowser
{
    async Task<BrowserCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        await using var browserSession = await OpenSessionAsync(session, viewport, policy, cancellationToken);
        await browserSession.NavigateAsync(cancellationToken);
        await browserSession.StabilizeAsync(cancellationToken);
        return await browserSession.CaptureCurrentStateAsync(cancellationToken);
    }

    Task<IReferenceBrowserSession> OpenSessionAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReferenceBrowserSession>(new CompatReferenceBrowserSession(this, session, viewport, policy));
    }
}

public interface IReferenceBrowserSession : IAsyncDisposable
{
    string SessionId { get; }

    Task NavigateAsync(CancellationToken cancellationToken);

    Task<PageStabilizationReport> StabilizeAsync(CancellationToken cancellationToken);

    Task<BrowserCaptureResult> CaptureCurrentStateAsync(CancellationToken cancellationToken);

    async Task<byte[]> CaptureViewportScreenshotAsync(CancellationToken cancellationToken)
    {
        var capture = await CaptureCurrentStateAsync(cancellationToken);
        return capture.ScreenshotPng;
    }

    async Task<BrowserDocumentMetrics> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var capture = await CaptureCurrentStateAsync(cancellationToken);
        return new BrowserDocumentMetrics(capture.DocumentWidth, capture.DocumentHeight, capture.ViewportWidth, capture.ViewportHeight);
    }

    Task<BrowserActionResult> ExecuteAsync(
        BrowserSessionAction action,
        CancellationToken cancellationToken);
}

public sealed record BrowserSessionAction(
    string Type,
    string? Selector = null,
    int? DelayMilliseconds = null,
    string? Key = null,
    int? ScrollX = null,
    int? ScrollY = null);

public sealed record BrowserActionResult(
    bool Executed,
    IReadOnlyList<string> Warnings);

public sealed record BrowserDocumentMetrics(
    int DocumentWidth,
    int DocumentHeight,
    int ViewportWidth,
    int ViewportHeight);

public abstract class ReferenceBrowserBase : IReferenceBrowser
{
    public async Task<BrowserCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        await using var browserSession = await OpenSessionAsync(session, viewport, policy, cancellationToken);
        await browserSession.NavigateAsync(cancellationToken);
        await browserSession.StabilizeAsync(cancellationToken);
        return await browserSession.CaptureCurrentStateAsync(cancellationToken);
    }

    public abstract Task<IReferenceBrowserSession> OpenSessionAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken);
}

internal sealed class CompatReferenceBrowserSession : IReferenceBrowserSession
{
    private readonly IReferenceBrowser browser;
    private readonly BrowserPageSession session;
    private readonly ViewportDefinition viewport;
    private readonly CapturePolicy policy;

    public CompatReferenceBrowserSession(
        IReferenceBrowser browser,
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy)
    {
        this.browser = browser;
        this.session = session;
        this.viewport = viewport;
        this.policy = policy;
        SessionId = $"compat-{Guid.NewGuid():N}";
    }

    public string SessionId { get; }

    public Task NavigateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<PageStabilizationReport> StabilizeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new PageStabilizationReport(["compat-capture"], []));

    public Task<BrowserCaptureResult> CaptureCurrentStateAsync(CancellationToken cancellationToken) =>
        browser.CaptureAsync(session, viewport, policy, cancellationToken);

    public Task<BrowserActionResult> ExecuteAsync(BrowserSessionAction action, CancellationToken cancellationToken) =>
        Task.FromResult(new BrowserActionResult(false, ["Compatibility browser sessions do not execute actions."]));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed record BrowserPageSession(
    string ProjectId,
    string PageId,
    string SourceUrl);

public sealed record BrowserCaptureResult(
    string BrowserEngine,
    string CaptureMethod,
    int ViewportWidth,
    int ViewportHeight,
    int DocumentWidth,
    int DocumentHeight,
    string DomHtml,
    byte[] ScreenshotPng,
    IReadOnlyList<ComputedStyleSample> Styles,
    IReadOnlyList<ElementBoxSample> Boxes,
    IReadOnlyList<AssetInventoryItem> Assets,
    IReadOnlyList<string> Warnings,
    string? CaptureCorrelationId = null);

public sealed record ComputedStyleSample(
    string Selector,
    IReadOnlyDictionary<string, string> Properties,
    string? EvidenceId = null);

public sealed record ElementBoxSample(
    string Selector,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height,
    string? EvidenceId = null);

public sealed record AssetInventoryItem(
    string Url,
    string MediaType,
    int? Width,
    int? Height,
    string SourceElement,
    bool ReferenceOnly,
    string? EvidenceId = null);
