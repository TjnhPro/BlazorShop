using BlazorShop.AI.StorefrontReverseEngineering.Workflows;

namespace BlazorShop.AI.StorefrontReverseEngineering.Application;

public sealed class Phase3BPreflightService
{
    private readonly VisualProjectService projectService;

    public Phase3BPreflightService(string repoRoot)
    {
        projectService = new VisualProjectService(repoRoot);
    }

    public async Task<Phase3BPreflightResult> CheckAsync(string projectRoot, CancellationToken cancellationToken)
    {
        VisualProjectInspection inspection;
        try
        {
            inspection = await projectService.InspectAsync(projectRoot, cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("SRE-INSPECT-001", StringComparison.Ordinal))
        {
            return Phase3BPreflightResult.Blocked(
                Path.GetFullPath(projectRoot),
                null,
                null,
                null,
                null,
                new Phase3BPreflightIssue(
                    "missing-project",
                    "blocking",
                    $"Phase 3B project root is missing project.json: {projectRoot}"));
        }

        var issues = new List<Phase3BPreflightIssue>();
        if (string.IsNullOrWhiteSpace(inspection.LatestRunId))
        {
            issues.Add(new Phase3BPreflightIssue(
                "missing-latest-run",
                "blocking",
                "Phase 3B requires a completed Phase 3A workflow run before analysis starts."));
        }
        else if (inspection.LatestRun is null)
        {
            issues.Add(new Phase3BPreflightIssue(
                "unavailable-latest-run",
                "blocking",
                $"Latest Phase 3A workflow run '{inspection.LatestRunId}' is {inspection.LatestRunState}."));
        }
        else if (inspection.LatestRunStatus != WorkflowRunStatus.Succeeded)
        {
            issues.Add(new Phase3BPreflightIssue(
                "failed-latest-run",
                "blocking",
                $"Latest Phase 3A workflow run '{inspection.LatestRunId}' ended with status {inspection.LatestRunStatus}."));
        }

        if (inspection.ReadinessPassed != true)
        {
            var reason = inspection.ReadinessPassed is null ? "unknown or missing" : "failed";
            issues.Add(new Phase3BPreflightIssue(
                "phase3a-readiness-not-passed",
                "blocking",
                $"Phase 3A readiness is {reason}; run validate and inspect before Phase 3B analysis."));
        }

        if (!File.Exists(inspection.BlueprintPath))
        {
            issues.Add(new Phase3BPreflightIssue(
                "missing-phase3a-blueprint",
                "blocking",
                $"Phase 3A draft blueprint is missing: {inspection.BlueprintPath}"));
        }

        return new Phase3BPreflightResult(
            issues.All(issue => !string.Equals(issue.Severity, "blocking", StringComparison.OrdinalIgnoreCase)),
            inspection.ArtifactRoot,
            inspection.Project.ProjectId,
            inspection.LatestRunId,
            inspection.ReadinessReportPath,
            inspection.BlueprintPath,
            issues);
    }
}

public sealed record Phase3BPreflightResult(
    bool Passed,
    string ProjectRoot,
    string? ProjectId,
    string? LatestRunId,
    string? ReadinessReportPath,
    string? BlueprintPath,
    IReadOnlyList<Phase3BPreflightIssue> Issues)
{
    public static Phase3BPreflightResult Blocked(
        string projectRoot,
        string? projectId,
        string? latestRunId,
        string? readinessReportPath,
        string? blueprintPath,
        params Phase3BPreflightIssue[] issues) =>
        new(false, projectRoot, projectId, latestRunId, readinessReportPath, blueprintPath, issues);
}

public sealed record Phase3BPreflightIssue(
    string Code,
    string Severity,
    string Message);
