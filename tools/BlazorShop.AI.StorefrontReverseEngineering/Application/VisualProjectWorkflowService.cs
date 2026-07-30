using BlazorShop.AI.StorefrontReverseEngineering.Analysis;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using ImageMagick;

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
        var required = new List<string>
        {
            "project.json",
            "configuration.json",
            "discovery/site-profile.json",
            "discovery/reconnaissance.json",
            "discovery/capture-plan.json",
            "analysis/page-topology.draft.json",
            "analysis/visual-blueprint.draft.json",
            "analysis/originality-audit.json"
        };
        CapturePlan? plan = null;
        var capturePlanPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create("discovery/capture-plan.json"));
        if (File.Exists(capturePlanPath))
        {
            plan = await store.ReadJsonAsync<CapturePlan>(ArtifactPath.Create("discovery/capture-plan.json"), "capture-plan", cancellationToken);
            foreach (var page in plan.Pages)
            {
                required.Add($"captures/{page.PageId}/capture-manifest.json");
                foreach (var viewport in plan.Viewports)
                {
                    var captureRoot = $"captures/{page.PageId}/{viewport.Id}";
                    required.Add($"{captureRoot}/manifest.json");
                    required.Add($"{captureRoot}/full-page.png");
                    required.Add($"{captureRoot}/dom.html");
                    required.Add($"{captureRoot}/styles.json");
                    required.Add($"{captureRoot}/boxes.json");
                    required.Add($"{captureRoot}/assets.json");
                    required.Add($"{captureRoot}/element-evidence-index.json");
                    required.Add($"{captureRoot}/asset-inventory.normalized.json");
                    required.Add($"{captureRoot}/capture-quality-report.json");
                }
            }
        }

        var findings = required
            .Where(path => !File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(path))))
            .Select(path => new ReadinessFinding("missing-artifact", "blocking", $"Required artifact is missing: {path}"))
            .ToList();

        foreach (var path in required.Where(path => path.EndsWith(".json", StringComparison.Ordinal) && File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(path)))))
        {
            try
            {
                ValidateArtifactByPath(store, path, cancellationToken).GetAwaiter().GetResult();
            }
            catch (InvalidOperationException exception)
            {
                findings.Add(new ReadinessFinding("invalid-schema", "blocking", $"Invalid schema for {path}: {exception.Message}"));
            }
        }

        if (plan is not null)
        {
            foreach (var page in plan.Pages)
            {
                var pageManifestPath = $"captures/{page.PageId}/capture-manifest.json";
                if (File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(pageManifestPath))))
                {
                    try
                    {
                        var pageManifest = await store.ReadJsonAsync<PageCaptureManifest>(ArtifactPath.Create(pageManifestPath), "page-capture-manifest", cancellationToken);
                        new VisualEvidenceExtractor(repoRoot).ValidateReferencedFiles(root, pageManifest);
                    }
                    catch (InvalidOperationException exception)
                    {
                        findings.Add(new ReadinessFinding("missing-manifest-reference", "blocking", exception.Message));
                    }
                }

                foreach (var viewport in plan.Viewports)
                {
                    var qualityPath = $"captures/{page.PageId}/{viewport.Id}/capture-quality-report.json";
                    if (!File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(qualityPath))))
                    {
                        continue;
                    }

                    var quality = await store.ReadJsonAsync<CaptureQualityReport>(ArtifactPath.Create(qualityPath), "capture-quality-report", cancellationToken);
                    if (!quality.Passed)
                    {
                        findings.Add(new ReadinessFinding("quality-failed", "blocking", $"Capture quality failed for {page.PageId}/{viewport.Id}."));
                    }

                    await ValidateViewportEvidenceReadinessAsync(root, store, page, viewport, quality, findings, cancellationToken);
                }
            }
        }

        var latestRunId = project.LatestRunId ?? FindLatestRunId(root);
        if (!string.IsNullOrWhiteSpace(latestRunId))
        {
            var runPath = $"runs/{latestRunId}.json";
            if (!File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(runPath))))
            {
                findings.Add(new ReadinessFinding("failed-latest-run", "blocking", $"Latest workflow run is missing: {runPath}"));
            }
            else
            {
                var run = await store.ReadJsonAsync<WorkflowRun>(ArtifactPath.Create(runPath), "workflow-run", cancellationToken);
                if (run.Status is WorkflowRunStatus.Failed or WorkflowRunStatus.Canceled)
                {
                    findings.Add(new ReadinessFinding("failed-latest-run", "blocking", $"Latest workflow run '{run.RunId}' ended with status {run.Status}."));
                }
                var incompleteSteps = run.Steps
                    .Where(step => step.Status is not (WorkflowStepStatus.Succeeded or WorkflowStepStatus.Skipped))
                    .ToArray();
                var isCurrentReadinessStep = run.Status == WorkflowRunStatus.Running &&
                    incompleteSteps.Any(step => step.Name == "validate-readiness" && step.Status == WorkflowStepStatus.Running) &&
                    incompleteSteps.All(step =>
                        step.Name == "validate-readiness" && step.Status == WorkflowStepStatus.Running ||
                        IsPhase3BDownstreamStep(step.Name) && step.Status == WorkflowStepStatus.Pending);

                if (run.Status != WorkflowRunStatus.Succeeded && !isCurrentReadinessStep)
                {
                    findings.Add(new ReadinessFinding("partial-latest-run", "blocking", $"Latest workflow run '{run.RunId}' is not complete. Status: {run.Status}."));
                }

                foreach (var step in incompleteSteps.Where(_ => !isCurrentReadinessStep))
                {
                    findings.Add(new ReadinessFinding("partial-latest-run", "blocking", $"Latest workflow run '{run.RunId}' has incomplete step '{step.Name}' with status {step.Status}."));
                }
            }
        }

        await ValidateOriginalityReadinessAsync(root, store, findings, cancellationToken);
        ValidateBlueprintEvidenceReferences(root, store, findings, cancellationToken);
        ValidateSensitiveRedaction(root, required, findings);

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
        else if (project.Status == VisualProjectStatus.ValidationFailed)
        {
            var recovered = VisualProjectStatusTransitions.MoveTo(project, VisualProjectStatus.DraftReady, recoveryMode: true);
            await store.WriteJsonAsync(ArtifactPath.Create("project.json"), "visual-project", recovered, cancellationToken);
        }

        return report;
    }

    public async Task<EvidenceSnapshot> AggregateEvidenceAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new EvidenceSnapshotAggregator(repoRoot).BuildAsync(root, cancellationToken);
    }

    public async Task<RawDesignTokenDocument> ExtractRawDesignTokensAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new RawDesignTokenExtractor(repoRoot).ExtractAsync(root, cancellationToken);
    }

    public async Task<SemanticTokenDocument> NormalizeSemanticTokensAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new SemanticTokenNormalizer(repoRoot).NormalizeAsync(root, cancellationToken);
    }

    public async Task<IReadOnlyList<PageArchetypeDocument>> ClassifyPageArchetypesAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new PageArchetypeClassifier(repoRoot).ClassifyAsync(root, cancellationToken);
    }

    public async Task<IReadOnlyList<SectionsDraftDocument>> SegmentSectionsAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new SectionSegmenter(repoRoot).SegmentAsync(root, cancellationToken);
    }

    public async Task<IReadOnlyList<(ResponsiveBehaviorDocument Responsive, InteractionModelDocument Interaction)>> AnalyzeResponsiveInteractionsAsync(
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new ResponsiveInteractionAnalyzer(repoRoot).AnalyzeAsync(root, cancellationToken);
    }

    public async Task<(ComponentCandidatesDocument Candidates, ComponentInstancesDocument Instances)> DetectComponentCandidatesAsync(
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new VisualComponentCandidateDetector(repoRoot).DetectAsync(root, cancellationToken);
    }

    public async Task<IReadOnlyList<EcommerceRegionsDocument>> ClassifyEcommerceRegionsAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new EcommerceRegionClassifier(repoRoot).ClassifyAsync(root, cancellationToken);
    }

    public async Task<PresentationComponentCatalog> BuildPresentationCatalogAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new PresentationComponentCatalogBuilder(repoRoot).BuildAsync(root, cancellationToken);
    }

    public async Task<PresentationMappingsDocument> MapPresentationComponentsAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new PresentationMapper(repoRoot).MapAsync(root, cancellationToken);
    }

    public async Task<ConfidenceReport> ScoreConfidenceAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new ConfidenceScorer(repoRoot).ScoreAsync(root, cancellationToken);
    }

    public async Task<(VisualBlueprintV1 Draft, VisualBlueprintV1 Reviewed, GenerationReadinessReport Readiness)> AssembleBlueprintV1Async(
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        return await new BlueprintV1Assembler(repoRoot).AssembleAsync(root, cancellationToken);
    }

    private async Task ValidateViewportEvidenceReadinessAsync(
        string root,
        FileSystemVisualArtifactStore store,
        CapturePlanPage page,
        ViewportDefinition viewport,
        CaptureQualityReport quality,
        List<ReadinessFinding> findings,
        CancellationToken cancellationToken)
    {
        var captureRoot = $"captures/{page.PageId}/{viewport.Id}";
        var manifestPath = $"{captureRoot}/manifest.json";
        var elementPath = $"{captureRoot}/element-evidence-index.json";
        var assetPath = $"{captureRoot}/asset-inventory.normalized.json";
        if (!File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(manifestPath))) ||
            !File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(elementPath))) ||
            !File.Exists(resolver.ResolveArtifactPath(root, ArtifactPath.Create(assetPath))))
        {
            return;
        }

        CaptureViewportManifest manifest;
        ElementEvidenceIndex elements;
        AssetInventoryEvidence assets;
        manifest = await ReadJsonUncheckedAsync<CaptureViewportManifest>(root, manifestPath, cancellationToken);
        elements = await ReadJsonUncheckedAsync<ElementEvidenceIndex>(root, elementPath, cancellationToken);
        assets = await ReadJsonUncheckedAsync<AssetInventoryEvidence>(root, assetPath, cancellationToken);

        if (elements.Elements.Count == 0)
        {
            findings.Add(new ReadinessFinding("empty-computed-style-evidence", "blocking", $"No element evidence was captured for {page.PageId}/{viewport.Id}."));
            return;
        }

        if (!elements.Elements.Any(element => element.Category is "semantic-landmark" or "section" or "heading" or "product-card-candidate"))
        {
            findings.Add(new ReadinessFinding("empty-computed-style-evidence", "blocking", $"No semantic landmark, section, heading, or product-card candidate evidence was captured for {page.PageId}/{viewport.Id}."));
        }

        if (!elements.Elements.Any(HasAnyStyleValue))
        {
            findings.Add(new ReadinessFinding("empty-style-groups", "blocking", $"Style evidence has no non-empty style values for {page.PageId}/{viewport.Id}."));
        }

        if (!elements.Elements.Any(element => HasStyleGroupValue(element, "typography")))
        {
            findings.Add(new ReadinessFinding("missing-typography-evidence", "blocking", $"Typography style evidence is missing for {page.PageId}/{viewport.Id}."));
        }

        if (!elements.Elements.Any(element => HasStyleGroupValue(element, "layout")))
        {
            findings.Add(new ReadinessFinding("missing-layout-evidence", "blocking", $"Layout style evidence is missing for {page.PageId}/{viewport.Id}."));
        }

        var usefulBoxes = elements.Elements.Where(element => element.Box is { Width: > 0, Height: > 0 }).ToArray();
        if (usefulBoxes.Length == 0)
        {
            findings.Add(new ReadinessFinding("missing-useful-bounding-box", "blocking", $"No useful bounding boxes were captured for {page.PageId}/{viewport.Id}."));
        }

        foreach (var element in elements.Elements.Where(element => element.Box is not null))
        {
            var box = element.Box!;
            if (box.Width <= 0 ||
                box.Height <= 0 ||
                box.X < -10 ||
                box.Y < -10 ||
                (quality.FinalWidth.HasValue && box.X > quality.FinalWidth.Value * 2) ||
                (quality.FinalHeight.HasValue && box.Y > quality.FinalHeight.Value * 2))
            {
                findings.Add(new ReadinessFinding("invalid-element-box", "blocking", $"Invalid element box for evidence '{element.EvidenceId}' in {page.PageId}/{viewport.Id}."));
            }
        }

        if (!elements.Elements.Any(element => element.Category is "semantic-landmark" or "section" or "heading" or "product-card-candidate" && element.Box is { Width: > 0, Height: > 0 }))
        {
            findings.Add(new ReadinessFinding("missing-useful-bounding-box", "blocking", $"No major element has a useful bounding box for {page.PageId}/{viewport.Id}."));
        }

        ValidateCorrelation(page.PageId, viewport.Id, manifest.CaptureCorrelationId, elements.CaptureCorrelationId, assets.CaptureCorrelationId, findings);
        ValidateQualityArtifactShape(root, captureRoot, quality, findings);
    }

    private async Task ValidateOriginalityReadinessAsync(
        string root,
        FileSystemVisualArtifactStore store,
        List<ReadinessFinding> findings,
        CancellationToken cancellationToken)
    {
        var auditPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create("analysis/originality-audit.json"));
        if (!File.Exists(auditPath))
        {
            return;
        }

        var audit = await ReadJsonUncheckedAsync<OriginalityAuditReport>(root, "analysis/originality-audit.json", cancellationToken);
        if (string.IsNullOrWhiteSpace(audit.ProjectId) || string.IsNullOrWhiteSpace(audit.PageId))
        {
            findings.Add(new ReadinessFinding("missing-originality-provenance", "blocking", "Originality audit is missing project or page provenance."));
        }

        if (audit.GenerationRestrictions.Count == 0)
        {
            findings.Add(new ReadinessFinding("empty-generation-restrictions", "blocking", "Originality audit has no generation restrictions."));
        }

        if (audit.Policy.TreatExternalAssetsAsReferenceOnly &&
            audit.ReferenceOnlyAssets.Any(asset => !asset.Reason.Contains("reference", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new ReadinessFinding("missing-reference-only-policy", "blocking", "Reference-only assets do not record a reference-only reason."));
        }

        if (audit.Policy.FlagLikelyBrandAssets &&
            audit.ReferenceOnlyAssets.Any(asset => asset.LikelyBrandAsset) &&
            !audit.Warnings.Any(warning => warning.Code.Contains("brand", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new ReadinessFinding("missing-reference-only-policy", "blocking", "Likely brand assets are missing a review warning."));
        }
    }

    private static bool HasAnyStyleValue(ElementEvidenceItem element) =>
        element.StyleGroups.Values.Any(group => group.Values.Any(value => !string.IsNullOrWhiteSpace(value)));

    private static bool HasStyleGroupValue(ElementEvidenceItem element, string groupName) =>
        element.StyleGroups.TryGetValue(groupName, out var group) &&
        group.Values.Any(value => !string.IsNullOrWhiteSpace(value));

    private async Task<TArtifact> ReadJsonUncheckedAsync<TArtifact>(
        string root,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var fullPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create(relativePath));
        var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<TArtifact>(json, VisualJson.Options)
            ?? throw new InvalidOperationException($"Artifact did not deserialize: {relativePath}");
    }

    private static void ValidateCorrelation(
        string pageId,
        string viewportId,
        string? manifestCorrelationId,
        string? elementCorrelationId,
        string? assetCorrelationId,
        List<ReadinessFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(manifestCorrelationId) ||
            string.IsNullOrWhiteSpace(elementCorrelationId) ||
            string.IsNullOrWhiteSpace(assetCorrelationId))
        {
            findings.Add(new ReadinessFinding("missing-capture-correlation", "blocking", $"Capture correlation is missing for {pageId}/{viewportId}."));
            return;
        }

        if (!string.Equals(manifestCorrelationId, elementCorrelationId, StringComparison.Ordinal) ||
            !string.Equals(manifestCorrelationId, assetCorrelationId, StringComparison.Ordinal))
        {
            findings.Add(new ReadinessFinding("capture-correlation-mismatch", "blocking", $"Capture correlation mismatch for {pageId}/{viewportId}."));
        }
    }

    private void ValidateQualityArtifactShape(
        string root,
        string captureRoot,
        CaptureQualityReport quality,
        List<ReadinessFinding> findings)
    {
        if (!quality.FinalWidth.HasValue || !quality.FinalHeight.HasValue)
        {
            findings.Add(new ReadinessFinding("quality-failed", "blocking", $"Capture quality report has no final dimensions for {captureRoot}."));
        }

        var screenshotPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create($"{captureRoot}/full-page.png"));
        if (File.Exists(screenshotPath))
        {
            try
            {
                using var image = new MagickImage(screenshotPath);
                if (quality.FinalWidth.HasValue && image.Width != (uint)quality.FinalWidth.Value ||
                    quality.FinalHeight.HasValue && image.Height != (uint)quality.FinalHeight.Value)
                {
                    findings.Add(new ReadinessFinding("quality-failed", "blocking", $"Screenshot dimensions do not match quality report for {captureRoot}."));
                }
            }
            catch (MagickException exception)
            {
                findings.Add(new ReadinessFinding("quality-failed", "blocking", $"Screenshot does not decode for {captureRoot}: {exception.Message}"));
            }
        }

        if (quality.FinalMethod == "stitched" || quality.CaptureMethod == "stitched")
        {
            if (quality.SegmentCount is null or <= 0)
            {
                findings.Add(new ReadinessFinding("invalid-stitch-artifact", "blocking", $"Stitched capture has no segment count for {captureRoot}."));
            }

            if (string.IsNullOrWhiteSpace(quality.FallbackReason))
            {
                findings.Add(new ReadinessFinding("invalid-stitch-artifact", "blocking", $"Stitched capture has no fallback reason for {captureRoot}."));
            }

            var stitchManifest = resolver.ResolveArtifactPath(root, ArtifactPath.Create($"{captureRoot}/stitch-manifest.json"));
            if (!File.Exists(stitchManifest))
            {
                findings.Add(new ReadinessFinding("missing-stitch-manifest", "blocking", $"Stitched capture is missing stitch-manifest.json for {captureRoot}."));
            }
        }
    }

    private async Task ValidateArtifactByPath(FileSystemVisualArtifactStore store, string path, CancellationToken cancellationToken)
    {
        var kind = path switch
        {
            "project.json" => "visual-project",
            "configuration.json" => "configuration",
            "discovery/site-profile.json" => "reference-site-profile",
            "discovery/reconnaissance.json" => "reconnaissance",
            "discovery/capture-plan.json" => "capture-plan",
            "analysis/page-topology.draft.json" => "page-topology-draft",
            "analysis/visual-blueprint.draft.json" => "visual-blueprint-draft",
            "analysis/originality-audit.json" => "originality-audit",
            "reports/readiness-report.json" => "readiness-report",
            _ when path.EndsWith("/manifest.json", StringComparison.Ordinal) => "capture-viewport-manifest",
            _ when path.EndsWith("/capture-manifest.json", StringComparison.Ordinal) => "page-capture-manifest",
            _ when path.EndsWith("/element-evidence-index.json", StringComparison.Ordinal) => "computed-style-evidence",
            _ when path.EndsWith("/asset-inventory.normalized.json", StringComparison.Ordinal) => "asset-inventory",
            _ when path.EndsWith("/capture-quality-report.json", StringComparison.Ordinal) => "capture-quality-report",
            _ => ""
        };

        if (!string.IsNullOrWhiteSpace(kind))
        {
            _ = await store.ReadJsonAsync<object>(ArtifactPath.Create(path), kind, cancellationToken);
        }
    }

    private void ValidateBlueprintEvidenceReferences(
        string root,
        FileSystemVisualArtifactStore store,
        List<ReadinessFinding> findings,
        CancellationToken cancellationToken)
    {
        var blueprintPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create("analysis/visual-blueprint.draft.json"));
        if (!File.Exists(blueprintPath))
        {
            return;
        }

        var blueprint = store.ReadJsonAsync<VisualBlueprintDraft>(ArtifactPath.Create("analysis/visual-blueprint.draft.json"), "visual-blueprint-draft", cancellationToken).GetAwaiter().GetResult();
        if (blueprint.EvidenceIds.Count == 0)
        {
            findings.Add(new ReadinessFinding("missing-evidence-reference", "blocking", "Visual blueprint has no evidence references."));
            return;
        }

        var capturesRoot = Path.Combine(root, "captures");
        if (!Directory.Exists(capturesRoot))
        {
            findings.Add(new ReadinessFinding("missing-evidence-reference", "blocking", "Capture evidence root is missing."));
            return;
        }

        var availableEvidenceIds = Directory.EnumerateFiles(capturesRoot, "element-evidence-index.json", SearchOption.AllDirectories)
            .Select(path => System.Text.Json.JsonSerializer.Deserialize<ElementEvidenceIndex>(File.ReadAllText(path), VisualJson.Options))
            .Where(index => index is not null)
            .SelectMany(index => index!.Elements.Select(element => element.EvidenceId))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var evidenceId in blueprint.EvidenceIds.Where(evidenceId => !availableEvidenceIds.Contains(evidenceId)))
        {
            findings.Add(new ReadinessFinding("missing-evidence-reference", "blocking", $"Visual blueprint references missing evidence id: {evidenceId}"));
        }
    }

    private void ValidateSensitiveRedaction(string root, IReadOnlyList<string> required, List<ReadinessFinding> findings)
    {
        foreach (var path in required.Where(path => path.EndsWith(".json", StringComparison.Ordinal)))
        {
            var fullPath = resolver.ResolveArtifactPath(root, ArtifactPath.Create(path));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var content = File.ReadAllText(fullPath);
            if (content.Contains("Authorization:", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("Set-Cookie:", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new ReadinessFinding("missing-provenance", "blocking", $"Sensitive header-like content was found in {path}."));
            }
        }
    }

    private static string? FindLatestRunId(string projectRoot)
    {
        var runsRoot = Path.Combine(projectRoot, "runs");
        return Directory.Exists(runsRoot)
            ? Directory.EnumerateFiles(runsRoot, "*.json").OrderByDescending(File.GetLastWriteTimeUtc).Select(Path.GetFileNameWithoutExtension).FirstOrDefault()
            : null;
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
        steps.Add(new AggregateEvidenceStep());
        steps.Add(new ExtractRawDesignTokensStep());
        steps.Add(new NormalizeSemanticTokensStep());
        steps.Add(new ClassifyPageArchetypesStep());
        steps.Add(new SegmentSectionsStep());
        steps.Add(new AnalyzeResponsiveInteractionsStep());
        steps.Add(new DetectComponentCandidatesStep());
        steps.Add(new ClassifyEcommerceRegionsStep());
        steps.Add(new BuildStorefrontPatternStep());
        steps.Add(new BuildPresentationCatalogStep());
        steps.Add(new MapPresentationComponentsStep());
        steps.Add(new ScoreConfidenceReviewStep());
        steps.Add(new AssembleBlueprintV1Step());
        return steps;
    }

    private static bool IsPhase3BDownstreamStep(string stepName) =>
        stepName is "aggregate-evidence" or "extract-raw-tokens" or "normalize-semantic-tokens" or "classify-page-archetypes" or "segment-sections" or "analyze-responsive-interactions" or "detect-component-candidates" or "classify-ecommerce-regions" or "build-storefront-pattern" or "build-presentation-catalog" or "map-presentation-components" or "score-confidence-review" or "assemble-blueprint-v1";

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
