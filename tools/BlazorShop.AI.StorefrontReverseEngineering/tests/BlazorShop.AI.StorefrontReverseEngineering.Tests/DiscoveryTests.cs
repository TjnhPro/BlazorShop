using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class DiscoveryTests
{
    [Fact]
    public async Task Discover_Fixture_WritesProfileReconnaissanceAndCapturePlan()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = CreateOutputRoot();
        var fixturePath = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html");
        var project = await new VisualProjectService(repoRoot).InitializeAsync(new Uri(fixturePath).AbsoluteUri, "Discover Fixture", outputRoot, false, CancellationToken.None);

        var result = await new VisualDiscoveryService(repoRoot, new FixtureReferenceBrowser())
            .DiscoverAsync(Path.Combine(outputRoot, project.ProjectId), CancellationToken.None);

        Assert.Equal("Fixture Storefront", result.SiteProfile.Title);
        Assert.Contains(result.Reconnaissance.Blockers, blocker => blocker.Code == "overlay-detected");
        Assert.Equal(3, result.CapturePlan.Viewports.Count);
        Assert.True(File.Exists(Path.Combine(project.ArtifactRoot, "discovery", "site-profile.json")));
        Assert.True(File.Exists(Path.Combine(project.ArtifactRoot, "discovery", "reconnaissance.json")));
        Assert.True(File.Exists(Path.Combine(project.ArtifactRoot, "discovery", "capture-plan.json")));
    }

    [Fact]
    public async Task Discover_DetectsBlockers()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = CreateOutputRoot();
        var project = await new VisualProjectService(repoRoot).InitializeAsync("https://example.test", "Blocker Fixture", outputRoot, false, CancellationToken.None);

        var result = await new VisualDiscoveryService(repoRoot, new BlockingReferenceBrowser())
            .DiscoverAsync(Path.Combine(outputRoot, project.ProjectId), CancellationToken.None);

        Assert.Contains(result.Reconnaissance.Blockers, blocker => blocker.Code == "authentication-wall");
        Assert.Contains(result.Reconnaissance.Blockers, blocker => blocker.Code == "excessive-page-height");
    }

    private static string CreateOutputRoot() =>
        Path.Combine("obj", "storefront-reverse-engineering", "projects", "discovery-test-" + Guid.NewGuid().ToString("N"));

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class BlockingReferenceBrowser : IReferenceBrowser
    {
        public Task<BrowserCaptureResult> CaptureAsync(BrowserPageSession session, ViewportDefinition viewport, CapturePolicy policy, CancellationToken cancellationToken)
        {
            return Task.FromResult(new BrowserCaptureResult(
                "test",
                "native-full-page",
                viewport.Width,
                viewport.Height,
                viewport.Width,
                policy.MaximumPageHeight,
                "<html lang=\"en\"><head><title>Login</title><meta name=\"robots\" content=\"noindex\"></head><body><main>Sign in required</main></body></html>",
                [],
                [],
                [],
                [],
                []));
        }
    }
}
