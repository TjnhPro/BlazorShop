using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;

namespace BlazorShop.AI.StorefrontReverseEngineering.Application;

public sealed class VisualProjectWorkflowContext
{
    public VisualProjectWorkflowContext(
        string repoRoot,
        VisualProject project,
        string artifactRoot,
        string runId,
        bool noAi,
        IVisualArtifactStore artifactStore,
        Func<string, IReferenceBrowser> browserFactory)
    {
        RepoRoot = repoRoot;
        Project = project;
        ArtifactRoot = artifactRoot;
        RunId = runId;
        NoAi = noAi;
        ArtifactStore = artifactStore;
        BrowserFactory = browserFactory;
    }

    public string RepoRoot { get; }

    public VisualProject Project { get; set; }

    public string ArtifactRoot { get; }

    public string RunId { get; }

    public bool NoAi { get; }

    public IVisualArtifactStore ArtifactStore { get; }

    public Func<string, IReferenceBrowser> BrowserFactory { get; }
}

public interface IVisualProjectWorkflowStep : IWorkflowStep<VisualProjectWorkflowContext>
{
    IReadOnlyList<string> InputArtifacts { get; }

    IReadOnlyList<string> OutputArtifacts { get; }
}

internal sealed class InitializeProjectStep : IVisualProjectWorkflowStep
{
    public string Name => "initialize-project";

    public IReadOnlyList<string> InputArtifacts => ["project.json", "configuration.json"];

    public IReadOnlyList<string> OutputArtifacts => ["project.json"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var updated = context.Project with
        {
            LatestRunId = context.RunId,
            UpdatedUtc = DateTimeOffset.UtcNow
        };
        await context.ArtifactStore.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", updated, cancellationToken);
        context.Project = updated;
        return WorkflowStepResult.Success();
    }
}

internal sealed class DiscoverReferenceStep : IVisualProjectWorkflowStep
{
    public string Name => "discover-reference";

    public IReadOnlyList<string> InputArtifacts => ["project.json", "configuration.json"];

    public IReadOnlyList<string> OutputArtifacts => ["discovery/site-profile.json", "discovery/reconnaissance.json", "discovery/capture-plan.json"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        if (File.Exists(Path.Combine(context.ArtifactRoot, "discovery", "capture-plan.json")) &&
            context.Project.Status is VisualProjectStatus.Discovered or VisualProjectStatus.Capturing or VisualProjectStatus.Captured or VisualProjectStatus.Analyzing or VisualProjectStatus.DraftReady)
        {
            return WorkflowStepResult.Skip("SRE-WORKFLOW-DISCOVER-SKIP", "Discovery artifacts already exist for this project.");
        }

        await new VisualDiscoveryService(context.RepoRoot, context.BrowserFactory(context.Project.ReferenceUrl))
            .DiscoverAsync(context.ArtifactRoot, cancellationToken);
        context.Project = await context.ArtifactStore.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        return WorkflowStepResult.Success();
    }
}

internal sealed class CaptureViewportStep : IVisualProjectWorkflowStep
{
    private readonly string viewportId;

    public CaptureViewportStep(string viewportId)
    {
        this.viewportId = viewportId;
    }

    public string Name => $"capture-viewport-{viewportId}";

    public IReadOnlyList<string> InputArtifacts => ["project.json", "configuration.json", "discovery/capture-plan.json"];

    public IReadOnlyList<string> OutputArtifacts =>
    [
        $"captures/home/{viewportId}/manifest.json",
        $"captures/home/{viewportId}/capture-quality-report.json",
        $"captures/home/{viewportId}/element-evidence-index.json",
        $"captures/home/{viewportId}/asset-inventory.normalized.json",
        "captures/home/capture-manifest.json"
    ];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var configuration = await context.ArtifactStore.ReadJsonAsync<VisualProjectConfiguration>(ArtifactPath.Create("configuration.json"), "configuration", cancellationToken);
        var plan = await context.ArtifactStore.ReadJsonAsync<CapturePlan>(ArtifactPath.Create("discovery/capture-plan.json"), "capture-plan", cancellationToken);
        var viewport = plan.Viewports.FirstOrDefault(candidate => string.Equals(candidate.Id, viewportId, StringComparison.Ordinal));
        if (viewport is null)
        {
            return WorkflowStepResult.Skip("SRE-WORKFLOW-VIEWPORT-SKIP", $"Capture plan does not include viewport '{viewportId}'.");
        }

