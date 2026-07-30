using System.Diagnostics;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Browser;

public sealed class NodePlaywrightReferenceBrowser : IReferenceBrowser
{
    private readonly string repoRoot;

    public NodePlaywrightReferenceBrowser(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
    }

    public async Task<BrowserCaptureResult> CaptureAsync(
        BrowserPageSession session,
        ViewportDefinition viewport,
        CapturePolicy policy,
        CancellationToken cancellationToken)
    {
        var script = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "capture", "capture-storefront.mjs");
        if (!File.Exists(script))
        {
            throw new InvalidOperationException($"[SRE-BROWSER-002] Node Playwright bridge is unavailable. Problem: '{script}' was not found. Cause: Phase 3A wraps the existing StorefrontBuilder capture script for initial parity. Fix: restore StorefrontBuilder scripts or use the fixture browser in tests.");
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "node",
            WorkingDirectory = repoRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            ArgumentList =
            {
                script,
                "--url",
                session.SourceUrl,
                "--output",
                Path.Combine("obj", "storefront-reverse-engineering", "node-bridge", session.ProjectId, session.PageId, viewport.Id),
                "--viewport",
                $"{viewport.Width}x{viewport.Height}"
            }
        }) ?? throw new InvalidOperationException("[SRE-BROWSER-003] Failed to start Node Playwright bridge.");

        var completed = await Task.Run(() => process.WaitForExit(policy.TimeoutMilliseconds), cancellationToken);
        if (!completed)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"[SRE-BROWSER-004] Browser capture timed out. Problem: '{session.SourceUrl}' did not finish within {policy.TimeoutMilliseconds}ms. Cause: reference page or browser dependencies may be unavailable. Fix: increase policy timeout after investigating page stability.");
        }

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"[SRE-BROWSER-005] Browser capture failed. Problem: Node bridge exited with {process.ExitCode}. Cause: Playwright dependencies or the reference URL may be unavailable. Fix: run npm install and npx playwright install chromium under StorefrontBuilder. Details: {stderr}");
        }

        throw new NotSupportedException("[SRE-BROWSER-006] Node bridge completed but direct result adaptation is deferred. Problem: Phase 3A keeps the bridge available while deterministic fixture capture owns automated tests. Cause: StorefrontBuilder script writes builder-shaped artifacts. Fix: use FixtureReferenceBrowser for tests or add adapter mapping in a later parity phase.");
    }
}
