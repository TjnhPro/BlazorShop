using BlazorShop.AI.StorefrontReverseEngineering.Analysis;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;

namespace BlazorShop.AI.StorefrontReverseEngineering.Application;

public sealed class VisualProjectWorkflowService
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public VisualProjectWorkflowService(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(this.repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<int> CaptureAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = CreateStore(root);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var configuration = await store.ReadJsonAsync<VisualProjectConfiguration>(ArtifactPath.Create("configuration.json"), "configuration", cancellationToken);
        var plan = await store.ReadJsonAsync<CapturePlan>(ArtifactPath.Create("discovery/capture-plan.json"), "capture-plan", cancellationToken);

        var capturing = VisualProjectStatusTransitions.MoveTo(project, VisualProjectStatus.Capturing);
        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", capturing, cancellationToken);

        var page = plan.Pages.First();
        var browser = ReferenceBrowserFactory.Create(repoRoot, page.Url);
        var captureService = new VisualCaptureService(repoRoot, browser);
        var extractor = new VisualEvidenceExtractor(repoRoot);
        var capturedCount = 0;

        foreach (var viewport in plan.Viewports)
        {
            var session = new BrowserPageSession(project.ProjectId, page.PageId, page.Url);
            var viewportResult = await captureService.CaptureViewportAsync(root, session, viewport, configuration.CapturePolicy, cancellationToken);
            await extractor.WriteViewportEvidenceAsync(root, session, viewport.Id, viewportResult, new EvidenceExtractionOptions(), cancellationToken);
            capturedCount++;
        }

        var captured = VisualProjectStatusTransitions.MoveTo(capturing, VisualProjectStatus.Captured);
        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", captured, cancellationToken);
        return capturedCount;
    }

    public async Task<VisualBlueprintDraft> AnalyzeAsync(string projectRoot, bool noAi, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = CreateStore(root);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var configuration = await store.ReadJsonAsync<VisualProjectConfiguration>(ArtifactPath.Create("configuration.json"), "configuration", cancellationToken);
        var plan = await store.ReadJsonAsync<CapturePlan>(ArtifactPath.Create("discovery/capture-plan.json"), "capture-plan", cancellationToken);

        var analyzing = VisualProjectStatusTransitions.MoveTo(project, VisualProjectStatus.Analyzing);
        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", analyzing, cancellationToken);

        var page = plan.Pages.First();
        var viewport = plan.Viewports.First();
        var elements = await store.ReadJsonAsync<ElementEvidenceIndex>(ArtifactPath.Create($"captures/{page.PageId}/{viewport.Id}/element-evidence-index.json"), "computed-style-evidence", cancellationToken);
        var assets = await store.ReadJsonAsync<AssetInventoryEvidence>(ArtifactPath.Create($"captures/{page.PageId}/{viewport.Id}/asset-inventory.normalized.json"), "asset-inventory", cancellationToken);
        var result = await new RuleBasedVisualAnalysisProvider()
            .AnalyzeAsync(new AnalysisContext(project.ProjectId, page.PageId, elements, assets, AiEnabled: !noAi), cancellationToken);

        await store.WriteJsonAsync(ArtifactPath.Create("analysis/page-topology.draft.json"), "page-topology-draft", result.PageTopology, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create($"analysis/page-specifications/{page.PageId}.json"), "page-specification-draft", result.PageSpecification, cancellationToken);
        foreach (var component in result.ComponentSpecifications)
        {
            await store.WriteJsonAsync(ArtifactPath.Create($"analysis/component-specifications/{component.CandidateId}.json"), "component-specification-draft", component, cancellationToken);
        }

        await store.WriteJsonAsync(ArtifactPath.Create("analysis/visual-blueprint.draft.json"), "visual-blueprint-draft", result.VisualBlueprint, cancellationToken);
        if (result.AiInferenceLog is not null)
        {
            await store.WriteJsonAsync(ArtifactPath.Create("analysis/ai-inference-log.json"), "ai-inference-log", result.AiInferenceLog, cancellationToken);
        }

        await new OriginalityAuditService(repoRoot)
            .WriteAuditAsync(root, project.ProjectId, page.PageId, assets, elements, configuration.OriginalityPolicy, cancellationToken);

        var draftReady = VisualProjectStatusTransitions.MoveTo(analyzing, VisualProjectStatus.DraftReady);
        await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", draftReady, cancellationToken);
        return result.VisualBlueprint;
    }

    public async Task<ReadinessReport> ValidateAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = CreateStore(root);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var required = new[]
        {
            "project.json",
            "configuration.json",
            "discovery/site-profile.json",
            "discovery/reconnaissance.json",
            "discovery/capture-plan.json",
            "captures/home/desktop-1440/manifest.json",
            "captures/home/tablet-768/manifest.json",
            "captures/home/mobile-390/manifest.json",
            "analysis/page-topology.draft.json",
            "analysis/visual-blueprint.draft.json",
            "analysis/originality-audit.json"
        };

        var findings = required
            .Where(path => !File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(path))))
            .Select(path => new ReadinessFinding("missing-artifact", "blocking", $"Required artifact is missing: {path}"))
            .ToList();

        var report = new ReadinessReport(
            "1.0",
            "readiness-report",
            $"readiness-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            findings.All(finding => finding.Severity != "blocking"),
            findings,
            required);

        await store.WriteJsonAsync(ArtifactPath.Create("reports/readiness-report.json"), "readiness-report", report, cancellationToken);
        Directory.CreateDirectory(Path.Combine(root, "reports"));
        await File.WriteAllTextAsync(Path.Combine(root, "reports", "readiness-report.md"), WriteMarkdown(report), cancellationToken);

        if (!report.Passed)
        {
            var failed = VisualProjectStatusTransitions.MoveTo(project, VisualProjectStatus.ValidationFailed, recoveryMode: true);
            await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", failed, cancellationToken);
        }

        return report;
    }

    public async Task<RunSummary> RunAsync(
        string referenceUrl,
        string name,
        string outputRoot,
        bool force,
        bool resume,
        bool noAi,
        CancellationToken cancellationToken,
        string? runId = null,
        string? forceStep = null)
    {
        var projectService = new VisualProjectService(repoRoot);
        VisualProjectInspection? inspection = null;
        VisualProject project;
        if (resume)
        {
            var projectId = VisualProjectId.Create(name);
            inspection = await projectService.InspectAsync(Path.Combine(outputRoot, projectId.Value), cancellationToken);
            project = inspection.Project;
        }
        else
        {
            project = await projectService.InitializeAsync(referenceUrl, name, outputRoot, force, cancellationToken);
        }

        var projectRoot = project.ArtifactRoot;
        var store = CreateStore(projectRoot);
        var configuration = await store.ReadJsonAsync<VisualProjectConfiguration>(ArtifactPath.Create("configuration.json"), "configuration", cancellationToken);
        var effectiveRunId = string.IsNullOrWhiteSpace(runId)
            ? resume && !string.IsNullOrWhiteSpace(inspection?.LatestRunId)
                ? inspection!.LatestRunId!
                : DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture)
            : runId!;
        var context = new VisualProjectWorkflowContext(
            repoRoot,
            project,
            projectRoot,
            effectiveRunId,
            noAi,
            store,
            sourceUrl => ReferenceBrowserFactory.Create(repoRoot, sourceUrl));
        var steps = CreateWorkflowSteps(configuration.Viewports);
        var run = await new SequentialWorkflowRunner<VisualProjectWorkflowContext>(store)
            .RunAsync(project.ProjectId, effectiveRunId, context, steps, forceStep, cancellationToken);

        var blueprintPath = resolver.ResolveArtifactPath(projectRoot, ArtifactPath.Create("analysis/visual-blueprint.draft.json"));
        var blueprintArtifactId = File.Exists(blueprintPath)
            ? $"visual-blueprint-{project.ProjectId}-home"
            : "(none)";
        var readinessPath = resolver.ResolveArtifactPath(projectRoot, ArtifactPath.Create("reports/readiness-report.json"));
        var readinessPassed = false;
        if (File.Exists(readinessPath))
        {
            var readinessJson = await File.ReadAllTextAsync(readinessPath, cancellationToken);
            readinessPassed = System.Text.Json.JsonSerializer.Deserialize<ReadinessReport>(readinessJson, VisualJson.Options)?.Passed == true;
        }

        var captured = run.Steps.Count(step => step.Name.StartsWith("capture-viewport-", StringComparison.Ordinal) && step.Status == WorkflowStepStatus.Succeeded);
        return new RunSummary(project.ProjectId, projectRoot, captured, blueprintArtifactId, readinessPassed, effectiveRunId, run.Status);
    }

    private FileSystemVisualArtifactStore CreateStore(string root) =>
        new(root, resolver, validator);

    private static IReadOnlyList<IWorkflowStep<VisualProjectWorkflowContext>> CreateWorkflowSteps(IReadOnlyList<ViewportDefinition> viewports)
    {
        var steps = new List<IWorkflowStep<VisualProjectWorkflowContext>>
        {
            new InitializeProjectStep(),
            new DiscoverReferenceStep()
        };
        steps.AddRange(viewports.Select(viewport => new CaptureViewportStep(viewport.Id)));
        steps.Add(new AnalyzeDraftStep());
        steps.Add(new OriginalityAuditStep());
        steps.Add(new ValidateReadinessStep());
        return steps;
    }

    private static string WriteMarkdown(ReadinessReport report)
    {
        var lines = new List<string>
        {
            "# Readiness Report",
            "",
            $"Project: `{report.ProjectId}`",
            $"Passed: `{report.Passed}`",
            "",
            "## Required Artifacts"
        };
        lines.AddRange(report.RequiredArtifacts.Select(path => $"- `{path}`"));
        lines.Add("");
        lines.Add("## Findings");
        lines.AddRange(report.Findings.Count == 0
            ? ["- None"]
            : report.Findings.Select(finding => $"- `{finding.Code}` ({finding.Severity}): {finding.Message}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}

public sealed record RunSummary(
    string ProjectId,
    string ArtifactRoot,
    int CapturedViewports,
    string BlueprintArtifactId,
    bool ReadinessPassed,
    string? RunId = null,
    WorkflowRunStatus? RunStatus = null);
