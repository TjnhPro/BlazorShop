using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed partial class FixtureReferenceBrowser : IReferenceBrowser
{
    private static readonly byte[] OnePixelPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
        0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, 0xDE, 0x00, 0x00, 0x00,
        0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
        0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D, 0xB0, 0x00, 0x00, 0x00,
        0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
    ];

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
            OnePixelPng,
            BuildStyleSamples(),
            BuildBoxes(viewport),
            ExtractAssets(html),
            warnings);
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

    [GeneratedRegex("<img[^>]+src=[\"'](?<src>[^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ImageRegex();
}
