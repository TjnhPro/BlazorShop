using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public interface IReferenceBrowser
{
    Task<BrowserCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken);
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
    IReadOnlyList<string> Warnings);

public sealed record ComputedStyleSample(
    string Selector,
    IReadOnlyDictionary<string, string> Properties);

public sealed record ElementBoxSample(
    string Selector,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height);

public sealed record AssetInventoryItem(
    string Url,
    string MediaType,
    int? Width,
    int? Height,
    string SourceElement,
    bool ReferenceOnly);
