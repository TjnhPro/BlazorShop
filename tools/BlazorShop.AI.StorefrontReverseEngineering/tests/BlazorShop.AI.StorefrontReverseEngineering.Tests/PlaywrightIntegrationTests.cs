using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using ImageMagick;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "Browser")]
public sealed class PlaywrightIntegrationTests
{
    [Fact]
    [Trait("Category", "Playwright")]
    public async Task Playwright_HttpFixture_CapturesRenderedEvidence()
    {
        await using var server = await StartServerAsync();
        var browser = new PlaywrightReferenceBrowser();
        var viewport = ViewportDefinition.Defaults.Single(candidate => candidate.Id == "desktop-1440");

        var capture = await browser.CaptureAsync(
            new BrowserPageSession("playwright", "home", server.BaseUrl),
            viewport,
            new CapturePolicy(),
            CancellationToken.None);

        using var image = new MagickImage(capture.ScreenshotPng);
        Assert.Equal((uint)viewport.Width, image.Width);
        Assert.True(image.Height >= (uint)viewport.Height);
        Assert.Contains(capture.Styles, style => style.Selector.Contains("header", StringComparison.Ordinal) && style.Properties["position"] == "sticky");
        Assert.Contains(capture.Boxes, box => box.Selector.Contains("product-card", StringComparison.Ordinal) && box.Width > 0 && box.Height > 0);
        Assert.Contains(capture.Styles, style => style.Selector.Contains("section.hero", StringComparison.Ordinal) && style.Properties["display"] == "grid");
        Assert.Contains(capture.Assets, asset => asset.MediaType == "css-background-image");
        Assert.Contains(capture.Assets, asset => asset.MediaType == "inline-svg");
        Assert.Contains(capture.Assets, asset => asset.MediaType == "video-poster");
    }

