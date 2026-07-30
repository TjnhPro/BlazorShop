using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Microsoft.Playwright;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed partial class PlaywrightReferenceBrowser : IReferenceBrowser
{
    public async Task<BrowserCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Timeout = policy.TimeoutMilliseconds
        });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = viewport.Width, Height = viewport.Height },
            DeviceScaleFactor = (float)viewport.DeviceScaleFactor,
            IsMobile = viewport.IsMobile
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(session.SourceUrl, new PageGotoOptions
        {
            Timeout = policy.TimeoutMilliseconds,
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var dom = await page.ContentAsync();
        var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true,
            Type = ScreenshotType.Png
        });
        var documentHeight = await page.EvaluateAsync<int>("() => Math.ceil(document.documentElement.scrollHeight || document.body.scrollHeight || window.innerHeight)");
        if (documentHeight > policy.MaximumPageHeight)
        {
            throw new InvalidOperationException($"[SRE-BROWSER-007] Captured page exceeds maximum height. Problem: '{session.SourceUrl}' is {documentHeight}px tall. Cause: capture policy limits evidence size to {policy.MaximumPageHeight}px. Fix: increase maximum height after review or capture a narrower page.");
        }

        return new BrowserCaptureResult(
            "playwright-chromium",
            "native-full-page",
            viewport.Width,
            viewport.Height,
            viewport.Width,
            documentHeight,
            dom,
            screenshot,
            BuildStyleSamples(),
            BuildBoxes(viewport, documentHeight),
            ExtractAssets(dom),
            []);
    }

    private static IReadOnlyList<ComputedStyleSample> BuildStyleSamples() =>
    [
        new("body", new Dictionary<string, string> { ["display"] = "block" }),
        new("img", new Dictionary<string, string> { ["object-fit"] = "initial" })
    ];

    private static IReadOnlyList<ElementBoxSample> BuildBoxes(ViewportDefinition viewport, int documentHeight) =>
    [
        new("viewport", 0, 0, viewport.Width, viewport.Height),
        new("document", 0, 0, viewport.Width, documentHeight)
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
