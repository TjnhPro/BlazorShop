using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Browser;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;

namespace BlazorShop.AI.StorefrontReverseEngineering.Cli;

public static class CliHost
{
    private static readonly string[] KnownCommands =
    [
        "init",
        "discover",
        "capture",
        "analyze",
        "dry-run-handoff",
        "inspect",
        "inspect-handoff",
        "resolve-safe-review",
        "validate",
        "validate-handoff",
        "run",
        "resume"
    ];

    public static Task<int> RunAsync(
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            WriteHelp(output);
            return Task.FromResult(0);
        }

        var command = args[0].Trim().ToLowerInvariant();
        if (!KnownCommands.Contains(command, StringComparer.Ordinal))
        {
            error.WriteLine($"[SRE-CLI-001] Unknown command '{args[0]}'. Problem: command is not supported. Cause: Storefront reverse engineering only exposes known workflow commands. Fix: run with --help and choose a listed command.");
            return Task.FromResult(2);
        }

        return RunCommandAsync(command, args[1..], output, error, cancellationToken);
    }

    public static void WriteHelp(TextWriter output)
    {
        output.WriteLine("BlazorShop.AI.StorefrontReverseEngineering");
        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering -- <command> [options]");
        output.WriteLine();
        output.WriteLine("Commands:");
        foreach (var command in KnownCommands)
        {
            output.WriteLine($"  {command}");
        }
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  init --url <url> --name <name> --output-root obj/storefront-reverse-engineering/projects [--force]");
        output.WriteLine("  run --url <url> --name <name> --output-root obj/storefront-reverse-engineering/projects --no-ai [--run-id <id>] [--force-step <step>]");
        output.WriteLine("  resume --project obj/storefront-reverse-engineering/projects/<project-id> [--run-id <id>] [--force-step <step>]");
        output.WriteLine("  inspect --project obj/storefront-reverse-engineering/projects/<project-id>");
        output.WriteLine("  resolve-safe-review --project obj/storefront-reverse-engineering/projects/<project-id>");
        output.WriteLine("  validate-handoff --handoff-root <path> --schema-root <path>");
        output.WriteLine("  inspect-handoff --handoff-root <path> --schema-root <path>");
        output.WriteLine("  dry-run-handoff --handoff-root <path> --schema-root <path>");
        output.WriteLine();
        output.WriteLine("Phase 3B force-step values:");
        output.WriteLine("  aggregate-evidence, extract-raw-tokens, normalize-semantic-tokens, classify-page-archetypes");
        output.WriteLine("  segment-sections, analyze-responsive-interactions, detect-component-candidates");
        output.WriteLine("  classify-ecommerce-regions, build-storefront-pattern, build-presentation-catalog, map-presentation-components");
        output.WriteLine("  score-confidence-review, assemble-blueprint-v1");
    }

    private static async Task<int> RunCommandAsync(
        string command,
        string[] args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        try
        {
            var options = CommandOptions.Parse(args);
            var service = new VisualProjectService(FindRepositoryRoot());
            switch (command)
            {
                case "init":
                    var project = await service.InitializeAsync(
                        options.GetRequired("url", "SRE-INIT-002"),
                        options.GetRequired("name", "SRE-INIT-003"),
                        options.GetRequired("output-root", "SRE-INIT-004"),
                        options.HasFlag("force"),
                        cancellationToken);
                    output.WriteLine($"Visual project initialized: {project.ProjectId}");
                    output.WriteLine($"Status: {project.Status}");
                    output.WriteLine($"Artifact root: {project.ArtifactRoot}");
                    return 0;
                case "inspect":
                    var inspection = await service.InspectAsync(
                        options.GetRequired("project", "SRE-INSPECT-002"),
                        cancellationToken);
                    output.WriteLine($"Project: {inspection.Project.ProjectId}");
                    output.WriteLine($"Name: {inspection.Project.Name}");
                    output.WriteLine($"Status: {inspection.Project.Status}");
                    output.WriteLine($"Source URL: {inspection.Project.ReferenceUrl}");
                    output.WriteLine($"Artifact root: {inspection.ArtifactRoot}");
                    output.WriteLine($"Latest run: {inspection.LatestRunId ?? "(none)"}");
                    output.WriteLine($"Latest run status: {inspection.LatestRunState}");
                    output.WriteLine($"Readiness passed: {FormatNullableBool(inspection.ReadinessPassed)}");
                    output.WriteLine($"Blocking findings: {inspection.BlockingFindingCount}");
                    output.WriteLine($"Warnings: {inspection.WarningCount}");
                    output.WriteLine($"Latest blocking finding: {FormatLatestBlockingFinding(inspection.LatestBlockingFinding)}");
                    output.WriteLine($"Blueprint path: {inspection.BlueprintPath}");
                    output.WriteLine($"Readiness report path: {inspection.ReadinessReportPath}");
                    output.WriteLine($"Readiness summary: {inspection.ReadinessSummary}");
                    if (inspection.InspectionWarning is not null)
                    {
                        output.WriteLine($"Inspection warning: {inspection.InspectionWarning}");
                    }

                    WriteRunInspection(output, inspection);
                    WritePhase3BInspection(output, inspection.Phase3B);

                    return 0;
                case "validate-handoff":
                case "inspect-handoff":
                    var handoffRoot = options.GetRequired("handoff-root", "SRE-HANDOFF-001");
                    var schemaRoot = options.GetRequired("schema-root", "SRE-HANDOFF-002");
                    var portableValidator = new PortableHandoffValidator();
                    var portableReport = await portableValidator.ValidateAsync(handoffRoot, schemaRoot, cancellationToken);
                    WritePortableHandoffInspection(output, portableReport);
                    return portableReport.Findings.Any(finding => finding.Severity == "blocking") ? 3 : 0;
                case "dry-run-handoff":
                    var dryRunRoot = options.GetRequired("handoff-root", "SRE-HANDOFF-001");
                    var dryRunSchemaRoot = options.GetRequired("schema-root", "SRE-HANDOFF-002");
                    var package = await new HandoffConsumerDryRunLoader().LoadAsync(dryRunRoot, dryRunSchemaRoot, cancellationToken);
                    WriteHandoffDryRun(output, package);
                    return 0;
                case "resolve-safe-review":
                    var reviewSummary = await new SafeReviewDecisionMaterializer(FindRepositoryRoot())
                        .MaterializeAsync(options.GetRequired("project", "SRE-REVIEW-003"), cancellationToken);
                    output.WriteLine($"Safe review decisions: approved={reviewSummary.Approved}; modified={reviewSummary.Modified}; blocked={reviewSummary.Blocked}; skipped={reviewSummary.Skipped}; stale={reviewSummary.Stale}");
                    output.WriteLine($"Decision path: {reviewSummary.DecisionPath}");
                    output.WriteLine($"Summary path: {reviewSummary.SummaryPath}");
                    foreach (var item in reviewSummary.Items.Where(item => item.Status is "Blocked"))
                    {
                        output.WriteLine($"Review blocker: {item.ItemId}");
                        output.WriteLine($"Cause: {item.Reason}");
                        output.WriteLine("Fix: Provide an explicit manual review decision or regenerate the upstream artifact with safe visual provenance.");
                    }

                    return reviewSummary.Blocked == 0 && reviewSummary.Stale == 0 ? 0 : 3;
                case "discover":
                    var projectPath = options.GetRequired("project", "SRE-DISCOVER-001");
                    var projectInspection = await service.InspectAsync(projectPath, cancellationToken);
                    var discoveryService = new VisualDiscoveryService(
                        FindRepositoryRoot(),
                        ReferenceBrowserFactory.Create(FindRepositoryRoot(), projectInspection.Project.ReferenceUrl));
                    var result = await discoveryService.DiscoverAsync(projectPath, cancellationToken);
                    output.WriteLine($"Discovery completed: {result.SiteProfile.ProjectId}");
                    output.WriteLine($"Title: {result.SiteProfile.Title ?? "(unknown)"}");
                    output.WriteLine($"Blockers: {result.Reconnaissance.Blockers.Count}");
                    output.WriteLine($"Capture pages: {result.CapturePlan.Pages.Count}");
                    return 0;
                case "capture":
                    var captured = await new VisualProjectWorkflowService(FindRepositoryRoot())
                        .CaptureAsync(options.GetRequired("project", "SRE-CAPTURE-001"), cancellationToken);
                    output.WriteLine($"Capture completed: {captured} viewport(s)");
                    return 0;
                case "analyze":
                    var blueprint = await new VisualProjectWorkflowService(FindRepositoryRoot())
                        .AnalyzeAsync(options.GetRequired("project", "SRE-ANALYZE-001"), options.HasFlag("no-ai"), cancellationToken);
                    output.WriteLine($"Analysis completed: {blueprint.ArtifactId}");
                    return 0;
                case "validate":
                    var report = await new VisualProjectWorkflowService(FindRepositoryRoot())
                        .ValidateAsync(options.GetRequired("project", "SRE-VALIDATE-001"), cancellationToken);
                    output.WriteLine($"Validation passed: {report.Passed}");
                    output.WriteLine($"Findings: {report.Findings.Count}");
                    return report.Passed ? 0 : 3;
                case "run":
                case "resume":
                    if (command == "resume" && options.GetOptional("project") is { } resumeProjectPath)
                    {
                        var resumeInspection = await service.InspectAsync(resumeProjectPath, cancellationToken);
                        var parent = Directory.GetParent(resumeInspection.Project.ArtifactRoot)?.FullName
                            ?? throw new InvalidOperationException("[SRE-RESUME-001] Cannot resolve project parent. Problem: project artifact root has no parent. Cause: resume needs the output root that owns the project. Fix: pass --url/--name/--output-root explicitly.");
                        var resumeSummary = await new VisualProjectWorkflowService(FindRepositoryRoot()).RunAsync(
                            resumeInspection.Project.ReferenceUrl,
                            resumeInspection.Project.Name,
                            parent,
                            force: false,
                            resume: true,
                            options.HasFlag("no-ai"),
                            cancellationToken,
                            options.GetOptional("run-id"),
                            options.GetOptional("force-step"));
                        output.WriteLine($"Run completed: {resumeSummary.ProjectId}");
                        output.WriteLine($"Artifact root: {resumeSummary.ArtifactRoot}");
                        output.WriteLine($"Run ID: {resumeSummary.RunId}");
                        output.WriteLine($"Run status: {resumeSummary.RunStatus}");
                        output.WriteLine($"Captured viewports: {resumeSummary.CapturedViewports}");
                        output.WriteLine($"Blueprint: {resumeSummary.BlueprintArtifactId}");
                        output.WriteLine($"Readiness passed: {resumeSummary.ReadinessPassed}");
                    return resumeSummary.ReadinessPassed && resumeSummary.RunStatus == WorkflowRunStatus.Succeeded ? 0 : 3;
                    }

                    var summary = await new VisualProjectWorkflowService(FindRepositoryRoot()).RunAsync(
                        options.GetRequired("url", "SRE-RUN-001"),
                        options.GetRequired("name", "SRE-RUN-002"),
                        options.GetRequired("output-root", "SRE-RUN-003"),
                        options.HasFlag("force"),
                        options.HasFlag("resume") || command == "resume",
                        options.HasFlag("no-ai"),
                        cancellationToken,
                        options.GetOptional("run-id"),
                        options.GetOptional("force-step"));
                    output.WriteLine($"Run completed: {summary.ProjectId}");
                    output.WriteLine($"Artifact root: {summary.ArtifactRoot}");
                    output.WriteLine($"Run ID: {summary.RunId}");
                    output.WriteLine($"Run status: {summary.RunStatus}");
                    output.WriteLine($"Captured viewports: {summary.CapturedViewports}");
                    output.WriteLine($"Blueprint: {summary.BlueprintArtifactId}");
                    output.WriteLine($"Readiness passed: {summary.ReadinessPassed}");
                    return summary.ReadinessPassed && summary.RunStatus == WorkflowRunStatus.Succeeded ? 0 : 3;
                default:
                    output.WriteLine($"StorefrontReverseEngineering command '{command}' is available. Implementation is added by later Phase 3A workflow phases.");
                    return 0;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Environment.CurrentDirectory;
    }

    private static string FormatNullableBool(bool? value) =>
        value.HasValue ? value.Value.ToString().ToLowerInvariant() : "unknown";

    private static string FormatLatestBlockingFinding(ReadinessFinding? finding) =>
        finding is null ? "(none)" : $"{finding.Code} - {finding.Message}";

    private static void WritePortableHandoffInspection(TextWriter output, PortableHandoffValidationReport report)
    {
        output.WriteLine($"Project ID: {report.ProjectId ?? "(unknown)"}");
        output.WriteLine($"Readiness passed: {report.ReadinessPassed}");
        output.WriteLine($"Package hash: {report.PackageHash ?? "(none)"}");
        output.WriteLine($"Artifact count: {report.ArtifactCount}");
        output.WriteLine($"Schema count: {report.SchemaCount}");
        output.WriteLine($"Consumer reference count: {report.ConsumerReferenceCount}");
        output.WriteLine($"Diagnostic provenance count: {report.DiagnosticProvenanceCount}");
        output.WriteLine($"First blocking finding: {FormatPortableFinding(report.Findings.FirstOrDefault(finding => finding.Severity == "blocking"))}");
    }

    private static void WriteHandoffDryRun(TextWriter output, HandoffConsumerDryRunPackage package)
    {
        output.WriteLine($"Project ID: {package.ProjectId}");
        output.WriteLine($"Readiness passed: {package.ReadinessReport.Passed}");
        output.WriteLine($"Page count: {package.Pages.Count}");
        output.WriteLine($"Allowed target file count: {package.AllowedTargetFiles.Count}");
        output.WriteLine($"Protected file count: {package.ProtectedFiles.Count}");
        output.WriteLine($"Evidence file count: {package.EvidenceFilePaths.Count}");
        output.WriteLine($"Unresolved region count: {package.UnresolvedRegions.Count}");
        output.WriteLine($"First unresolved region: {package.UnresolvedRegions.FirstOrDefault() ?? "(none)"}");
    }

    private static string FormatPortableFinding(PortableHandoffValidationFinding? finding) =>
        finding is null
            ? "(none)"
            : string.Join(" | ",
                new[]
                {
                    finding.Code,
                    finding.Message,
                    finding.Problem is null ? null : "Problem: " + finding.Problem,
                    finding.Cause is null ? null : "Cause: " + finding.Cause,
                    finding.FixSuggestion is null ? null : "Fix: " + finding.FixSuggestion
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static void WriteRunInspection(TextWriter output, VisualProjectInspection inspection)
    {
        if (inspection.LatestRunId is null)
        {
            output.WriteLine("Steps: (none)");
            return;
        }

        if (inspection.LatestRun is null)
        {
            output.WriteLine("Steps: (unavailable)");
            return;
        }

        output.WriteLine($"Run status: {inspection.LatestRun.Status}");
        output.WriteLine("Steps:");
        foreach (var step in inspection.LatestRun.Steps)
        {
            var latestFailure = step.Errors.LastOrDefault()?.Message ?? "";
            output.WriteLine($"  {step.Name}: {step.Status}; retries={step.RetryCount}; failure={latestFailure}");
        }
    }

    private static void WritePhase3BInspection(TextWriter output, Phase3BInspection phase3B)
    {
        output.WriteLine("Phase 3B artifacts:");
        output.WriteLine($"  Evidence snapshot: {FormatArtifact(phase3B.EvidenceSnapshot)}");
        output.WriteLine($"  Tokens: raw={phase3B.RawTokens.Status} ({phase3B.RawTokens.RelativePath}); semantic={phase3B.SemanticTokens.Status} ({phase3B.SemanticTokens.RelativePath})");
        output.WriteLine($"  Archetypes: {FormatGroup(phase3B.Archetypes)}");
        output.WriteLine($"  Sections: {FormatGroup(phase3B.Sections)}");
        output.WriteLine($"  Mapping: mappings={phase3B.Mappings.Status} ({phase3B.Mappings.RelativePath}); unsupported={phase3B.UnsupportedPatterns.Status} ({phase3B.UnsupportedPatterns.RelativePath})");
        output.WriteLine($"  Review queue count: {phase3B.ReviewQueueCount?.ToString() ?? "unknown"} ({phase3B.ReviewQueue.Status}; {phase3B.ReviewQueue.RelativePath})");
        output.WriteLine($"  Review decision totals: approved={phase3B.ReviewDecisionTotals.Approved}; modified={phase3B.ReviewDecisionTotals.Modified}; rejected={phase3B.ReviewDecisionTotals.Rejected}; deferred={phase3B.ReviewDecisionTotals.Deferred}; stale={phase3B.ReviewDecisionTotals.Stale}");
        output.WriteLine($"  Resolved artifacts: {phase3B.ReviewResolution.Status} ({phase3B.ReviewResolution.RelativePath}); bundle hash={phase3B.ReviewBundleHash ?? "(none)"}");
        output.WriteLine($"  Reviewed blueprint: {phase3B.ReviewedBlueprint.Status} ({phase3B.ReviewedBlueprint.RelativePath})");
        output.WriteLine($"  Page slot contracts: {phase3B.PageSlotContracts.Status} ({phase3B.PageSlotContracts.RelativePath})");
        output.WriteLine($"  Generation readiness: {FormatGenerationReadiness(phase3B)}");
        output.WriteLine($"  Slot blockers: missing required={phase3B.MissingRequiredSlotCount}; duplicate={phase3B.DuplicateSlotCount}; unapproved extras={phase3B.UnapprovedExtraSectionCount}");
        output.WriteLine($"  Latest Phase 3B blocking finding: {FormatLatestPhase3BFinding(phase3B.LatestBlockingFinding)}");
        output.WriteLine($"  Handoff manifest: {phase3B.AgentHandoffManifest.Status} ({phase3B.AgentHandoffManifest.RelativePath})");
        output.WriteLine($"  Handoff evidence manifest: {phase3B.AgentHandoffEvidenceManifest.Status} ({phase3B.AgentHandoffEvidenceManifest.RelativePath})");
        output.WriteLine($"  Handoff screenshots: {phase3B.HandoffScreenshotCount}; section crops: {phase3B.HandoffSectionCropCount}; missing evidence: {phase3B.MissingEvidenceCount}");
        output.WriteLine($"  Handoff package hash: {phase3B.HandoffPackageHash ?? "(none)"}");
        output.WriteLine($"  Final handoff readiness: {FormatAgentHandoffReadiness(phase3B)}");
        output.WriteLine($"  Final handoff blockers: {phase3B.AgentHandoffBlockerCount}; warnings: {phase3B.AgentHandoffWarningCount}");
        output.WriteLine($"  Latest final handoff blocker: {FormatLatestPhase3BFinding(phase3B.LatestAgentHandoffBlockingFinding)}");
        output.WriteLine($"  Latest final blocker: {FormatLatestPhase3BFinding(phase3B.LatestFinalBlockingFinding)}");
        output.WriteLine($"  Suggested fix: {phase3B.LatestFinalBlockerFix}");
        output.WriteLine($"  Agent handoff path: analysis/agent-handoff");
        output.WriteLine($"  Next recommended command: dotnet test tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter AgentHandoff");

        foreach (var problem in phase3B.Problems)
        {
            output.WriteLine($"Phase 3B problem: {problem.Problem}");
            output.WriteLine($"Cause: {problem.Cause}");
            output.WriteLine($"Fix: {problem.Fix}");
        }
    }

    private static string FormatArtifact(Phase3BArtifactInspection artifact) =>
        $"{artifact.Status} - {artifact.RelativePath}";

    private static string FormatGroup(Phase3BGroupInspection group) =>
        group.Expected == 0
            ? "missing - no page artifacts found"
            : $"present={group.Present}/{group.Expected}; missing={group.Missing}; invalid={group.Invalid}";

    private static string FormatGenerationReadiness(Phase3BInspection phase3B)
    {
        if (phase3B.GenerationReadiness.Status != "present")
        {
            return $"{phase3B.GenerationReadiness.Status} - {phase3B.GenerationReadiness.RelativePath}";
        }

        return $"{FormatNullableBool(phase3B.GenerationReadinessPassed)} - {phase3B.GenerationReadiness.RelativePath}";
    }

    private static string FormatAgentHandoffReadiness(Phase3BInspection phase3B)
    {
        if (phase3B.AgentHandoffReadiness.Status != "present")
        {
            return $"{phase3B.AgentHandoffReadiness.Status} - {phase3B.AgentHandoffReadiness.RelativePath}";
        }

        return $"{FormatNullableBool(phase3B.AgentHandoffReadinessPassed)} - {phase3B.AgentHandoffReadiness.RelativePath}";
    }

    private static string FormatLatestPhase3BFinding(GenerationReadinessFinding? finding) =>
        finding is null
            ? "(none)"
            : $"{finding.Code} - {finding.Message}" + (finding.ArtifactPath is null ? "" : $" ({finding.ArtifactPath})");
}
