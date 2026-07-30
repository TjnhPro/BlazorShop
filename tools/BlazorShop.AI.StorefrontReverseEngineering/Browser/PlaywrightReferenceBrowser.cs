using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Microsoft.Playwright;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed class PlaywrightReferenceBrowser : ReferenceBrowserBase
{
    public override async Task<IReferenceBrowserSession> OpenSessionAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Timeout = policy.TimeoutMilliseconds
        });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = viewport.Width, Height = viewport.Height },
            DeviceScaleFactor = (float)viewport.DeviceScaleFactor,
            IsMobile = viewport.IsMobile
        });
        var page = await context.NewPageAsync();

        return new PlaywrightReferenceBrowserSession(playwright, browser, context, page, session, viewport, policy);
    }

    private sealed class PlaywrightReferenceBrowserSession : IReferenceBrowserSession
    {
        private const int MaximumEvidenceElements = 80;
        private const int MaximumEvidenceAssets = 80;
        private const int MaximumTextLength = 160;

        private readonly IPlaywright playwright;
        private readonly IBrowser browser;
        private readonly IBrowserContext context;
        private readonly IPage page;
        private readonly BrowserPageSession session;
        private readonly ViewportDefinition viewport;
        private readonly CapturePolicy policy;
        private bool navigated;

        public PlaywrightReferenceBrowserSession(
            IPlaywright playwright,
            IBrowser browser,
            IBrowserContext context,
            IPage page,
            BrowserPageSession session,
            ViewportDefinition viewport,
            CapturePolicy policy)
        {
            this.playwright = playwright;
            this.browser = browser;
            this.context = context;
            this.page = page;
            this.session = session;
            this.viewport = viewport;
            this.policy = policy;
            SessionId = $"pw-{Guid.NewGuid():N}";
        }

        public string SessionId { get; }

        public async Task NavigateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await page.GotoAsync(session.SourceUrl, new PageGotoOptions
            {
                Timeout = policy.TimeoutMilliseconds,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            navigated = true;
        }

        public async Task<PageStabilizationReport> StabilizeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNavigated();

            var steps = new List<string> { "wait-dom-ready" };
            var warnings = new List<string>();
            var hiddenNoiseSelectors = new List<string>();

            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = Math.Min(policy.TimeoutMilliseconds, 5000)
                });
                steps.Add("wait-network-idle");
            }
            catch (TimeoutException)
            {
                steps.Add("wait-network-idle-fallback");
                warnings.Add("Network idle wait timed out; capture continued after DOMContentLoaded.");
            }

            await page.EvaluateAsync(
                "() => document.fonts && document.fonts.ready ? document.fonts.ready.then(() => true) : true");
            steps.Add("wait-fonts-when-available");

            await page.EvaluateAsync(
                "() => Promise.all(Array.from(document.images).slice(0, 80).map(img => img.complete ? true : new Promise(resolve => { img.addEventListener('load', resolve, { once: true }); img.addEventListener('error', resolve, { once: true }); setTimeout(resolve, 2500); })))");
            steps.Add("wait-important-images");

            await page.AddStyleTagAsync(new PageAddStyleTagOptions
            {
                Content = "*,*::before,*::after{animation-duration:0.001s!important;animation-delay:0s!important;transition-duration:0.001s!important;scroll-behavior:auto!important}"
            });
            steps.Add("inject-reduced-motion-capture-style");

            if (!policy.StrictWarnings)
            {
                hiddenNoiseSelectors.Add(".cookie-banner");
                hiddenNoiseSelectors.Add("[data-capture-noise]");
                await page.EvaluateAsync(
                    "selectors => selectors.forEach(selector => document.querySelectorAll(selector).forEach(element => { element.setAttribute('data-sre-hidden-noise', 'true'); element.style.setProperty('display', 'none', 'important'); }))",
                    hiddenNoiseSelectors);
                steps.Add("hide-configured-noise-selectors");
            }

            var metrics = await GetMetricsAsync(cancellationToken);
            var stepHeight = Math.Max(1, viewport.Height);
            for (var y = 0; y < metrics.DocumentHeight; y += stepHeight)
            {
                await page.EvaluateAsync("y => window.scrollTo(0, y)", y);
                await page.WaitForTimeoutAsync(100);
            }

            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await page.WaitForTimeoutAsync(150);
            steps.Add("warm-scroll-down-up");

            return new PageStabilizationReport(steps, hiddenNoiseSelectors, warnings);
        }

        public async Task<BrowserCaptureResult> CaptureCurrentStateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNavigated();

            var evidence = await page.EvaluateAsync<RenderedPageEvidence>(
                EvidenceScript,
                new EvidenceCaptureOptions(MaximumEvidenceElements, MaximumEvidenceAssets, MaximumTextLength));

            if (evidence.DocumentHeight > policy.MaximumPageHeight)
            {
                throw new InvalidOperationException($"[SRE-BROWSER-007] Captured page exceeds maximum height. Problem: '{session.SourceUrl}' is {evidence.DocumentHeight}px tall. Cause: capture policy limits evidence size to {policy.MaximumPageHeight}px. Fix: increase maximum height after review or capture a narrower page.");
            }

            var dom = await page.ContentAsync();
            var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = true,
                Type = ScreenshotType.Png
            });

            return new BrowserCaptureResult(
                "playwright-chromium",
                "native-full-page",
                viewport.Width,
                viewport.Height,
                evidence.DocumentWidth,
                evidence.DocumentHeight,
                dom,
                screenshot,
                evidence.Styles.Select(style => new ComputedStyleSample(style.Selector, style.Properties, style.EvidenceId)).ToArray(),
                evidence.Boxes.Select(box => new ElementBoxSample(box.Selector, box.X, box.Y, box.Width, box.Height, box.EvidenceId)).ToArray(),
                evidence.Assets.Select(asset => new AssetInventoryItem(asset.Url, asset.MediaType, asset.Width, asset.Height, asset.SourceElement, true, asset.EvidenceId)).ToArray(),
                evidence.Warnings);
        }

        public async Task<BrowserActionResult> ExecuteAsync(BrowserSessionAction action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNavigated();

            if (string.Equals(action.Type, "wait", StringComparison.OrdinalIgnoreCase))
            {
                await page.WaitForTimeoutAsync(action.DelayMilliseconds ?? 250);
                return new BrowserActionResult(true, []);
            }

            var beforeUri = new Uri(page.Url);
            if (string.Equals(action.Type, "click-selector", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator(action.Selector!).First.ClickAsync(new LocatorClickOptions
                {
                    Timeout = policy.TimeoutMilliseconds
                });
            }
            else if (string.Equals(action.Type, "hover-selector", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator(action.Selector!).First.HoverAsync(new LocatorHoverOptions { Timeout = policy.TimeoutMilliseconds });
            }
            else if (string.Equals(action.Type, "focus-selector", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator(action.Selector!).First.FocusAsync(new LocatorFocusOptions { Timeout = policy.TimeoutMilliseconds });
            }
            else if (string.Equals(action.Type, "scroll-to-selector", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator(action.Selector!).First.ScrollIntoViewIfNeededAsync(new LocatorScrollIntoViewIfNeededOptions { Timeout = policy.TimeoutMilliseconds });
            }
            else if (string.Equals(action.Type, "key-press", StringComparison.OrdinalIgnoreCase))
            {
                await page.Keyboard.PressAsync(action.Key!, new KeyboardPressOptions { Delay = 10 });
            }
            else if (string.Equals(action.Type, "scroll-to-y", StringComparison.OrdinalIgnoreCase))
            {
                await page.EvaluateAsync("y => window.scrollTo(0, y)", action.ScrollY ?? 0);
            }
            else
            {
                return new BrowserActionResult(false, [$"Unsupported browser action '{action.Type}'."]);
            }

            await page.WaitForTimeoutAsync(action.DelayMilliseconds ?? 150);
            var afterUri = new Uri(page.Url);
            if (!string.Equals(beforeUri.Host, afterUri.Host, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"[SRE-INTERACTION-004] External navigation refused. Problem: interaction moved from '{beforeUri.Host}' to '{afterUri.Host}'. Cause: Phase 3A interactions must stay within the allowed reference origin. Fix: remove or replace the selector.");
            }

            return new BrowserActionResult(true, []);
        }

        public Task<byte[]> CaptureViewportScreenshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNavigated();
            return page.ScreenshotAsync(new PageScreenshotOptions
            {
                FullPage = false,
                Type = ScreenshotType.Png
            });
        }

        public async Task<BrowserDocumentMetrics> GetMetricsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureNavigated();
            var metrics = await page.EvaluateAsync<RenderedDocumentMetrics>(
                "() => ({ DocumentWidth: Math.ceil(Math.max(document.documentElement.scrollWidth, document.body ? document.body.scrollWidth : 0, window.innerWidth)), DocumentHeight: Math.ceil(Math.max(document.documentElement.scrollHeight, document.body ? document.body.scrollHeight : 0, window.innerHeight)), ViewportWidth: window.innerWidth, ViewportHeight: window.innerHeight })");

            return new BrowserDocumentMetrics(metrics.DocumentWidth, metrics.DocumentHeight, metrics.ViewportWidth, metrics.ViewportHeight);
        }

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await browser.DisposeAsync();
            playwright.Dispose();
        }

        private void EnsureNavigated()
        {
            if (!navigated)
            {
                throw new InvalidOperationException("[SRE-BROWSER-008] Browser session has not navigated. Problem: capture was requested before navigation. Cause: session lifecycle steps ran out of order. Fix: call NavigateAsync before stabilization or capture.");
            }
        }
    }

    private sealed record EvidenceCaptureOptions(int MaximumElements, int MaximumAssets, int MaximumTextLength);

    private sealed class RenderedPageEvidence
    {
        public int DocumentWidth { get; set; }

        public int DocumentHeight { get; set; }

        public List<RenderedStyleSample> Styles { get; set; } = [];

        public List<RenderedBoxSample> Boxes { get; set; } = [];

        public List<RenderedAssetSample> Assets { get; set; } = [];

        public List<string> Warnings { get; set; } = [];
    }

    private sealed class RenderedDocumentMetrics
    {
        public int DocumentWidth { get; set; }

        public int DocumentHeight { get; set; }

        public int ViewportWidth { get; set; }

        public int ViewportHeight { get; set; }
    }

    private sealed class RenderedStyleSample
    {
        public string EvidenceId { get; set; } = "";

        public string Selector { get; set; } = "";

        public Dictionary<string, string> Properties { get; set; } = [];
    }

    private sealed class RenderedBoxSample
    {
        public string EvidenceId { get; set; } = "";

        public string Selector { get; set; } = "";

        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }
    }

    private sealed class RenderedAssetSample
    {
        public string EvidenceId { get; set; } = "";

        public string Url { get; set; } = "";

        public string MediaType { get; set; } = "";

        public int? Width { get; set; }

        public int? Height { get; set; }

        public string SourceElement { get; set; } = "";
    }

    private const string EvidenceScript =
        """
        options => {
          const maxElements = Math.max(1, options.MaximumElements || 80);
          const maxAssets = Math.max(1, options.MaximumAssets || 80);
          const maxText = Math.max(20, options.MaximumTextLength || 160);
          const styleNames = [
            'font-family','font-size','font-weight','line-height','color','background','background-color',
            'border','border-radius','box-shadow','display','grid-template-columns','flex-direction','gap',
            'position','top','left','z-index','overflow','object-fit','transform','transition'
          ];
          const dynamicClass = value => /(^|[-_])[a-f0-9]{6,}($|[-_])/i.test(value) || /css-[a-z0-9]{5,}/i.test(value);
          const cssEscape = value => window.CSS && CSS.escape ? CSS.escape(value) : value.replace(/[^a-zA-Z0-9_-]/g, '\\$&');
          const domPath = element => {
            const parts = [];
            let current = element;
            while (current && current.nodeType === Node.ELEMENT_NODE && current !== document.documentElement && parts.length < 8) {
              const tag = current.tagName.toLowerCase();
              const parent = current.parentElement;
              if (!parent) {
                parts.unshift(tag);
                break;
              }
              const siblings = Array.from(parent.children).filter(child => child.tagName === current.tagName);
              const index = siblings.indexOf(current) + 1;
              parts.unshift(`${tag}:nth-of-type(${index})`);
              current = parent;
            }
            return parts.join(' > ');
          };
          const stableSelector = element => {
            if (element.id) {
              return `#${cssEscape(element.id)}`;
            }
            for (const name of ['data-testid','data-test','data-component','data-section','data-role']) {
              const value = element.getAttribute(name);
              if (value) {
                return `[${name}="${value.replace(/"/g, '\\"')}"]`;
              }
            }
            const tag = element.tagName.toLowerCase();
            const classes = Array.from(element.classList || []).filter(name => !dynamicClass(name)).slice(0, 2);
            if (classes.length > 0) {
              return `${tag}.${classes.map(cssEscape).join('.')}`;
            }
            const role = element.getAttribute('role');
            if (role) {
              return `${tag}[role="${role.replace(/"/g, '\\"')}"]`;
            }
            return domPath(element) || tag;
          };
          const interesting = element => {
            const tag = element.tagName.toLowerCase();
            if (['script','style','template','noscript'].includes(tag)) {
              return false;
            }
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            if (style.display === 'none' || style.visibility === 'hidden' || rect.width <= 0 || rect.height <= 0) {
              return false;
            }
            if (['header','main','footer','section','article','nav','aside','h1','h2','h3','h4','h5','h6','a','button','input','select','textarea','img','video','svg'].includes(tag)) {
              return true;
            }
            if (element.className && /\b(product|card|hero|grid|menu|accordion|banner|price|gallery)\b/i.test(String(element.className))) {
              return true;
            }
            const role = element.getAttribute('role');
            return !!role;
          };
          const elements = Array.from(document.querySelectorAll('body *')).filter(interesting).slice(0, maxElements);
          const styles = [];
          const boxes = [];
          const assets = [];
          const seenAssets = new Set();
          const addAsset = (url, mediaType, width, height, sourceSelector, evidenceId) => {
            if (!url || seenAssets.has(`${mediaType}|${url}`) || assets.length >= maxAssets) {
              return;
            }
            seenAssets.add(`${mediaType}|${url}`);
            assets.push({ EvidenceId: evidenceId, Url: url, MediaType: mediaType, Width: width || null, Height: height || null, SourceElement: sourceSelector });
          };
          elements.forEach((element, index) => {
            const evidenceId = `ev-${String(index + 1).padStart(3, '0')}`;
            const selector = stableSelector(element);
            const computed = getComputedStyle(element);
            const properties = {};
            styleNames.forEach(name => properties[name] = computed.getPropertyValue(name));
            const text = (element.innerText || element.textContent || '').replace(/\s+/g, ' ').trim().slice(0, maxText);
            if (text) {
              properties['text-snippet'] = text;
            }
            styles.push({ EvidenceId: evidenceId, Selector: selector, Properties: properties });
            const rect = element.getBoundingClientRect();
            boxes.push({
              EvidenceId: evidenceId,
              Selector: selector,
              X: Math.round((rect.left + window.scrollX) * 100) / 100,
              Y: Math.round((rect.top + window.scrollY) * 100) / 100,
              Width: Math.round(rect.width * 100) / 100,
              Height: Math.round(rect.height * 100) / 100
            });
            if (element.tagName.toLowerCase() === 'img') {
              addAsset(element.currentSrc || element.src, 'image', element.naturalWidth, element.naturalHeight, selector, evidenceId);
              if (element.getAttribute('srcset')) {
                addAsset(element.getAttribute('srcset'), 'image-srcset', null, null, selector, evidenceId);
              }
            }
            if (element.tagName.toLowerCase() === 'source') {
              addAsset(element.srcset || element.src, 'source', null, null, selector, evidenceId);
            }
            if (element.tagName.toLowerCase() === 'video') {
              addAsset(element.poster, 'video-poster', element.videoWidth, element.videoHeight, selector, evidenceId);
              Array.from(element.querySelectorAll('source')).forEach(source => addAsset(source.src, 'video', null, null, selector, evidenceId));
            }
            if (element.tagName.toLowerCase() === 'svg') {
              addAsset(selector, 'inline-svg', Math.round(rect.width), Math.round(rect.height), selector, evidenceId);
            }
            const background = computed.getPropertyValue('background-image');
            if (background && background !== 'none') {
              for (const match of background.matchAll(/url\(["']?([^"')]+)["']?\)/g)) {
                addAsset(match[1], 'css-background-image', Math.round(rect.width), Math.round(rect.height), selector, evidenceId);
              }
            }
            const fontFamily = computed.getPropertyValue('font-family');
            if (fontFamily) {
              addAsset(fontFamily, 'font-family', null, null, selector, evidenceId);
            }
          });
          const root = document.documentElement;
          const body = document.body;
          return {
            DocumentWidth: Math.ceil(Math.max(root.scrollWidth, body ? body.scrollWidth : 0, window.innerWidth)),
            DocumentHeight: Math.ceil(Math.max(root.scrollHeight, body ? body.scrollHeight : 0, window.innerHeight)),
            Styles: styles,
            Boxes: boxes,
            Assets: assets,
            Warnings: []
          };
        }
        """;
}
