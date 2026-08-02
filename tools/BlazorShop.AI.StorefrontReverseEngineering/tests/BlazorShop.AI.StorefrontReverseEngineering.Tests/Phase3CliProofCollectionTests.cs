using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "ClosureProof")]
public sealed class Phase3CliProofCollectionTests
{
    [Fact]
    public async Task CliProofCollection_CoversHomeListingProductAndUnsupportedRoutes()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "phase3-cli-proof-" + Guid.NewGuid().ToString("N"));
        Phase3TempPathRegistry.Register(Path.Combine(repoRoot, outputRoot));

        var routes = new[]
        {
            new RouteFixture("home", "phase3b-home.html", false),
            new RouteFixture("listing", "phase3b-plp.html", false),
            new RouteFixture("product", "phase3b-pdp.html", false),
            new RouteFixture("unsupported", "phase3b-unsupported.html", true)
        };

        foreach (var route in routes)
        {
            var projectId = "phase3-cli-proof-" + route.Id;
            var runId = projectId + "-run";
            var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", route.FileName)).AbsoluteUri;
            using var runOut = new StringWriter();
            using var runErr = new StringWriter();

            var exitCode = await CliHost.RunAsync(
                ["run", "--url", fixtureUrl, "--name", projectId, "--output-root", outputRoot, "--no-ai", "--force", "--run-id", runId],
                runOut,
                runErr,
                CancellationToken.None);

            var projectRoot = Path.Combine(repoRoot, outputRoot, projectId);
            Assert.Equal(3, exitCode);
            Assert.Contains("Run status: Failed", runOut.ToString(), StringComparison.Ordinal);
            AssertStrictReviewBlockers(projectRoot, runId);
            await AssertInspectAsync(projectRoot, route.Id);

            if (route.ExpectUnsupportedPattern)
            {
                AssertUnsupportedPatterns(projectRoot);
            }
        }
    }

    private static void AssertStrictReviewBlockers(string projectRoot, string runId)
    {
        var readiness = JsonSerializer.Deserialize<ReadinessReport>(
            File.ReadAllText(Path.Combine(projectRoot, "reports", "readiness-report.json")),
            VisualJson.Options)!;
        Assert.True(readiness.Passed);

        var run = JsonSerializer.Deserialize<WorkflowRun>(
            File.ReadAllText(Path.Combine(projectRoot, "runs", runId + ".json")),
            VisualJson.Options)!;
        var codes = run.Steps.SelectMany(step => step.Errors).Select(error => error.Code).ToArray();
        Assert.Contains("missing-review-decisions", codes);
        Assert.Contains("reviewed-blueprint-not-resolved", codes);
    }

    private static async Task AssertInspectAsync(string projectRoot, string routeId)
    {
        using var inspectOut = new StringWriter();
        using var inspectErr = new StringWriter();

        var exitCode = await CliHost.RunAsync(["inspect", "--project", projectRoot], inspectOut, inspectErr, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("", inspectErr.ToString());
        Assert.Contains("Phase 3B artifacts:", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Latest final blocker: reviewed-blueprint-not-resolved", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains(routeId, projectRoot, StringComparison.Ordinal);
    }

    private static void AssertUnsupportedPatterns(string projectRoot)
    {
        var path = Path.Combine(projectRoot, "analysis", "mapping", "unsupported-patterns.json");
        var patterns = JsonNode.Parse(File.ReadAllText(path))!["patterns"]!.AsArray();

        Assert.NotEmpty(patterns);
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

    private sealed record RouteFixture(string Id, string FileName, bool ExpectUnsupportedPattern);
}
