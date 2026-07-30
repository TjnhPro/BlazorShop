using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class EndToEndCliTests
{
    [Fact]
    public async Task Run_LocalFixtureFullWorkflow_Passes()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "e2e-test-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(
            ["run", "--url", fixtureUrl, "--name", "Fixture Demo", "--output-root", outputRoot, "--no-ai", "--force"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("Run completed: fixture-demo", stdout.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(repoRoot, outputRoot, "fixture-demo", "reports", "readiness-report.md")));
        Assert.True(File.Exists(Path.Combine(repoRoot, outputRoot, "fixture-demo", "analysis", "visual-blueprint.draft.json")));
    }

    [Fact]
    public async Task Validate_MissingArtifacts_ReturnsBlockingReport()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "validation-failure-" + Guid.NewGuid().ToString("N"));
        var project = await new VisualProjectService(repoRoot).InitializeAsync("https://example.test", "Validation Failure", outputRoot, false, CancellationToken.None);

        var report = await new VisualProjectWorkflowService(repoRoot).ValidateAsync(project.ArtifactRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "missing-artifact");
    }

    [Fact]
    public async Task Run_Resume_ReusesExistingProject()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "resume-test-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var service = new VisualProjectWorkflowService(repoRoot);

        var first = await service.RunAsync(fixtureUrl, "Resume Demo", outputRoot, force: true, resume: false, noAi: true, CancellationToken.None);
        var second = await service.RunAsync(fixtureUrl, "Resume Demo", outputRoot, force: false, resume: true, noAi: true, CancellationToken.None);

        Assert.Equal(first.ProjectId, second.ProjectId);
        Assert.True(second.ReadinessPassed);
    }

    [Fact]
    public async Task Run_TimeoutFailure_IsRecordedAsBlockingCaptureFailure()
    {
        var result = await new StableFullPageCaptureService(new TimeoutReferenceBrowser())
            .CaptureAsync(
                new BrowserPageSession("timeout", "home", "https://example.test"),
                ViewportDefinition.Defaults[0],
                new CapturePolicy(TimeoutMilliseconds: 1),
                forceStitchedFallback: false,
                CancellationToken.None);

        Assert.Equal("failed", result.Capture.CaptureMethod);
        Assert.False(result.QualityReport.Passed);
        Assert.Contains(result.QualityReport.Findings, finding => finding.Code == "capture-failed");
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

    private sealed class TimeoutReferenceBrowser : IReferenceBrowser
    {
        public Task<BrowserCaptureResult> CaptureAsync(BrowserPageSession session, ViewportDefinition viewport, CapturePolicy policy, CancellationToken cancellationToken)
        {
            throw new TimeoutException("Simulated timeout.");
        }
    }
}