        var page = plan.Pages.First();
        var currentProject = await context.ArtifactStore.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        if (currentProject.Status == VisualProjectStatus.Discovered)
        {
            currentProject = VisualProjectStatusTransitions.MoveTo(currentProject, VisualProjectStatus.Capturing);
            await context.ArtifactStore.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", currentProject, cancellationToken);
        }

        var session = new BrowserPageSession(currentProject.ProjectId, page.PageId, page.Url);
        var browser = context.BrowserFactory(page.Url);
        var captured = await new VisualCaptureService(context.RepoRoot, browser)
            .CaptureViewportAsync(context.ArtifactRoot, session, viewport, configuration.CapturePolicy, cancellationToken, context.RunId);
        await new VisualEvidenceExtractor(context.RepoRoot)
            .WriteViewportEvidenceAsync(context.ArtifactRoot, session, viewport.Id, captured, new EvidenceExtractionOptions(), cancellationToken);

        if (plan.Viewports.All(candidate => File.Exists(Path.Combine(context.ArtifactRoot, "captures", page.PageId, candidate.Id, "manifest.json"))))
        {
            var capturedProject = VisualProjectStatusTransitions.MoveTo(currentProject, VisualProjectStatus.Captured);
            await context.ArtifactStore.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", capturedProject, cancellationToken);
            context.Project = capturedProject;
        }

        return WorkflowStepResult.Success();
    }
}

internal sealed class AnalyzeDraftStep : IVisualProjectWorkflowStep
{
    public string Name => "analyze-draft";

    public IReadOnlyList<string> InputArtifacts => ["captures/home/capture-manifest.json"];

    public IReadOnlyList<string> OutputArtifacts => ["analysis/page-topology.draft.json", "analysis/visual-blueprint.draft.json"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        await new VisualProjectWorkflowService(context.RepoRoot)
            .AnalyzeAsync(context.ArtifactRoot, context.NoAi, cancellationToken);
        context.Project = await context.ArtifactStore.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        return WorkflowStepResult.Success();
    }
}

internal sealed class OriginalityAuditStep : IVisualProjectWorkflowStep
{
    public string Name => "originality-audit";

    public IReadOnlyList<string> InputArtifacts => ["analysis/visual-blueprint.draft.json"];

    public IReadOnlyList<string> OutputArtifacts => ["analysis/originality-audit.json", "reports/originality-audit.md"];

    public Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(File.Exists(Path.Combine(context.ArtifactRoot, "analysis", "originality-audit.json"))
            ? WorkflowStepResult.Success()
            : WorkflowStepResult.Failure("SRE-WORKFLOW-ORIGINALITY-MISSING", "Originality audit was not written by the analysis step.", retryable: true));
    }
}

internal sealed class ValidateReadinessStep : IVisualProjectWorkflowStep
{
    public string Name => "validate-readiness";

    public IReadOnlyList<string> InputArtifacts => ["project.json", "discovery/capture-plan.json", "analysis/visual-blueprint.draft.json", "analysis/originality-audit.json"];

    public IReadOnlyList<string> OutputArtifacts => ["reports/readiness-report.json", "reports/readiness-report.md"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var report = await new VisualProjectWorkflowService(context.RepoRoot)
            .ValidateAsync(context.ArtifactRoot, cancellationToken);
        context.Project = await context.ArtifactStore.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        return report.Passed
            ? WorkflowStepResult.Success(report.Findings.Where(finding => finding.Severity == "warning").Select(finding => new WorkflowMessage(finding.Code, finding.Message)).ToArray())
            : WorkflowStepResult.Failure("SRE-WORKFLOW-READINESS-FAILED", "Readiness validation returned blocking findings.");
    }
}

internal sealed class AggregateEvidenceStep : IVisualProjectWorkflowStep
{
    public string Name => "aggregate-evidence";

    public IReadOnlyList<string> InputArtifacts => ["reports/readiness-report.json", "analysis/visual-blueprint.draft.json"];

    public IReadOnlyList<string> OutputArtifacts => ["analysis/evidence-snapshot.json", "reports/evidence-snapshot.md"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var snapshot = await new EvidenceSnapshotAggregator(context.RepoRoot)
            .BuildAsync(context.ArtifactRoot, cancellationToken);
        var warnings = snapshot.Issues
            .Where(issue => issue.Severity == "warning")
            .Select(issue => new WorkflowMessage(issue.Code, issue.Message))
            .ToArray();

        return snapshot.Issues.Any(issue => issue.Severity == "blocking")
            ? WorkflowStepResult.Failure("SRE-WORKFLOW-EVIDENCE-SNAPSHOT-BLOCKED", "Evidence snapshot aggregation returned blocking findings.")
            : WorkflowStepResult.Success(warnings);
    }
}

