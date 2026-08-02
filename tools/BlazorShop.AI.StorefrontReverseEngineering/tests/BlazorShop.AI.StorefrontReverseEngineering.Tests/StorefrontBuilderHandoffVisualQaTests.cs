using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderHandoffVisualQa")]
public sealed class StorefrontBuilderHandoffVisualQaTests
{
    private static readonly Lazy<Task<string>> HandoffProjectRoot = new(CreateHandoffProjectAsync);

    [Fact]
    public async Task HandoffVisualQa_PassesFixtureProofAcrossDesktopAndMobile()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("positive");

        var result = await RunVisualQaAsync(projectRoot, fixtureRoot, allowPlaceholders: true);

        Assert.True(result.ExitCode == 0, result.Output);
        var report = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "visual-qa-report.md"));
        Assert.Contains("Proof mode: skeleton", report, StringComparison.Ordinal);
        Assert.Contains("Handoff mode: true", report, StringComparison.Ordinal);
        Assert.Contains("Visual fidelity diff is not a hard gate for skeleton proof.", report, StringComparison.Ordinal);
        Assert.Contains("shell-home desktop-1440", report, StringComparison.Ordinal);
        Assert.Contains("shell-home mobile-390", report, StringComparison.Ordinal);
        Assert.Contains("product.purchase", report, StringComparison.Ordinal);
        Assert.Contains("Smoke result: pass", report, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_MissingGeneratedCssFails()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("missing-css", includeGeneratedCssLink: false);

        var result = await RunVisualQaAsync(projectRoot, fixtureRoot, allowPlaceholders: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Generated handoff CSS is not linked", await ReadReportAsync(projectRoot), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_BlankPageFails()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("blank", blankBody: true);

        var result = await RunVisualQaAsync(projectRoot, fixtureRoot, allowPlaceholders: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Hidden primary content or blank body", await ReadReportAsync(projectRoot), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_MissingRequiredSlotFails()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("missing-slot", includePurchaseSlot: false);

        var result = await RunVisualQaAsync(projectRoot, fixtureRoot, allowPlaceholders: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Required handoff slot 'product.purchase' is not visible", await ReadReportAsync(projectRoot), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_BrokenAssetFails()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("broken-asset", brokenImage: true);

        var result = await RunVisualQaAsync(projectRoot, fixtureRoot, allowPlaceholders: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Broken generated asset", await ReadReportAsync(projectRoot), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_RemovedBrowserActionDescriptorFails()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("missing-descriptor", includeCommandDescriptor: false);

        var result = await RunVisualQaAsync(projectRoot, fixtureRoot, allowPlaceholders: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Product purchase browser-action descriptor is missing", await ReadReportAsync(projectRoot), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_PlaceholderTextFailsUnlessAllowed()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("placeholder");

        var result = await RunVisualQaAsync(projectRoot, fixtureRoot, allowPlaceholders: false);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Generated-owned visual file still contains placeholder marker", await ReadReportAsync(projectRoot), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_RuntimeProofRequiresBaseUrl()
    {
        var projectRoot = await HandoffProjectRoot.Value;

        var result = await RunRuntimeVisualQaAsync(projectRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-VISUAL-QA-002", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_RuntimeProofRejectsFixtureRoot()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("runtime-rejects-fixture");

        var result = await RunRuntimeVisualQaAsync(projectRoot, baseUrl: "http://127.0.0.1:1", fixtureRoot: fixtureRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-VISUAL-QA-003", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffVisualQa_RuntimeProofBlocksDirectStorefrontApiCalls()
    {
        var projectRoot = await HandoffProjectRoot.Value;
        var fixtureRoot = await CreateVisualFixtureAsync("runtime-direct-api", directStorefrontApiCall: true);
        await using var server = await RuntimeVisualQaServer.StartAsync(fixtureRoot);

        var result = await RunRuntimeVisualQaAsync(projectRoot, server.BaseUrl);

        Assert.NotEqual(0, result.ExitCode);
        var report = await ReadReportAsync(projectRoot);
        Assert.Contains("Proof mode: runtime", report, StringComparison.Ordinal);
        Assert.Contains("## Runtime Route Statuses", report, StringComparison.Ordinal);
        Assert.Contains("Forbidden direct browser request", report, StringComparison.Ordinal);
    }

    private static async Task<string> CreateVisualFixtureAsync(
        string name,
        bool includeGeneratedCssLink = true,
        bool blankBody = false,
        bool includePurchaseSlot = true,
        bool brokenImage = false,
        bool includeCommandDescriptor = true,
        bool directStorefrontApiCall = false)
    {
        var root = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "visual-qa-fixtures", name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(
            Path.Combine(root, "storefront-builder.generated.css"),
            ".sfb-product-gallery{width:320px;aspect-ratio:1/1;background:#eee}.sfb-shell-header,.sfb-hero,.sfb-product-card,.sfb-product-page,.sfb-product-purchase,.sfb-fallback-page,footer{display:block;padding:8px}.wide{max-width:100%}");

        var cssLink = includeGeneratedCssLink ? "<link rel=\"stylesheet\" href=\"storefront-builder.generated.css\">" : "";
        var image = brokenImage
            ? "<img alt=\"fixture\" src=\"missing-product-image.png\">"
            : "<img alt=\"fixture\" src=\"data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%22100%22 height=%22100%22%3E%3Crect width=%22100%22 height=%22100%22 fill=%22%23ddd%22/%3E%3C/svg%3E\">";
        var command = includeCommandDescriptor ? "data-storefront-command=\"cart.add-line\"" : "";
        var directApiScript = directStorefrontApiCall ? "<script>fetch('/api/storefront/stores/sample/catalog').catch(() => {})</script>" : "";
        var purchase = includePurchaseSlot
            ? $"<aside class=\"sfb-product-purchase\" data-storefront-product-purchase><input data-storefront-purchase-quantity value=\"1\"><button {command} data-storefront-product-purchase-submit>Add</button></aside>"
            : "";
        var body = blankBody
            ? ""
            : $"<header class=\"sfb-shell-header\"><nav class=\"sfb-main-nav\">Nav</nav><span class=\"sfb-cart-badge\" data-storefront-cart-badge>0</span></header><main><h1 class=\"sfb-hero\">Visual Fixture</h1><section class=\"sfb-catalog-toolbar\">Filters</section><article class=\"sfb-product-card\">Card</article><article class=\"sfb-product-page\">Product info</article><section class=\"sfb-product-gallery\">{image}</section>{purchase}<section class=\"sfb-fallback-page\">Shell</section></main><footer>Footer</footer>";
        var html = $"<!doctype html><html><head><meta charset=\"utf-8\">{cssLink}<style>body{{font-family:Arial,sans-serif}}.sfb-product-gallery{{width:320px;aspect-ratio:1/1;background:#eee}}.sfb-shell-header,.sfb-hero,.sfb-product-card,.sfb-product-page,.sfb-product-purchase,.sfb-fallback-page,footer{{display:block;padding:8px}}</style><title>Fixture</title></head><body>{body}{directApiScript}</body></html>";

        foreach (var page in new[] { "shell-home", "catalog", "product", "cart", "checkout", "account", "state-pages" })
        {
            await File.WriteAllTextAsync(Path.Combine(root, $"{page}.html"), html);
        }

        return root;
    }

    private static async Task<string> CreateHandoffProjectAsync()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Visual QA");
        fixture.DeleteSourceProject();
        var outputRoot = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-visual-qa-tests", Guid.NewGuid().ToString("N"));
        const string projectName = "BlazorShop.Storefront.Phase4VisualQa";
        var result = await RunProcessAsync(
            "pwsh",
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "build-storefront.ps1"),
                "-Url",
                "https://example.test",
                "-Name",
                projectName,
                "-StoreKey",
                "sample",
                "-OutputRoot",
                outputRoot,
                "-Mode",
                "generate",
                "-HandoffRoot",
                fixture.PortableRoot,
                "-HandoffSchemaRoot",
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas"),
                "-Force"
            ],
            TimeSpan.FromMinutes(5));
        Assert.True(result.ExitCode == 0, result.Output);
        return Path.Combine(outputRoot, projectName);
    }

    private static Task<ProcessResult> RunVisualQaAsync(string projectRoot, string fixtureRoot, bool allowPlaceholders)
    {
        var args = new List<string>
        {
            Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "qa", "run-visual-qa.mjs"),
            "--proof-mode",
            "skeleton",
            "--project-root",
            projectRoot,
            "--fixture-root",
            fixtureRoot,
            "--screenshot-root",
            Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "visual-qa-screens", Guid.NewGuid().ToString("N"))
        };
        if (allowPlaceholders)
        {
            args.Add("--allow-planned-placeholders");
        }

        return RunProcessAsync("node", args, TimeSpan.FromMinutes(3));
    }

    private static Task<ProcessResult> RunRuntimeVisualQaAsync(string projectRoot, string? baseUrl = null, string? fixtureRoot = null)
    {
        var args = new List<string>
        {
            Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "qa", "run-visual-qa.mjs"),
            "--proof-mode",
            "runtime",
            "--project-root",
            projectRoot,
            "--screenshot-root",
            Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "visual-qa-screens", Guid.NewGuid().ToString("N")),
            "--allow-planned-placeholders"
        };

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            args.Add("--base-url");
            args.Add(baseUrl);
        }

        if (!string.IsNullOrWhiteSpace(fixtureRoot))
        {
            args.Add("--fixture-root");
            args.Add(fixtureRoot);
        }

        return RunProcessAsync("node", args, TimeSpan.FromMinutes(3));
    }

    private static Task<string> ReadReportAsync(string projectRoot) =>
        File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "visual-qa-report.md"));

    private static async Task<ProcessResult> RunProcessAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = GetRepoRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(timeout);
        await process.WaitForExitAsync(cts.Token);
        var output = (await stdoutTask) + (await stderrTask);
        return new ProcessResult(process.ExitCode, output);
    }

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();

    private sealed record ProcessResult(int ExitCode, string Output);

    private sealed class RuntimeVisualQaServer : IAsyncDisposable
    {
        private readonly HttpListener listener;
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task loop;
        private readonly string html;
        private readonly string css;

        private RuntimeVisualQaServer(HttpListener listener, string html, string css)
        {
            this.listener = listener;
            this.html = html;
            this.css = css;
            loop = Task.Run(ListenAsync);
        }

        public string BaseUrl { get; private init; } = "";

        public static Task<RuntimeVisualQaServer> StartAsync(string fixtureRoot)
        {
            var port = GetFreePort();
            var listener = new HttpListener();
            var baseUrl = $"http://127.0.0.1:{port}/";
            listener.Prefixes.Add(baseUrl);
            listener.Start();

            var server = new RuntimeVisualQaServer(
                listener,
                File.ReadAllText(Path.Combine(fixtureRoot, "shell-home.html")),
                File.ReadAllText(Path.Combine(fixtureRoot, "storefront-builder.generated.css")))
            {
                BaseUrl = baseUrl
            };
            return Task.FromResult(server);
        }

        public async ValueTask DisposeAsync()
        {
            cancellation.Cancel();
            listener.Stop();
            listener.Close();
            try
            {
                await loop;
            }
            catch (ObjectDisposedException)
            {
            }
            catch (HttpListenerException)
            {
            }
        }

        private async Task ListenAsync()
        {
            while (!cancellation.IsCancellationRequested)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => RespondAsync(context), cancellation.Token);
            }
        }

        private async Task RespondAsync(HttpListenerContext context)
        {
            try
            {
                var path = context.Request.Url?.AbsolutePath ?? "/";
                if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteAsync(context, "text/css; charset=utf-8", Encoding.UTF8.GetBytes(css));
                    return;
                }

                if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 204;
                    context.Response.Close();
                    return;
                }

                await WriteAsync(context, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html));
            }
            catch
            {
                if (context.Response.OutputStream.CanWrite)
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
            }
        }

        private static async Task WriteAsync(HttpListenerContext context, string contentType, byte[] bytes)
        {
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
