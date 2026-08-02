using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Domain;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        var phase3B = InspectPhase3B(projectRoot, readiness?.Passed);

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
            inspectionWarnings.Count == 0 ? null : string.Join(" | ", inspectionWarnings),
            phase3B);
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

    private Phase3BInspection InspectPhase3B(string projectRoot, bool? phase3AReadinessPassed)
    {
        var evidence = InspectArtifact(projectRoot, "analysis/evidence-snapshot.json", "evidence-snapshot");
        var rawTokens = InspectArtifact(projectRoot, "analysis/tokens/raw-design-tokens.json", "raw-design-tokens");
        var semanticTokens = InspectArtifact(projectRoot, "analysis/tokens/semantic-tokens.draft.json", "semantic-tokens");
        var mappings = InspectArtifact(projectRoot, "analysis/mapping/presentation-mappings.draft.json", "presentation-mappings");
        var unsupported = InspectArtifact(projectRoot, "analysis/mapping/unsupported-patterns.json", "unsupported-patterns");
        var catalogValidation = InspectArtifact(projectRoot, "presentation-catalog/catalog-validation-report.json", "presentation-catalog-validation-report");
        var reviewQueue = InspectArtifact(projectRoot, "review/review-queue.json", "review-queue");
        var reviewDecisions = InspectArtifact(projectRoot, "review/review-decisions.json", "review-decisions");
        var reviewResolution = InspectArtifact(projectRoot, "analysis/resolved/review-resolution-manifest.json", "review-resolution-manifest");
        var reviewedBlueprint = InspectArtifact(projectRoot, "analysis/visual-blueprint.v1.reviewed.json", "visual-blueprint-v1");
        var pageSlotContracts = InspectArtifact(projectRoot, "analysis/storefront-pattern/page-contracts.json", "page-contracts");
        var generationReadiness = InspectArtifact(projectRoot, "reports/generation-readiness.json", "generation-readiness");
        var handoffManifest = InspectArtifact(projectRoot, "analysis/agent-handoff/manifest.json", "agent-handoff-manifest");
        var evidenceManifest = InspectArtifact(projectRoot, "analysis/agent-handoff/evidence-manifest.json", "agent-handoff-evidence-manifest");
        var handoffReadiness = InspectArtifact(projectRoot, "analysis/agent-handoff/handoff-readiness.json", "agent-handoff-readiness");

        var pageIds = evidence.Node?["pages"]?.AsArray()
            .Select(page => page?["pageId"]?.GetValue<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        var pageRoot = Path.Combine(projectRoot, "analysis", "pages");
        if (pageIds.Length == 0 && Directory.Exists(pageRoot))
        {
            pageIds = Directory.EnumerateDirectories(pageRoot)
                .Select(Path.GetFileName)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        var archetypeArtifacts = pageIds
            .Select(pageId => InspectArtifact(projectRoot, $"analysis/pages/{pageId}/page-archetype.json", "page-archetype"))
            .ToArray();
        var sectionArtifacts = pageIds
            .Select(pageId => InspectArtifact(projectRoot, $"analysis/pages/{pageId}/sections.draft.json", "sections"))
            .ToArray();

        var reviewQueueCount = reviewQueue.Node?["items"]?.AsArray().Count;
        var blockingReviewCount = reviewQueue.Node?["items"]?.AsArray()
            .Count(item => item?["blocking"]?.GetValue<bool>() == true);
        var decisionTotals = BuildReviewDecisionTotals(reviewQueue.Node, reviewDecisions.Node);

        var generationPassed = generationReadiness.Node?["passed"]?.GetValue<bool>();
        var generationFindings = ReadGenerationFindings(generationReadiness.Node);
        var latestBlockingFinding = generationFindings
            .Where(finding => string.Equals(finding.Severity, "blocking", StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();
        var handoffPassed = handoffReadiness.Node?["passed"]?.GetValue<bool>();
        var handoffFindings = ReadGenerationFindings(handoffReadiness.Node);
        var latestHandoffBlockingFinding = handoffFindings
            .Where(finding => string.Equals(finding.Severity, "blocking", StringComparison.OrdinalIgnoreCase))
            .LastOrDefault();
        var latestFinalBlockingFinding = latestHandoffBlockingFinding ?? latestBlockingFinding;
        var screenshotCount = evidenceManifest.Node?["pages"]?.AsArray()
            .Sum(page => page?["screenshots"]?.AsArray().Count ?? 0) ?? 0;
        var sectionCropCount = evidenceManifest.Node?["pages"]?.AsArray()
            .Sum(page => page?["sections"]?.AsArray().Count ?? 0) ?? 0;
        var missingEvidenceCount = generationFindings
            .Concat(handoffFindings)
            .Count(finding => finding.Code is "missing-page-evidence" or "missing-section-evidence" or "missing-section-screenshot" or "missing-screenshot" or "missing-agent-handoff-evidence");
        var blockerFix = latestFinalBlockingFinding is null
            ? "(none)"
            : SuggestedPhase3DFix(latestFinalBlockingFinding);

        var problems = BuildPhase3BProblems(
            phase3AReadinessPassed,
            evidence,
            rawTokens,
            semanticTokens,
            catalogValidation,
            reviewQueue,
            blockingReviewCount ?? 0,
            unsupported,
            generationFindings);

        return new Phase3BInspection(
            evidence,
            rawTokens,
            semanticTokens,
            BuildCountStatus(archetypeArtifacts),
            BuildCountStatus(sectionArtifacts),
            mappings,
            unsupported,
            reviewQueue,
            reviewQueueCount,
            decisionTotals,
            reviewResolution,
            reviewResolution.Node?["decisionBundleHash"]?.GetValue<string>(),
            reviewedBlueprint,
            pageSlotContracts,
            generationReadiness,
            generationPassed,
            latestBlockingFinding,
            CountFindings(generationFindings, "missing-required-slot"),
            CountFindings(generationFindings, "duplicate-required-slot", "duplicate-non-repeatable-slot"),
            CountFindings(generationFindings, "unapproved-extra-section"),
            handoffManifest,
            evidenceManifest,
            screenshotCount,
            sectionCropCount,
            missingEvidenceCount,
            handoffManifest.Node?["packageHash"]?.GetValue<string>() ?? FileHash(projectRoot, "analysis/agent-handoff/manifest.json"),
            handoffReadiness,
            handoffPassed,
            handoffFindings.Count(finding => string.Equals(finding.Severity, "blocking", StringComparison.OrdinalIgnoreCase)),
            handoffFindings.Count(finding => string.Equals(finding.Severity, "warning", StringComparison.OrdinalIgnoreCase)),
            latestHandoffBlockingFinding,
            latestFinalBlockingFinding,
            blockerFix,
            problems);
    }

    private Phase3BArtifactInspection InspectArtifact(string projectRoot, string relativePath, string artifactKind)
    {
        var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return new Phase3BArtifactInspection(relativePath, fullPath, "missing", null, null);
        }

        try
        {
            var node = JsonNode.Parse(File.ReadAllText(fullPath))
                ?? throw new JsonException("Artifact is empty.");
            schemaValidator.Validate(artifactKind, node);
            return new Phase3BArtifactInspection(relativePath, fullPath, "present", node, null);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return new Phase3BArtifactInspection(relativePath, fullPath, "invalid", null, exception.Message);
        }
    }

    private static Phase3BGroupInspection BuildCountStatus(IReadOnlyList<Phase3BArtifactInspection> artifacts)
    {
        var expected = artifacts.Count;
        var present = artifacts.Count(artifact => artifact.Status == "present");
        var invalid = artifacts.Count(artifact => artifact.Status == "invalid");
        var missing = artifacts.Count(artifact => artifact.Status == "missing");
        return new Phase3BGroupInspection(expected, present, missing, invalid);
    }

    private static IReadOnlyList<GenerationReadinessFinding> ReadGenerationFindings(JsonNode? node)
    {
        var findings = node?["findings"]?.AsArray();
        if (findings is null)
        {
            return [];
        }

        return findings
            .Select(finding => new GenerationReadinessFinding(
                finding?["code"]?.GetValue<string>() ?? "unknown",
                finding?["severity"]?.GetValue<string>() ?? "unknown",
                finding?["message"]?.GetValue<string>() ?? "",
                finding?["artifactPath"]?.GetValue<string>()))
            .ToArray();
    }

    private static ReviewDecisionTotals BuildReviewDecisionTotals(JsonNode? queueNode, JsonNode? decisionsNode)
    {
        var decisions = decisionsNode?["decisions"]?.AsArray();
        if (decisions is null)
        {
            return new ReviewDecisionTotals(0, 0, 0, 0, 0);
        }

        var queueById = queueNode?["items"]?.AsArray()
            .Select(item => new
            {
                ItemId = item?["itemId"]?.GetValue<string>(),
                SourceArtifactId = item?["sourceArtifactId"]?.GetValue<string>(),
                SourceArtifactHash = item?["sourceArtifactHash"]?.GetValue<string>()
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.ItemId))
            .ToDictionary(item => item.ItemId!, StringComparer.Ordinal) ?? [];

        var approved = 0;
        var modified = 0;
        var rejected = 0;
        var deferred = 0;
        var stale = 0;
        foreach (var decision in decisions)
        {
            var status = decision?["status"]?.GetValue<string>() ?? "";
            if (string.Equals(status, "Approved", StringComparison.Ordinal)) approved++;
            if (string.Equals(status, "Modified", StringComparison.Ordinal)) modified++;
            if (string.Equals(status, "Rejected", StringComparison.Ordinal)) rejected++;
            if (string.Equals(status, "Deferred", StringComparison.Ordinal)) deferred++;

            var itemId = decision?["itemId"]?.GetValue<string>();
            if (itemId is null || !queueById.TryGetValue(itemId, out var queueItem))
            {
                continue;
            }

            var sourceArtifactId = decision?["sourceArtifactId"]?.GetValue<string>();
            var sourceArtifactHash = decision?["sourceArtifactHash"]?.GetValue<string>();
            if (!string.Equals(sourceArtifactId, queueItem.SourceArtifactId, StringComparison.Ordinal) ||
                !string.Equals(sourceArtifactHash, queueItem.SourceArtifactHash, StringComparison.Ordinal))
            {
                stale++;
            }
        }

        return new ReviewDecisionTotals(approved, modified, rejected, deferred, stale);
    }

    private static int CountFindings(IReadOnlyList<GenerationReadinessFinding> findings, params string[] codes) =>
        findings.Count(finding => codes.Contains(finding.Code, StringComparer.Ordinal));

    private static string SuggestedPhase3DFix(GenerationReadinessFinding finding) =>
        finding.Code switch
        {
            "reviewed-blueprint-not-resolved" or "missing-review-decisions" =>
                "Complete review/review-decisions.json, then run resume --project <project> --force-step assemble-blueprint-v1.",
            "reviewed-blueprint-references-draft" or "reviewed-blueprint-hash-stale" =>
                "Rerun resume --project <project> --force-step assemble-blueprint-v1 after regenerating reviewed inputs.",
            "missing-required-slot" or "duplicate-required-slot" or "duplicate-non-repeatable-slot" or "unapproved-extra-section" =>
                "Fix reviewed page compositions, then rerun resume --project <project> --force-step assemble-blueprint-v1.",
            "missing-section-evidence" or "missing-page-evidence" or "missing-section-screenshot" or "missing-screenshot" =>
                "Regenerate capture/evidence artifacts, then rerun resume --project <project> --force-step assemble-agent-handoff.",
            "handoff-hash-mismatch" or "missing-agent-handoff-artifact" =>
                "Rerun resume --project <project> --force-step assemble-agent-handoff, then validate-agent-handoff-readiness.",
            _ => "Inspect the listed artifact path, fix the blocker, and rerun the failed force-step."
        };

    private static string? FileHash(string projectRoot, string relativePath)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()
            : null;
    }

    private static IReadOnlyList<Phase3BProblem> BuildPhase3BProblems(
        bool? phase3AReadinessPassed,
        Phase3BArtifactInspection evidence,
        Phase3BArtifactInspection rawTokens,
        Phase3BArtifactInspection semanticTokens,
        Phase3BArtifactInspection catalogValidation,
        Phase3BArtifactInspection reviewQueue,
        int blockingReviewCount,
        Phase3BArtifactInspection unsupported,
        IReadOnlyList<GenerationReadinessFinding> generationFindings)
    {
        var problems = new List<Phase3BProblem>();

        if (phase3AReadinessPassed != true)
        {
            problems.Add(new Phase3BProblem(
                "missing Phase 3A readiness",
                "Phase 3B requires reports/readiness-report.json with passed=true before analysis artifacts are trusted.",
                "Run validate or a successful no-AI workflow before rerunning Phase 3B steps."));
        }

        if (evidence.Status == "missing")
        {
            problems.Add(new Phase3BProblem(
                "missing evidence snapshot",
                "aggregate-evidence has not produced analysis/evidence-snapshot.json for this project.",
                "Run resume --project <project> --force-step aggregate-evidence after Phase 3A readiness passes."));
        }

        if (rawTokens.Status == "invalid" || semanticTokens.Status == "invalid")
        {
            problems.Add(new Phase3BProblem(
                "invalid token schema",
                "The raw or semantic token artifact is present but does not satisfy the registered schema.",
                "Regenerate tokens with --force-step extract-raw-tokens or --force-step normalize-semantic-tokens."));
        }

        var catalogDrift = catalogValidation.Node?["passed"]?.GetValue<bool>() == false || catalogValidation.Status == "invalid";
        if (catalogDrift)
        {
            problems.Add(new Phase3BProblem(
                "catalog drift",
                "The Presentation catalog validation report is failing or unreadable.",
                "Update the catalog builder against current Presentation/Starter contracts, then rerun build-presentation-catalog."));
        }

        var unresolvedReview = blockingReviewCount > 0 ||
            generationFindings.Any(finding => string.Equals(finding.Code, "missing-review-decisions", StringComparison.Ordinal));
        if (unresolvedReview && reviewQueue.Status != "missing")
        {
            problems.Add(new Phase3BProblem(
                "unresolved blocking review item",
                "The review queue still contains blocking items or generation readiness found missing review decisions.",
                "Write review/review-decisions.json for blocking items, then rerun score-confidence-review and assemble-blueprint-v1."));
        }

        var unsupportedCritical = unsupported.Node?["patterns"]?.AsArray()
            .Any(pattern => pattern?["humanReviewRequired"]?.GetValue<bool>() == true) == true ||
            generationFindings.Any(finding => string.Equals(finding.Code, "missing-mapping-for-critical-region", StringComparison.Ordinal));
        if (unsupportedCritical)
        {
            problems.Add(new Phase3BProblem(
                "unsupported critical pattern",
                "At least one ecommerce-critical visual pattern has no supported Presentation mapping.",
                "Resolve the pattern through review or add supported Presentation capability before generation consumes the blueprint."));
        }

        return problems;
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
    string? InspectionWarning,
    Phase3BInspection Phase3B);

public sealed record Phase3BInspection(
    Phase3BArtifactInspection EvidenceSnapshot,
    Phase3BArtifactInspection RawTokens,
    Phase3BArtifactInspection SemanticTokens,
    Phase3BGroupInspection Archetypes,
    Phase3BGroupInspection Sections,
    Phase3BArtifactInspection Mappings,
    Phase3BArtifactInspection UnsupportedPatterns,
    Phase3BArtifactInspection ReviewQueue,
    int? ReviewQueueCount,
    ReviewDecisionTotals ReviewDecisionTotals,
    Phase3BArtifactInspection ReviewResolution,
    string? ReviewBundleHash,
    Phase3BArtifactInspection ReviewedBlueprint,
    Phase3BArtifactInspection PageSlotContracts,
    Phase3BArtifactInspection GenerationReadiness,
    bool? GenerationReadinessPassed,
    GenerationReadinessFinding? LatestBlockingFinding,
    int MissingRequiredSlotCount,
    int DuplicateSlotCount,
    int UnapprovedExtraSectionCount,
    Phase3BArtifactInspection AgentHandoffManifest,
    Phase3BArtifactInspection AgentHandoffEvidenceManifest,
    int HandoffScreenshotCount,
    int HandoffSectionCropCount,
    int MissingEvidenceCount,
    string? HandoffPackageHash,
    Phase3BArtifactInspection AgentHandoffReadiness,
    bool? AgentHandoffReadinessPassed,
    int AgentHandoffBlockerCount,
    int AgentHandoffWarningCount,
    GenerationReadinessFinding? LatestAgentHandoffBlockingFinding,
    GenerationReadinessFinding? LatestFinalBlockingFinding,
    string LatestFinalBlockerFix,
    IReadOnlyList<Phase3BProblem> Problems);

public sealed record ReviewDecisionTotals(
    int Approved,
    int Modified,
    int Rejected,
    int Deferred,
    int Stale);

public sealed record Phase3BArtifactInspection(
    string RelativePath,
    string FullPath,
    string Status,
    JsonNode? Node,
    string? Error);

public sealed record Phase3BGroupInspection(
    int Expected,
    int Present,
    int Missing,
    int Invalid);

public sealed record Phase3BProblem(
    string Problem,
    string Cause,
    string Fix);