    [Fact]
    [Trait("Category", "Playwright")]
    public async Task Playwright_HttpFixture_MobileEvidenceDiffersFromDesktop()
    {
        await using var server = await StartServerAsync();
        var browser = new PlaywrightReferenceBrowser();
        var desktop = await browser.CaptureAsync(new BrowserPageSession("playwright", "home", server.BaseUrl), ViewportDefinition.Defaults[0], new CapturePolicy(), CancellationToken.None);
        var mobile = await browser.CaptureAsync(new BrowserPageSession("playwright", "home", server.BaseUrl), ViewportDefinition.Defaults.Single(candidate => candidate.Id == "mobile-390"), new CapturePolicy(), CancellationToken.None);

        Assert.NotEqual(desktop.ViewportWidth, mobile.ViewportWidth);
        Assert.Contains(mobile.Styles, style => style.Selector.Contains("button.mobile-menu", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Playwright")]
    public async Task Playwright_HttpFixture_CustomCapturePolicyLimitsEvidence()
    {
        await using var server = await StartServerAsync();
        var browser = new PlaywrightReferenceBrowser();
        var capture = await browser.CaptureAsync(
            new BrowserPageSession("playwright-policy", "home", server.BaseUrl),
            ViewportDefinition.Defaults[0],
            new CapturePolicy(MaximumEvidenceElements: 2, MaximumEvidenceAssets: 1, MaximumTextLength: 24),
            CancellationToken.None);

        Assert.True(capture.Styles.Count <= 2);
        Assert.True(capture.Boxes.Count <= 2);
        Assert.True(capture.Assets.Count <= 1);
        Assert.All(
            capture.Styles.Where(style => style.Properties.TryGetValue("text-snippet", out _)),
            style => Assert.True(style.Properties["text-snippet"].Length <= 24));
    }

    [Fact]
    [Trait("Category", "Playwright")]
    public async Task Playwright_HttpFixture_CustomNoiseSelectorIsHidden()
    {
        await using var server = await StartServerAsync();
        var browser = new PlaywrightReferenceBrowser();
        var viewport = ViewportDefinition.Defaults[0];

        await using var session = await browser.OpenSessionAsync(
            new BrowserPageSession("playwright-noise", "home", server.BaseUrl),
            viewport,
            new CapturePolicy(NoiseSelectors: [".product-card"]),
            CancellationToken.None);
        await session.NavigateAsync(CancellationToken.None);
        var stabilization = await session.StabilizeAsync(CancellationToken.None);
        var evidence = await session.ExtractRenderedEvidenceAsync(CancellationToken.None);

        Assert.Contains(".product-card", stabilization.HiddenNoiseSelectors);
        Assert.DoesNotContain(evidence.Boxes, box => box.Selector.Contains("product-card", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Playwright")]
    public async Task Playwright_HttpFixture_ExcludesOffscreenAccessibilityHelpersFromRenderedEvidence()
    {
        await using var server = await StartServerFromHtmlAsync(
            """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <title>Offscreen helper fixture</title>
              <style>
                body { margin: 0; font-family: Inter, Arial, sans-serif; }
                .skip-to-content-link { position: absolute; left: -99999px; top: 0; width: 152px; height: 52px; }
                .skip-to-content-link:focus { left: 16px; }
                .visually-hidden { position: absolute; left: -99999px; width: 1px; height: 1px; overflow: hidden; }
                .site-header { display: flex; height: 72px; align-items: center; }
                .hero { display: grid; min-height: 360px; background: #eef6f3; }
                .product-card { width: 320px; min-height: 240px; border-radius: 8px; }
              </style>
            </head>
            <body>
              <a class="skip-to-content-link button-secondary" href="#main">Skip to content</a>
              <div class="visually-hidden" role="status" aria-live="polite">Cart updated</div>
              <header class="site-header"><strong>Fixture Brand</strong><nav><a href="/shop">Shop</a></nav></header>
              <main id="main">
                <section class="hero"><h1>Visible hero heading</h1></section>
                <section class="product-grid" aria-label="Products"><article class="product-card"><h2>Visible Product</h2><button>Add</button></article></section>
              </main>
            </body>
            </html>
            """);
        var browser = new PlaywrightReferenceBrowser();

        var capture = await browser.CaptureAsync(
            new BrowserPageSession("playwright-offscreen", "home", server.BaseUrl),
            ViewportDefinition.Defaults[0],
            new CapturePolicy(MaximumEvidenceElements: 20),
            CancellationToken.None);

        Assert.DoesNotContain(capture.Boxes, box => box.Selector.Contains("skip-to-content-link", StringComparison.Ordinal));
        Assert.DoesNotContain(capture.Styles, style => style.Selector.Contains("skip-to-content-link", StringComparison.Ordinal));
        Assert.DoesNotContain(capture.Boxes, box => box.Selector.Contains("visually-hidden", StringComparison.Ordinal));
        Assert.Contains(capture.Boxes, box => box.Selector.Contains("header.site-header", StringComparison.Ordinal));
        Assert.Contains(capture.Boxes, box => box.Selector.Contains("section.hero", StringComparison.Ordinal));
        Assert.Contains(capture.Boxes, box => box.Selector.Contains("article.product-card", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Playwright")]
    public async Task Playwright_HttpFixture_StitchedFallbackCreatesRealImage()
    {
        await using var server = await StartServerAsync();
        var repoRoot = GetRepoRoot();
        var projectRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "playwright-stitch-" + Guid.NewGuid().ToString("N"));
        var viewportRoot = Path.Combine(projectRoot, "captures", "home", "desktop-1440");
        Directory.CreateDirectory(viewportRoot);
        var viewport = ViewportDefinition.Defaults[0];

        var result = await new StableFullPageCaptureService(new PlaywrightReferenceBrowser())
            .CaptureAsync(
                new BrowserPageSession("playwright", "home", server.BaseUrl),
                viewport,
                new CapturePolicy(PreserveViewportSegments: true),
                forceStitchedFallback: true,
                CancellationToken.None,
                viewportRoot,
                $"obj/storefront-reverse-engineering/projects/{Path.GetFileName(projectRoot)}/captures/home/desktop-1440");

        using var image = new MagickImage(result.Capture.ScreenshotPng);
        Assert.Equal("stitched", result.Capture.CaptureMethod);
        Assert.NotEmpty(result.Segments);
        Assert.Equal((uint)viewport.Width, image.Width);
        Assert.Equal((uint)result.Capture.DocumentHeight, image.Height);
        Assert.True(File.Exists(Path.Combine(viewportRoot, "stitch-manifest.json")));
    }

    [Fact]
    [Trait("Category", "Playwright")]
    public async Task Playwright_HttpFixture_InteractionBeforeAfterDiffers()
    {
        await using var server = await StartServerAsync();
        var evidence = await new InteractionCaptureService(GetRepoRoot(), new PlaywrightReferenceBrowser())
            .CaptureAsync(
                Path.Combine("obj", "storefront-reverse-engineering", "projects", "playwright-interaction-" + Guid.NewGuid().ToString("N")),
                new BrowserPageSession("playwright", "home", server.BaseUrl),
                ViewportDefinition.Defaults.Single(candidate => candidate.Id == "mobile-390"),
                new CapturePolicy(),
                new InteractionCapturePlan("mobile-menu-open", [new InteractionActionDefinition(InteractionActionType.ClickSelector, ".mobile-menu")]),
                CancellationToken.None);

        Assert.True(evidence.DomChanged);
        Assert.True(evidence.ScreenshotChanged);
        Assert.Empty(evidence.Errors);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task Playwright_HttpFixture_FullWorkflowProducesReadinessPass()
    {
        await using var server = await StartServerAsync();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "playwright-e2e-" + Guid.NewGuid().ToString("N"));

        var summary = await new VisualProjectWorkflowService(GetRepoRoot())
            .RunAsync(server.BaseUrl, "Playwright Fixture", outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "playwright-e2e");

        Assert.True(summary.ReadinessPassed);
        Assert.Equal("playwright-e2e", summary.RunId);
        Assert.True(File.Exists(Path.Combine(summary.ArtifactRoot, "runs", "playwright-e2e.json")));
    }

    private static async Task<TestHttpFixtureServer> StartServerAsync()
    {
        var fixturePath = Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        return await TestHttpFixtureServer.StartAsync(fixturePath);
    }

    private static async Task<TestHttpFixtureServer> StartServerFromHtmlAsync(string html)
    {
        var path = Path.Combine(Path.GetTempPath(), "sre-offscreen-helper-" + Guid.NewGuid().ToString("N") + ".html");
        await File.WriteAllTextAsync(path, html);
        return await TestHttpFixtureServer.StartAsync(path);
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
}
