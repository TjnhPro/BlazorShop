using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using System.Text.Json;

namespace BlazorShop.AI.StorefrontReverseEngineering.Application;

public sealed class VisualProjectService
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver rootResolver;
    private readonly IVisualSchemaValidator schemaValidator;

    public VisualProjectService(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        rootResolver = new ApprovedArtifactRootResolver(this.repoRoot);
        schemaValidator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<VisualProject> InitializeAsync(
        string referenceUrl,
        string name,
        string outputRoot,
        bool force,
        CancellationToken cancellationToken)
    {
        var url = ReferenceUrl.Create(referenceUrl);
        var projectId = VisualProjectId.Create(name);
        var root = rootResolver.ResolveRoot(outputRoot);
        var projectRoot = rootResolver.ResolveRoot(Path.Combine(root, projectId.Value));

        if (Directory.Exists(projectRoot) && !force)
        {
            throw new InvalidOperationException($"[SRE-INIT-001] Visual project already exists. Problem: '{projectRoot}' already contains project state. Cause: init is non-destructive by default. Fix: choose a new name or pass --force to overwrite deterministic project metadata.");
        }

        if (Directory.Exists(projectRoot) && force)
        {
            DeleteProjectRootForForce(root, projectRoot);
        }

        Directory.CreateDirectory(projectRoot);
        var store = CreateStore(projectRoot);
        var now = DateTimeOffset.UtcNow;

        var project = new VisualProject(
            "1.0",
            "visual-project",
            $"project-{projectId.Value}",
            now,
            projectId.Value,
            name.Trim(),
            url.Value,
            projectRoot,
            VisualProjectStatus.Created,
            UpdatedUtc: now);

        var configuration = new VisualProjectConfiguration(
            "1.0",
            "configuration",
            $"configuration-{projectId.Value}",
            now,
            projectId.Value,
            name.Trim(),
            url.Value,
            root,
            new CapturePolicy(),
            new OriginalityPolicy(),
            ViewportDefinition.Defaults);

        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", project, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("configuration.json"), "configuration", configuration, cancellationToken);
        return project;
    }

    public async Task<VisualProjectInspection> InspectAsync(string projectPath, CancellationToken cancellationToken)
    {
        var projectRoot = rootResolver.ResolveRoot(projectPath);
        var projectFile = Path.Combine(projectRoot, "project.json");
        if (!File.Exists(projectFile))
        {
            throw new InvalidOperationException($"[SRE-INSPECT-001] Visual project was not found. Problem: '{projectPath}' has no project.json. Cause: inspect expects an initialized reverse-engineering project. Fix: run init first or pass the correct --project path.");
        }

        var store = CreateStore(projectRoot);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var latestRunId = project.LatestRunId ?? FindLatestRun(projectRoot);
        var latestRunPath = latestRunId is null
            ? null
            : Path.Combine(projectRoot, "runs", latestRunId + ".json");
        var latestRun = latestRunPath is null
            ? null
            : TryReadLatestRun(latestRunPath, out _);
        var latestRunState = ResolveLatestRunState(latestRunId, latestRunPath, latestRun);

        var blueprintPath = Path.Combine(projectRoot, "analysis", "visual-blueprint.draft.json");
        var readinessReportPath = Path.Combine(projectRoot, "reports", "readiness-report.json");
        var readiness = TryReadReadiness(readinessReportPath, out var readinessError);
        var blockingFindings = readiness?.Findings
            .Where(finding => string.Equals(finding.Severity, "blocking", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? [];
        var warningCount = readiness?.Findings.Count(finding =>
            string.Equals(finding.Severity, "warning", StringComparison.OrdinalIgnoreCase)) ?? 0;

        var inspectionWarnings = new List<string>();
        if (latestRunPath is not null && !File.Exists(latestRunPath))
        {
            inspectionWarnings.Add($"Latest workflow run file is missing: {latestRunPath}");
        }
        else if (latestRunPath is not null && latestRun is null)
        {
            inspectionWarnings.Add($"Latest workflow run file is invalid: {latestRunPath}");
        }

        if (readinessError is not null)
        {
            inspectionWarnings.Add(readinessError);
        }

        var readinessSummary = readiness is null
            ? readinessError is null
                ? "No readiness report found."
                : "Readiness report invalid."
            : $"Readiness passed: {readiness.Passed}; blocking: {blockingFindings.Length}; warnings: {warningCount}.";

        return new VisualProjectInspection(
            project,
            latestRunId,
            latestRun,
            latestRun?.Status,
            latestRunState,
            readiness?.Passed,
            blockingFindings.Length,
            warningCount,
            blockingFindings.LastOrDefault(),
            blueprintPath,
            readinessReportPath,
            project.ArtifactRoot,
            readinessSummary,
            inspectionWarnings.Count == 0 ? null : string.Join(" | ", inspectionWarnings));
    }

    private FileSystemVisualArtifactStore CreateStore(string projectRoot) =>
        new(projectRoot, rootResolver, schemaValidator);

    private static void DeleteProjectRootForForce(string approvedOutputRoot, string projectRoot)
    {
        var fullOutputRoot = Path.GetFullPath(approvedOutputRoot);
        var fullProjectRoot = Path.GetFullPath(projectRoot);
        if (fullProjectRoot.Equals(fullOutputRoot, StringComparison.OrdinalIgnoreCase) ||
            !ApprovedArtifactRootResolver.IsUnderRoot(fullProjectRoot, fullOutputRoot) ||
            fullProjectRoot.Contains(Path.Combine("storefront-builder", "generated"), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"[SRE-FORCE-001] Unsafe force cleanup refused. Problem: '{projectRoot}' is not a single reverse-engineering project root. Cause: --force may only delete one project under the approved reverse-engineering output root. Fix: pass a project name under artifacts/storefront-reverse-engineering/projects or obj/storefront-reverse-engineering/projects.");
        }

        Directory.Delete(fullProjectRoot, recursive: true);
    }

    private static string? FindLatestRun(string projectRoot)
    {
        var runsRoot = Path.Combine(projectRoot, "runs");
        if (!Directory.Exists(runsRoot))
        {
            return null;
        }

        return Directory.EnumerateFiles(runsRoot, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault();
    }

    private static WorkflowRun? TryReadLatestRun(string runPath, out string? error)
    {
        error = null;
        if (!File.Exists(runPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(runPath);
            return JsonSerializer.Deserialize<WorkflowRun>(json, VisualJson.Options);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            error = exception.Message;
            return null;
        }
    }

    private static ReadinessReport? TryReadReadiness(string readinessReportPath, out string? error)
    {
        error = null;
        if (!File.Exists(readinessReportPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(readinessReportPath);
            return JsonSerializer.Deserialize<ReadinessReport>(json, VisualJson.Options)
                ?? throw new JsonException("Readiness report deserialized to null.");
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            error = $"Readiness report is invalid: {readinessReportPath}: {exception.Message}";
            return null;
        }
    }

    private static string ResolveLatestRunState(string? latestRunId, string? latestRunPath, WorkflowRun? latestRun)
    {
        if (latestRunId is null)
        {
            return "(none)";
        }

        if (latestRunPath is not null && !File.Exists(latestRunPath))
        {
            return "missing";
        }

        return latestRun is null ? "invalid" : latestRun.Status.ToString();
    }
}

public sealed record VisualProjectInspection(
    VisualProject Project,
    string? LatestRunId,
    WorkflowRun? LatestRun,
    WorkflowRunStatus? LatestRunStatus,
    string LatestRunState,
    bool? ReadinessPassed,
    int BlockingFindingCount,
    int WarningCount,
    ReadinessFinding? LatestBlockingFinding,
    string BlueprintPath,
    string ReadinessReportPath,
    string ArtifactRoot,
    string ReadinessSummary,
    string? InspectionWarning);
