using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontPhase4MvpGateVisualQaContract")]
public sealed class StorefrontPhase4MvpGateVisualQaContractTests
{
    [Fact]
    public async Task MvpGate_RejectsMissingReferenceEvidenceReview()
    {
        var projectRoot = await CreateProjectAsync(report => report with { ReferenceEvidenceReviewed = false });

        var result = await RunGateAsync(projectRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("referenceEvidenceReviewed must be true", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MvpGate_RejectsMissingRuntimeEvidence()
    {
        var projectRoot = await CreateProjectAsync(report => report with { RuntimeEvidencePaths = [] });

        var result = await RunGateAsync(projectRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("runtimeEvidencePaths must contain at least one runtime evidence artifact", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MvpGate_RejectsUnacceptedMajorIssue()
    {
        var projectRoot = await CreateProjectAsync(report => report with
        {
            UnacceptedMajorCount = 1,
            Passed = false,
            FinalDecision = "failed"
        });

        var result = await RunGateAsync(projectRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("unaccepted major issue", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MvpGate_RejectsPassFlagWithNonzeroCounters()
    {
        var projectRoot = await CreateProjectAsync(report => report with { UnacceptedMajorCount = 1 });

        var result = await RunGateAsync(projectRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("says pass but unaccepted critical/major counters are nonzero", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MvpGate_RejectsMissingRequiredViewportCoverage()
    {
        var projectRoot = await CreateProjectAsync(report => report with
        {
            PageViewportCoverage = [new("home", ["desktop"])],
            ViewportCaptures = [new("home", "desktop", "docs/storefront-analysis/visual-qa/home-desktop.png", "passed")]
        });

        var result = await RunGateAsync(projectRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("pageViewportCoverage is missing required coverage", result.Output, StringComparison.Ordinal);
    }

    private static async Task<string> CreateProjectAsync(Func<VisualQaReportFixture, VisualQaReportFixture> mutate)
    {
        var repoRoot = GetRepoRoot();
        var projectName = "BlazorShop.Storefront.Phase4QaContractProbe";
        var projectRoot = Path.Combine(repoRoot, "obj", "storefront-builder", "phase4-mvp-gate-contract", Guid.NewGuid().ToString("N"), projectName);
        var analysisRoot = Path.Combine(projectRoot, "docs", "storefront-analysis");
        Directory.CreateDirectory(Path.Combine(projectRoot, "Features"));
        Directory.CreateDirectory(Path.Combine(analysisRoot, "agent-task-package"));
        Directory.CreateDirectory(Path.Combine(analysisRoot, "visual-checkpoints", "qa-contract"));
        Directory.CreateDirectory(Path.Combine(analysisRoot, "visual-qa"));

        await File.WriteAllTextAsync(Path.Combine(projectRoot, $"{projectName}.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"></Project>");
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "Features", "feature-manifest.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(analysisRoot, "metadata.yaml"), $"projectName: {projectName}\nstoreKey: sample\n");
        await WriteJsonAsync(Path.Combine(analysisRoot, "generation-plan.json"), new { schemaVersion = "0.1.0" });
        await WriteJsonAsync(Path.Combine(analysisRoot, "agent-task-package", "manifest.json"), new { schemaVersion = "0.1.0" });

        await WriteJsonAsync(Path.Combine(analysisRoot, "visual-plan.json"), new
        {
            schemaVersion = "0.1.0",
            operationId = "qa-contract",
            projectName,
            storeKey = "sample",
            handoffHash = "hash",
            generationPlanHash = "hash",
            taskPackageHash = "hash",
            pages = new[] { "home" },
            pageViewportCoverage = new[] { new Coverage("home", ["desktop", "tablet", "mobile"]) },
            visualSlots = Array.Empty<string>(),
            allowedFiles = new[] { "wwwroot/css/storefront-builder.generated.css" },
            plannedGeneratedOwnedFiles = new[] { "wwwroot/css/storefront-builder.generated.css" },
            protectedFiles = Array.Empty<string>(),
            implementationOrder = Array.Empty<string>(),
            risks = Array.Empty<string>(),
            blockers = Array.Empty<string>()
        });

        await WriteJsonAsync(Path.Combine(analysisRoot, "visual-implementation-checklist.json"), new
        {
            schemaVersion = "0.1.0",
            checklistId = "qa-contract",
            sourceVisualPlanHash = "hash",
            fileTasks = Array.Empty<object>(),
            acceptanceChecks = Array.Empty<object>(),
            requiredScreenshots = Array.Empty<object>(),
            forbiddenEdits = Array.Empty<object>()
        });

        await WriteJsonAsync(Path.Combine(analysisRoot, "visual-checkpoints", "qa-contract", "visual-checkpoint.json"), new
        {
            schemaVersion = "0.1.0",
            checkpointId = "qa-contract",
            operationId = "qa-contract",
            visualPlanHash = "hash",
            checklistHash = "hash",
            preEditSnapshotHash = "hash",
            postEditSnapshotHash = "hash",
            changedFiles = new[] { "wwwroot/css/storefront-builder.generated.css" },
            unexpectedFiles = Array.Empty<string>(),
            sourceTreeSnapshotScope = new[] { "wwwroot/css/storefront-builder.generated.css" },
            preEditFileHashes = new { },
            postEditFileHashes = new { },
            diffSummary = new { }
        });

        await WriteJsonAsync(Path.Combine(analysisRoot, "visual-implementation-report.json"), new
        {
            schemaVersion = "0.1.0",
            operationId = "qa-contract",
            checkpointPath = "docs/storefront-analysis/visual-checkpoints/qa-contract/visual-checkpoint.json",
            changedFiles = new[] { "wwwroot/css/storefront-builder.generated.css" },
            recorderResultPath = "docs/storefront-analysis/agent-written-files.json",
            boundaryResult = "passed",
            buildResult = "passed",
            unresolvedItems = Array.Empty<string>()
        });

        await WriteJsonAsync(Path.Combine(analysisRoot, "agent-written-files.json"), new
        {
            schemaVersion = "0.1.0",
            artifactKind = "storefront-builder.agent-written-files",
            artifactId = "qa-contract",
            detectionMode = "checkpoint-auto-detect",
            generationPlanHash = "hash",
            files = new[] { new { path = "wwwroot/css/storefront-builder.generated.css" } }
        });

        await File.WriteAllTextAsync(Path.Combine(analysisRoot, "visual-qa", "home-desktop.png"), "placeholder");
        await File.WriteAllTextAsync(Path.Combine(analysisRoot, "reference-home-desktop.png"), "placeholder");
        await WriteJsonAsync(Path.Combine(analysisRoot, "visual-qa-report.json"), mutate(VisualQaReportFixture.Valid()));

        return projectRoot;
    }

    private static async Task<ProcessResult> RunGateAsync(string projectRoot)
    {
        var result = await RunProcessAsync(
            "powershell",
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                Path.Combine(GetRepoRoot(), "scripts", "qa", "run-storefront-phase4-mvp-gate.ps1"),
                "-GeneratedProjectRoot",
                projectRoot,
                "-ProofMode",
                "Runtime",
                "-BaseUrl",
                "http://127.0.0.1:1",
                "-SkipRepair",
                "-CommandTimeoutSeconds",
                "5"
            ],
            TimeSpan.FromSeconds(30));
        return result;
    }

    private static Task WriteJsonAsync(string path, object value) =>
        File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

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
        return new ProcessResult(process.ExitCode, (await stdoutTask) + (await stderrTask));
    }

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();

    private sealed record Coverage(string PageId, string[] Viewports);

    private sealed record ViewportCapture(string PageId, string Viewport, string ScreenshotPath, string Status);

    private sealed record VisualQaReportFixture(
        string SchemaVersion,
        string OperationId,
        bool ReferenceEvidenceReviewed,
        string[] RuntimeEvidencePaths,
        string[] ReferenceEvidencePaths,
        Coverage[] PageViewportCoverage,
        string IndependentReviewer,
        string[] ComparisonDimensions,
        object[] AcceptedDifferences,
        int UnacceptedCriticalCount,
        int UnacceptedMajorCount,
        string FinalDecision,
        ViewportCapture[] ViewportCaptures,
        string[] EvidencePaths,
        object[] Issues,
        object[] RepairAttempts,
        bool Passed)
    {
        public static VisualQaReportFixture Valid() =>
            new(
                "0.1.0",
                "qa-contract",
                true,
                ["docs/storefront-analysis/visual-qa/home-desktop.png"],
                ["docs/storefront-analysis/reference-home-desktop.png"],
                [new("home", ["desktop", "tablet", "mobile"])],
                "visual-qa-agent",
                ["layout", "responsive", "ecommerce-slot-coverage"],
                [],
                0,
                0,
                "passed",
                [
                    new("home", "desktop", "docs/storefront-analysis/visual-qa/home-desktop.png", "passed"),
                    new("home", "tablet", "docs/storefront-analysis/visual-qa/home-tablet.png", "passed"),
                    new("home", "mobile", "docs/storefront-analysis/visual-qa/home-mobile.png", "passed")
                ],
                ["docs/storefront-analysis/visual-qa-report.md"],
                [],
                [new { attempt = 0, source = "no-repair-attempted", status = "skipped" }],
                true);
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
