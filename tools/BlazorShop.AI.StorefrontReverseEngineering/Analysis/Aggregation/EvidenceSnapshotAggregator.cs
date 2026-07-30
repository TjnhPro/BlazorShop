using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;

public sealed class EvidenceSnapshotAggregator
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public EvidenceSnapshotAggregator(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(this.repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<EvidenceSnapshot> BuildAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var preflight = await new Phase3BPreflightService(repoRoot).CheckAsync(root, cancellationToken);
        var issues = preflight.Issues
            .Select(issue => new EvidenceSnapshotIssue(issue.Code, issue.Severity, issue.Message))
            .ToList();
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var sourcePaths = new SortedSet<string>(StringComparer.Ordinal);
        var sourceEvidenceIds = new SortedSet<string>(StringComparer.Ordinal);

        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        sourcePaths.Add("project.json");
        var configuration = await store.ReadJsonAsync<VisualProjectConfiguration>(ArtifactPath.Create("configuration.json"), "configuration", cancellationToken);
        sourcePaths.Add("configuration.json");
        var plan = await store.ReadJsonAsync<CapturePlan>(ArtifactPath.Create("discovery/capture-plan.json"), "capture-plan", cancellationToken);
        sourcePaths.Add("discovery/capture-plan.json");
        sourcePaths.Add("reports/readiness-report.json");

        await TryReadAsync<OriginalityAuditReport>(
            store,
            "analysis/originality-audit.json",
            "originality-audit",
            issues,
            cancellationToken);
        sourcePaths.Add("analysis/originality-audit.json");
        await LoadInteractionEvidenceAsync(store, root, issues, sourcePaths, sourceEvidenceIds, cancellationToken);

        var pages = new List<EvidenceSnapshotPage>();
        foreach (var page in plan.Pages.Take(configuration.CapturePolicy.MaximumPages))
        {
            var pagePaths = new SortedSet<string>(StringComparer.Ordinal);
            var viewports = new List<EvidenceSnapshotViewport>();
            var pageManifestPath = $"captures/{page.PageId}/capture-manifest.json";
            var pageManifest = await TryReadAsync<PageCaptureManifest>(
                store,
                pageManifestPath,
                "page-capture-manifest",
                issues,
                cancellationToken,
                page.PageId);
            sourcePaths.Add(pageManifestPath);
            pagePaths.Add(pageManifestPath);

            foreach (var viewport in plan.Viewports)
            {
                var viewportSnapshot = await BuildViewportAsync(
                    store,
                    page,
                    viewport,
                    pageManifest,
                    issues,
                    sourcePaths,
                    sourceEvidenceIds,
                    cancellationToken);
                viewports.Add(viewportSnapshot);
            }

            pages.Add(new EvidenceSnapshotPage(page.PageId, page.Url, page.Label, viewports, pagePaths.ToArray()));
        }

        DetectOrphanEvidence(root, plan, issues, sourcePaths);

        var snapshot = new EvidenceSnapshot(
            "1.0",
            "evidence-snapshot",
            $"evidence-snapshot-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            preflight.LatestRunId,
            NormalizeProjectPath(root, preflight.ReadinessReportPath ?? Path.Combine(root, "reports", "readiness-report.json")),
            sourcePaths.ToArray(),
            sourceEvidenceIds.ToArray(),
            pages,
            issues);

        await store.WriteJsonAsync(ArtifactPath.Create("analysis/evidence-snapshot.json"), "evidence-snapshot", snapshot, cancellationToken);
        await File.WriteAllTextAsync(
            resolver.ResolveArtifactPath(root, ArtifactPath.Create("reports/evidence-snapshot.md")),
            WriteMarkdown(snapshot),
            cancellationToken);

        return snapshot;
    }

    private async Task<EvidenceSnapshotViewport> BuildViewportAsync(
        FileSystemVisualArtifactStore store,
        CapturePlanPage page,
        ViewportDefinition viewport,
        PageCaptureManifest? pageManifest,
        List<EvidenceSnapshotIssue> issues,
        SortedSet<string> sourcePaths,
        SortedSet<string> sourceEvidenceIds,
        CancellationToken cancellationToken)
    {
        var rootPath = $"captures/{page.PageId}/{viewport.Id}";
        var viewportIssues = new List<EvidenceSnapshotIssue>();
        var viewportPaths = new SortedSet<string>(StringComparer.Ordinal);
        var manifestPath = $"{rootPath}/manifest.json";
        var elementPath = $"{rootPath}/element-evidence-index.json";
        var assetPath = $"{rootPath}/asset-inventory.normalized.json";
        var qualityPath = $"{rootPath}/capture-quality-report.json";
        var expectedPaths = new[] { manifestPath, elementPath, assetPath, qualityPath };

        foreach (var expectedPath in expectedPaths)
        {
            sourcePaths.Add(expectedPath);
            viewportPaths.Add(expectedPath);
            if (!File.Exists(resolver.ResolveArtifactPath(store.Root, ArtifactPath.Create(expectedPath))))
            {
                AddIssue(
                    issues,
                    viewportIssues,
                    "missing-viewport-artifact",
                    "blocking",
                    $"Configured viewport artifact is missing: {expectedPath}",
                    page.PageId,
                    viewport.Id,
                    expectedPath);
            }
        }

        var manifest = await TryReadAsync<CaptureViewportManifest>(store, manifestPath, "capture-viewport-manifest", issues, cancellationToken, page.PageId, viewport.Id, viewportIssues);
        var elements = await TryReadAsync<ElementEvidenceIndex>(store, elementPath, "computed-style-evidence", issues, cancellationToken, page.PageId, viewport.Id, viewportIssues);
        var assets = await TryReadAsync<AssetInventoryEvidence>(store, assetPath, "asset-inventory", issues, cancellationToken, page.PageId, viewport.Id, viewportIssues);
        var quality = await TryReadAsync<CaptureQualityReport>(store, qualityPath, "capture-quality-report", issues, cancellationToken, page.PageId, viewport.Id, viewportIssues);

        var correlationCandidates = new[]
            {
                pageManifest?.CaptureCorrelationIds?.GetValueOrDefault(viewport.Id),
                manifest?.CaptureCorrelationId,
                elements?.CaptureCorrelationId,
                assets?.CaptureCorrelationId,
                quality?.CaptureCorrelationId
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (correlationCandidates.Length > 1)
        {
            AddIssue(
                issues,
                viewportIssues,
                "capture-correlation-mismatch",
                "blocking",
                $"Capture correlation mismatch for {page.PageId}/{viewport.Id}.",
                page.PageId,
                viewport.Id,
                rootPath);
        }

        foreach (var evidenceId in elements?.Elements.Select(element => element.EvidenceId) ?? [])
        {
            sourceEvidenceIds.Add(evidenceId);
        }

        foreach (var evidenceId in assets?.Assets.Select(asset => asset.EvidenceId) ?? [])
        {
            sourceEvidenceIds.Add(evidenceId);
        }

        return new EvidenceSnapshotViewport(
            viewport.Id,
            manifest?.ViewportWidth ?? viewport.Width,
            manifest?.ViewportHeight ?? viewport.Height,
            manifest?.DocumentWidth ?? 0,
            manifest?.DocumentHeight ?? 0,
            correlationCandidates.FirstOrDefault(),
            manifest?.CaptureMethod ?? "missing",
            quality?.Passed == true,
            (elements?.Elements ?? [])
                .Select(element => new EvidenceSnapshotElement(
                    element.EvidenceId,
                    element.Selector,
                    element.Category,
                    element.TextSnippet,
                    element.StyleGroups,
                    element.Box,
                    elementPath))
                .ToArray(),
            (assets?.Assets ?? [])
                .Select(asset => new EvidenceSnapshotAsset(
                    asset.EvidenceId,
                    asset.Url,
                    asset.MediaType,
                    asset.Width,
                    asset.Height,
                    asset.SourceElement,
                    asset.ReferenceOnly,
                    assetPath))
                .ToArray(),
            viewportPaths.ToArray(),
            viewportIssues);
    }

    private async Task<TArtifact?> TryReadAsync<TArtifact>(
        FileSystemVisualArtifactStore store,
        string relativePath,
        string artifactKind,
        List<EvidenceSnapshotIssue> issues,
        CancellationToken cancellationToken,
        string? pageId = null,
        string? viewportId = null,
        List<EvidenceSnapshotIssue>? scopedIssues = null)
    {
        if (!File.Exists(resolver.ResolveArtifactPath(store.Root, ArtifactPath.Create(relativePath))))
        {
            return default;
        }

        try
        {
            return await store.ReadJsonAsync<TArtifact>(ArtifactPath.Create(relativePath), artifactKind, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            AddIssue(
                issues,
                scopedIssues,
                "invalid-schema",
                "blocking",
                $"Artifact schema mismatch for {relativePath}: {exception.Message}",
                pageId,
                viewportId,
                relativePath);
            return default;
        }
    }

    private void DetectOrphanEvidence(
        string root,
        CapturePlan plan,
        List<EvidenceSnapshotIssue> issues,
        SortedSet<string> sourcePaths)
    {
        var configured = plan.Pages
            .SelectMany(page => plan.Viewports.Select(viewport => $"captures/{page.PageId}/{viewport.Id}/element-evidence-index.json"))
            .ToHashSet(StringComparer.Ordinal);
        var capturesRoot = resolver.ResolveArtifactPath(root, ArtifactPath.Create("captures"));
        if (!Directory.Exists(capturesRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(capturesRoot, "element-evidence-index.json", SearchOption.AllDirectories))
        {
            var relative = NormalizeProjectPath(root, file);
            sourcePaths.Add(relative);
            if (!configured.Contains(relative))
            {
                issues.Add(new EvidenceSnapshotIssue(
                    "orphan-evidence",
                    "warning",
                    $"Evidence file is not referenced by the configured capture plan: {relative}",
                    ArtifactPath: relative));
            }
        }
    }

    private async Task LoadInteractionEvidenceAsync(
        FileSystemVisualArtifactStore store,
        string root,
        List<EvidenceSnapshotIssue> issues,
        SortedSet<string> sourcePaths,
        SortedSet<string> sourceEvidenceIds,
        CancellationToken cancellationToken)
    {
        var interactionsRoot = resolver.ResolveArtifactPath(root, ArtifactPath.Create("interactions"));
        if (!Directory.Exists(interactionsRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(interactionsRoot, "interaction-evidence.json", SearchOption.AllDirectories))
        {
            var relative = NormalizeProjectPath(root, file);
            sourcePaths.Add(relative);
            var evidence = await TryReadAsync<InteractionEvidence>(
                store,
                relative,
                "interaction-evidence",
                issues,
                cancellationToken);
            foreach (var evidenceId in evidence?.ChangedElementEvidenceIds ?? [])
            {
                sourceEvidenceIds.Add(evidenceId);
            }
        }
    }

    private static void AddIssue(
        List<EvidenceSnapshotIssue> issues,
        List<EvidenceSnapshotIssue>? scopedIssues,
        string code,
        string severity,
        string message,
        string? pageId,
        string? viewportId,
        string? artifactPath)
    {
        var issue = new EvidenceSnapshotIssue(code, severity, message, pageId, viewportId, artifactPath);
        issues.Add(issue);
        scopedIssues?.Add(issue);
    }

    private static string WriteMarkdown(EvidenceSnapshot snapshot)
    {
        var lines = new List<string>
        {
            "# Evidence Snapshot",
            "",
            $"Project: `{snapshot.ProjectId}`",
            $"Latest run: `{snapshot.LatestRunId ?? "(none)"}`",
            $"Pages: `{snapshot.Pages.Count}`",
            $"Source evidence IDs: `{snapshot.SourceEvidenceIds.Count}`",
            $"Issues: `{snapshot.Issues.Count}`",
            "",
            "## Pages"
        };
        foreach (var page in snapshot.Pages)
        {
            lines.Add($"- `{page.PageId}`: {page.Viewports.Count} viewport(s)");
        }

        lines.Add("");
        lines.Add("## Issues");
        lines.AddRange(snapshot.Issues.Count == 0
            ? ["- None"]
            : snapshot.Issues.Select(issue => $"- `{issue.Code}` ({issue.Severity}) {issue.PageId}/{issue.ViewportId}: {issue.Message}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string NormalizeProjectPath(string root, string fullPath)
    {
        var relative = Path.GetRelativePath(root, fullPath);
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}
