using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed class SyntheticReferenceBrowser : IReferenceBrowser
{
    public Task<BrowserCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string html = """
            <!doctype html>
            <html lang="en">
            <head><title>Synthetic Reference</title><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="canonical" href="https://example.test/"></head>
            <body><header>Reference</header><main><section class="hero"><h1>Reference storefront</h1></section><div class="cookie-banner">Cookie notice</div></main></body>
            </html>
            """;

        return Task.FromResult(new BrowserCaptureResult(
            "synthetic-reference",
            "native-full-page",
            viewport.Width,
            viewport.Height,
            viewport.Width,
            viewport.Height,
            html,
            [],
            [],
            [],
            [],
            []));
    }
}
