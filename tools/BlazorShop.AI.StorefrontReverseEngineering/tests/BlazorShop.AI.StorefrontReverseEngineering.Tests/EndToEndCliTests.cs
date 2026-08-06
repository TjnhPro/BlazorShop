using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
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

        Assert.Equal(3, exitCode);
        Assert.Contains("Run completed: fixture-demo", stdout.ToString(), StringComparison.Ordinal);
        Assert.Contains("Run status: Failed", stdout.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(repoRoot, outputRoot, "fixture-demo", "reports", "readiness-report.md")));
        Assert.True(File.Exists(Path.Combine(repoRoot, outputRoot, "fixture-demo", "analysis", "visual-blueprint.draft.json")));
    }

    [Fact]
    public async Task Inspect_AfterRun_ShowsWorkflowRunState()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "inspect-run-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        using var runOut = new StringWriter();
        using var runErr = new StringWriter();

        var runExit = await CliHost.RunAsync(
            ["run", "--url", fixtureUrl, "--name", "Inspect Demo", "--output-root", outputRoot, "--no-ai", "--force", "--run-id", "inspect-run"],
            runOut,
            runErr,
            CancellationToken.None);

        using var inspectOut = new StringWriter();
        using var inspectErr = new StringWriter();
        var projectRoot = Path.Combine(outputRoot, "inspect-demo");
        var inspectExit = await CliHost.RunAsync(
            ["inspect", "--project", projectRoot],
            inspectOut,
            inspectErr,
            CancellationToken.None);

        Assert.Equal(3, runExit);
        Assert.Equal(0, inspectExit);
        Assert.True(File.Exists(Path.Combine(repoRoot, projectRoot, "runs", "inspect-run.json")));
        Assert.Contains("Run status: Failed", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Latest run: inspect-run", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Latest run status: Failed", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Readiness passed: true", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Blocking findings: 0", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Warnings: 0", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Latest blocking finding: (none)", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Reviewed blueprint: missing", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Final handoff readiness: missing", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Latest final blocker: reviewed-blueprint-not-resolved", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Suggested fix: Complete review/review-decisions.json", inspectOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("capture-viewport-desktop-1440", inspectOut.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inspect_AfterFailedReadinessShowsBlockingSummary()
    {
        var projectRoot = await CreateReadyProjectAsync("Inspect Failed Readiness");
        await MutateJsonAsync(projectRoot, "captures/home/desktop-1440/capture-quality-report.json", json =>
        {
            json["passed"] = false;
        });

        var report = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);
        var inspectOutput = await RunInspectAsync(projectRoot);

        Assert.False(report.Passed);
        Assert.Contains("Readiness passed: false", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Blocking findings:", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Latest blocking finding: quality-failed -", inspectOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inspect_HandlesMissingLatestRunFile()
    {
        var projectRoot = await CreateReadyProjectAsync("Inspect Missing Run");
        File.Delete(Path.Combine(projectRoot, "runs", "readiness-fixture.json"));

        var inspectOutput = await RunInspectAsync(projectRoot);

        Assert.Contains("Latest run: readiness-fixture", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Latest run status: missing", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Readiness passed: true", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Steps: (unavailable)", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Inspection warning: Latest workflow run file is missing:", inspectOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inspect_HandlesInvalidLatestRunFile()
    {
        var projectRoot = await CreateReadyProjectAsync("Inspect Invalid Run");
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "runs", "readiness-fixture.json"), "{");

        var inspectOutput = await RunInspectAsync(projectRoot);

        Assert.Contains("Latest run: readiness-fixture", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Latest run status: invalid", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Steps: (unavailable)", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Inspection warning: Latest workflow run file is invalid:", inspectOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inspect_HandlesInvalidReadinessJson()
    {
        var projectRoot = await CreateReadyProjectAsync("Inspect Invalid Readiness");
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "reports", "readiness-report.json"), "{");

        var inspectOutput = await RunInspectAsync(projectRoot);

        Assert.Contains("Readiness passed: unknown", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Readiness summary: Readiness report invalid.", inspectOutput, StringComparison.Ordinal);
        Assert.Contains("Inspection warning: Readiness report is invalid:", inspectOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resume_CommandCanUseProjectPathAndRunId()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "resume-cli-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        using var runOut = new StringWriter();
        using var runErr = new StringWriter();
        var runExit = await CliHost.RunAsync(
            ["run", "--url", fixtureUrl, "--name", "Resume Cli", "--output-root", outputRoot, "--no-ai", "--force", "--run-id", "resume-cli-run"],
            runOut,
            runErr,
            CancellationToken.None);

        using var resumeOut = new StringWriter();
        using var resumeErr = new StringWriter();
        var resumeExit = await CliHost.RunAsync(
            ["resume", "--project", Path.Combine(outputRoot, "resume-cli"), "--run-id", "resume-cli-run", "--no-ai"],
            resumeOut,
            resumeErr,
            CancellationToken.None);

        Assert.Equal(3, runExit);
        Assert.Equal(3, resumeExit);
        Assert.Contains("Run ID: resume-cli-run", resumeOut.ToString(), StringComparison.Ordinal);
        Assert.Contains("Run status: Failed", resumeOut.ToString(), StringComparison.Ordinal);
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
    public async Task Readiness_QualityFailureBlocksAndRecoveryReturnsDraftReady()
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "readiness-recovery-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var service = new VisualProjectWorkflowService(repoRoot);
        var summary = await service.RunAsync(fixtureUrl, "Readiness Recovery", outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "readiness-run");
        var projectRoot = summary.ArtifactRoot;
        var qualityPath = Path.Combine(projectRoot, "captures", "home", "desktop-1440", "capture-quality-report.json");
        var quality = JsonNode.Parse(await File.ReadAllTextAsync(qualityPath))!.AsObject();

        quality["passed"] = false;
        await File.WriteAllTextAsync(qualityPath, quality.ToJsonString(VisualJson.Options));
        var failed = await service.ValidateAsync(projectRoot, CancellationToken.None);
        var failedProject = await new VisualProjectService(repoRoot).InspectAsync(projectRoot, CancellationToken.None);

        quality["passed"] = true;
        await File.WriteAllTextAsync(qualityPath, quality.ToJsonString(VisualJson.Options));
        var recovered = await service.ValidateAsync(projectRoot, CancellationToken.None);
        var recoveredProject = await new VisualProjectService(repoRoot).InspectAsync(projectRoot, CancellationToken.None);

        Assert.False(failed.Passed);
        Assert.Contains(failed.Findings, finding => finding.Code == "quality-failed");
        Assert.Equal(VisualProjectStatus.ValidationFailed, failedProject.Project.Status);
        Assert.True(recovered.Passed);
        Assert.Equal(VisualProjectStatus.DraftReady, recoveredProject.Project.Status);
    }

    [Fact]
    public async Task Readiness_EmptyElementEvidenceArrayFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Empty Evidence");
        await MutateJsonAsync(projectRoot, "captures/home/desktop-1440/element-evidence-index.json", json =>
        {
            json["elements"] = new JsonArray();
        });

        var report = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "empty-computed-style-evidence");
    }

    [Fact]
    public async Task Readiness_EmptyStyleGroupsFail()
    {
        var projectRoot = await CreateReadyProjectAsync("Empty Styles");
        await MutateJsonAsync(projectRoot, "captures/home/desktop-1440/element-evidence-index.json", json =>
        {
            foreach (var element in json["elements"]!.AsArray())
            {
                element!.AsObject()["styleGroups"] = new JsonObject();
            }
        });

        var report = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "empty-style-groups");
        Assert.Contains(report.Findings, finding => finding.Code == "missing-typography-evidence");
        Assert.Contains(report.Findings, finding => finding.Code == "missing-layout-evidence");
    }

    [Fact]
    public async Task Readiness_MissingUsefulBoxesAndInvalidBoxFail()
    {
        var projectRoot = await CreateReadyProjectAsync("Bad Boxes");
        await MutateFirstElementAsync(projectRoot, element =>
        {
            element["box"] = JsonNode.Parse("""{"x":0,"y":0,"width":0,"height":20}""");
        });

        var report = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "invalid-element-box");
    }

    [Fact]
    public async Task Readiness_StillBlocksInvalidVisibleElementBox()
    {
        var projectRoot = await CreateReadyProjectAsync("Invalid Visible Box");
        await MutateFirstElementAsync(projectRoot, element =>
        {
            element["box"] = JsonNode.Parse("""{"x":-99999,"y":0,"width":320,"height":120}""");
        });

        var report = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "invalid-element-box");
    }

    [Fact]
    public async Task Readiness_MissingAndMismatchedCorrelationFail()
    {
        var missingProjectRoot = await CreateReadyProjectAsync("Missing Correlation");
        await MutateJsonAsync(missingProjectRoot, "captures/home/desktop-1440/element-evidence-index.json", json =>
        {
            json["captureCorrelationId"] = null;
        });
        var missing = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(missingProjectRoot, CancellationToken.None);

        var mismatchProjectRoot = await CreateReadyProjectAsync("Mismatch Correlation");
        await MutateJsonAsync(mismatchProjectRoot, "captures/home/desktop-1440/asset-inventory.normalized.json", json =>
        {
            json["captureCorrelationId"] = "wrong-correlation";
        });
        var mismatch = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(mismatchProjectRoot, CancellationToken.None);

        Assert.Contains(missing.Findings, finding => finding.Code == "missing-capture-correlation");
        Assert.Contains(mismatch.Findings, finding => finding.Code == "capture-correlation-mismatch");
    }

    [Fact]
    public async Task Readiness_EmptyOriginalityRestrictionsFail()
    {
        var projectRoot = await CreateReadyProjectAsync("Empty Restrictions");
        await MutateJsonAsync(projectRoot, "analysis/originality-audit.json", json =>
        {
            json["generationRestrictions"] = new JsonArray();
        });

        var report = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "empty-generation-restrictions");
    }

    [Fact]
    public async Task Readiness_StitchedMethodWithoutManifestFails()
    {
        var projectRoot = await CreateReadyProjectAsync("Missing Stitch Manifest");
        await MutateJsonAsync(projectRoot, "captures/home/desktop-1440/capture-quality-report.json", json =>
        {
            json["captureMethod"] = "stitched";
            json["finalMethod"] = "stitched";
            json["segmentCount"] = 1;
            json["fallbackReason"] = "test-stitch";
            json["passed"] = true;
        });

        var report = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(projectRoot, CancellationToken.None);

        Assert.False(report.Passed);
        Assert.Contains(report.Findings, finding => finding.Code == "missing-stitch-manifest");
    }

    [Fact]
    public async Task Readiness_FailedAndPartialLatestRunFail()
    {
        var failedProjectRoot = await CreateReadyProjectAsync("Failed Run");
        await MutateJsonAsync(failedProjectRoot, "runs/readiness-fixture.json", json =>
        {
            json["status"] = WorkflowRunStatus.Failed.ToString();
            json["steps"]!.AsArray()[0]!["status"] = WorkflowStepStatus.Failed.ToString();
        });
        var failed = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(failedProjectRoot, CancellationToken.None);

        var partialProjectRoot = await CreateReadyProjectAsync("Partial Run");
        await MutateJsonAsync(partialProjectRoot, "runs/readiness-fixture.json", json =>
        {
            json["status"] = WorkflowRunStatus.Running.ToString();
            json["steps"]!.AsArray()[0]!["status"] = WorkflowStepStatus.Pending.ToString();
        });
        var partial = await new VisualProjectWorkflowService(GetRepoRoot()).ValidateAsync(partialProjectRoot, CancellationToken.None);

        Assert.Contains(failed.Findings, finding => finding.Code == "failed-latest-run");
        Assert.Contains(partial.Findings, finding => finding.Code == "partial-latest-run");
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

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "readiness-mutation-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "readiness-fixture");

        Assert.True(summary.ReadinessPassed);
        return summary.ArtifactRoot;
    }

    private static async Task MutateFirstElementAsync(string projectRoot, Action<JsonObject> mutate)
    {
        await MutateJsonAsync(projectRoot, "captures/home/desktop-1440/element-evidence-index.json", json =>
        {
            mutate(json["elements"]!.AsArray()[0]!.AsObject());
        });
    }

    private static async Task MutateJsonAsync(string projectRoot, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        mutate(json);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
    }

    private static async Task<string> RunInspectAsync(string projectRoot)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = await CliHost.RunAsync(
            ["inspect", "--project", projectRoot],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("", stderr.ToString());
        return stdout.ToString();
    }

    private sealed class TimeoutReferenceBrowser : IReferenceBrowser
    {
        public Task<BrowserCaptureResult> CaptureAsync(BrowserPageSession session, ViewportDefinition viewport, CapturePolicy policy, CancellationToken cancellationToken)
        {
            throw new TimeoutException("Simulated timeout.");
        }
    }
}