internal sealed class ExtractRawDesignTokensStep : IVisualProjectWorkflowStep
{
    public string Name => "extract-raw-tokens";

    public IReadOnlyList<string> InputArtifacts => ["analysis/evidence-snapshot.json"];

    public IReadOnlyList<string> OutputArtifacts => ["analysis/tokens/raw-design-tokens.json", "analysis/tokens/token-frequency-report.json"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var tokens = await new RawDesignTokenExtractor(context.RepoRoot)
            .ExtractAsync(context.ArtifactRoot, cancellationToken);
        var warnings = tokens.Issues
            .Where(issue => issue.Severity == "warning")
            .Select(issue => new WorkflowMessage(issue.Code, issue.Message))
            .ToArray();

        return tokens.Tokens.Count == 0
            ? WorkflowStepResult.Failure("SRE-WORKFLOW-RAW-TOKENS-EMPTY", "Raw design token extraction produced no tokens.")
            : WorkflowStepResult.Success(warnings);
    }
}

internal sealed class NormalizeSemanticTokensStep : IVisualProjectWorkflowStep
{
    public string Name => "normalize-semantic-tokens";

    public IReadOnlyList<string> InputArtifacts => ["analysis/tokens/raw-design-tokens.json"];

    public IReadOnlyList<string> OutputArtifacts => ["analysis/tokens/semantic-tokens.draft.json", "analysis/tokens/token-conflicts.json"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var tokens = await new SemanticTokenNormalizer(context.RepoRoot)
            .NormalizeAsync(context.ArtifactRoot, cancellationToken);
        var warnings = tokens.HumanReviewRequired
            ? tokens.ReviewReasons.Select(reason => new WorkflowMessage("semantic-token-review-required", reason)).ToArray()
            : [];

        return tokens.Tokens.Count == 0
            ? WorkflowStepResult.Failure("SRE-WORKFLOW-SEMANTIC-TOKENS-EMPTY", "Semantic token normalization produced no tokens.")
            : WorkflowStepResult.Success(warnings);
    }
}

internal sealed class ClassifyPageArchetypesStep : IVisualProjectWorkflowStep
{
    public string Name => "classify-page-archetypes";

    public IReadOnlyList<string> InputArtifacts => ["analysis/evidence-snapshot.json", "analysis/tokens/semantic-tokens.draft.json"];

    public IReadOnlyList<string> OutputArtifacts => ["analysis/pages/{pageId}/page-archetype.json"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var pages = await new PageArchetypeClassifier(context.RepoRoot)
            .ClassifyAsync(context.ArtifactRoot, cancellationToken);

        return pages.Count == 0
            ? WorkflowStepResult.Failure("SRE-WORKFLOW-PAGE-ARCHETYPE-EMPTY", "Page archetype classification produced no page artifacts.")
            : WorkflowStepResult.Success();
    }
}

internal sealed class SegmentSectionsStep : IVisualProjectWorkflowStep
{
    public string Name => "segment-sections";

    public IReadOnlyList<string> InputArtifacts => ["analysis/evidence-snapshot.json", "analysis/pages/{pageId}/page-archetype.json"];

    public IReadOnlyList<string> OutputArtifacts => ["analysis/pages/{pageId}/sections.draft.json"];

    public async Task<WorkflowStepResult> ExecuteAsync(VisualProjectWorkflowContext context, CancellationToken cancellationToken)
    {
        var documents = await new SectionSegmenter(context.RepoRoot)
            .SegmentAsync(context.ArtifactRoot, cancellationToken);
        var warnings = documents
            .SelectMany(document => document.Issues)
            .Where(issue => issue.Severity == "warning")
            .Select(issue => new WorkflowMessage(issue.Code, issue.Message))
            .ToArray();

        return documents.Any(document => document.Issues.Any(issue => issue.Severity == "blocking"))
            ? WorkflowStepResult.Failure("SRE-WORKFLOW-SECTIONS-BLOCKED", "Section segmentation returned blocking findings.")
            : WorkflowStepResult.Success(warnings);
    }
}
