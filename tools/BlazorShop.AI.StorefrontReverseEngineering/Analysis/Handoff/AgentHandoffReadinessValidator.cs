using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed class AgentHandoffReadinessValidator
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public AgentHandoffReadinessValidator(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(this.repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<AgentHandoffReadinessReport> ValidateAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var findings = new List<AgentHandoffReadinessFinding>();

        foreach (var artifact in AgentHandoffContract.RequiredArtifacts)
        {
            if (artifact.RelativePath == "analysis/agent-handoff/handoff-readiness.json")
            {
                continue;
            }

            var path = Path.Combine(root, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (artifact.IsDirectory)
            {
                if (!Directory.Exists(path) || !Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any())
                {
                    findings.Add(Block("missing-agent-handoff-artifact", $"Required handoff directory is missing or empty: {artifact.RelativePath}", artifact.RelativePath));
                }

                continue;
            }

            if (!File.Exists(path))
            {
                findings.Add(Block("missing-agent-handoff-artifact", $"Required handoff artifact is missing: {artifact.RelativePath}", artifact.RelativePath));
            }
        }

        AddContractArtifactFindings(root, findings);
        AddGenerationReadinessFindings(root, findings);
        AddAllowedProtectedFindings(root, findings);
        AddEvidenceManifestFindings(root, findings);
        AddTaskContractFindings(root, findings);
        AddSemanticContractFindings(root, findings);
        AddStaticBoundaryFindings(findings);

        var report = new AgentHandoffReadinessReport(
            "1.0",
            "agent-handoff-readiness",
            $"agent-handoff-readiness-{project.ProjectId}",
            project.CreatedUtc,
            project.ProjectId,
            findings.All(finding => finding.Severity != "blocking"),
            findings,
            "analysis/agent-handoff");
        await WriteAsync(root, report, cancellationToken);
        return report;
    }

    private static void AddGenerationReadinessFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var path = Path.Combine(root, "reports", "generation-readiness.json");
        if (!File.Exists(path))
        {
            findings.Add(Block("missing-agent-handoff-artifact", "Generation readiness is missing.", "reports/generation-readiness.json"));
            return;
        }

        var readiness = JsonSerializer.Deserialize<GenerationReadinessReport>(File.ReadAllText(path), VisualJson.Options);
        if (readiness is null)
        {
            findings.Add(Block("schema-validation-failed", "Generation readiness could not be parsed.", "reports/generation-readiness.json"));
            return;
        }

        foreach (var finding in readiness.Findings.Where(finding => finding.Severity == "blocking"))
        {
            findings.Add(Block(NormalizeBlockingCode(finding.Code), finding.Message, finding.ArtifactPath));
        }

        if (!readiness.Passed)
        {
            findings.Add(Block("blocking-unresolved-region", "Generation readiness has not passed.", "reports/generation-readiness.json"));
        }
    }

    private void AddContractArtifactFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var manifestPath = Path.Combine(root, "analysis", "agent-handoff", "manifest.json");
        AgentHandoffManifest? manifest = null;
        if (File.Exists(manifestPath))
        {
            manifest = JsonSerializer.Deserialize<AgentHandoffManifest>(File.ReadAllText(manifestPath), VisualJson.Options);
        }

        foreach (var artifact in AgentHandoffContract.RequiredArtifacts.Where(artifact => !artifact.IsDirectory))
        {
            if (artifact.RelativePath == "analysis/agent-handoff/handoff-readiness.json")
            {
                continue;
            }

            if (Path.IsPathRooted(artifact.RelativePath) || artifact.RelativePath.Split('/', '\\').Contains("..", StringComparer.Ordinal))
            {
                findings.Add(Block("absolute-source-dependency", $"Required artifact path is not portable: {artifact.RelativePath}", artifact.RelativePath));
                continue;
            }

            if (!artifact.RelativePath.StartsWith(AgentHandoffContract.HandoffRoot + "/", StringComparison.Ordinal))
            {
                findings.Add(Block("handoff-path-escape", $"Required artifact is outside handoff root: {artifact.RelativePath}", artifact.RelativePath));
                continue;
            }

            var path = Path.Combine(root, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            if (artifact.ContentType == "application/json")
            {
                AddJsonArtifactFindings(root, artifact, path, findings, validator);
            }

            var entry = manifest?.ArtifactEntries.FirstOrDefault(candidate => string.Equals(candidate.Path, artifact.RelativePath, StringComparison.Ordinal));
            if (manifest is not null &&
                !artifact.IsDirectory &&
                (!manifest.ArtifactList.Contains(artifact.RelativePath, StringComparer.Ordinal) || entry is null))
            {
                findings.Add(Block("missing-agent-handoff-artifact", $"Manifest is missing required artifact entry: {artifact.RelativePath}", "analysis/agent-handoff/manifest.json"));
            }

            if (entry is not null && artifact.HashRequired)
            {
                var actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
                if (!string.Equals(actualHash, entry.Sha256, StringComparison.Ordinal))
                {
                    findings.Add(Block("handoff-hash-mismatch", $"Manifest hash does not match artifact: {artifact.RelativePath}", artifact.RelativePath));
                }
            }
        }

        if (manifest is not null)
        {
            if (!string.Equals(manifest.HandoffRoot, AgentHandoffContract.HandoffRoot, StringComparison.Ordinal))
            {
                findings.Add(Block("handoff-path-escape", "Manifest handoffRoot does not match canonical handoff root.", "analysis/agent-handoff/manifest.json"));
            }

            if (manifest.ArtifactEntries.Any(entry => !entry.Path.StartsWith(AgentHandoffContract.HandoffRoot + "/", StringComparison.Ordinal)))
            {
                findings.Add(Block("handoff-path-escape", "Manifest contains artifact entry outside handoff root.", "analysis/agent-handoff/manifest.json"));
            }

            if (manifest.ArtifactList.Any(path => !path.StartsWith(AgentHandoffContract.HandoffRoot + "/", StringComparison.Ordinal)))
            {
                findings.Add(Block("handoff-path-escape", "Manifest artifact list contains a path outside handoff root.", "analysis/agent-handoff/manifest.json"));
            }

            foreach (var entry in manifest.ArtifactEntries.Where(entry => !string.IsNullOrWhiteSpace(entry.Sha256)))
            {
                var path = Path.Combine(root, entry.Path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                {
                    findings.Add(Block("missing-agent-handoff-artifact", $"Manifest declares missing artifact entry: {entry.Path}", "analysis/agent-handoff/manifest.json"));
                    continue;
                }

                var actualHash = FileHash(path);
                if (!string.Equals(actualHash, entry.Sha256, StringComparison.Ordinal))
                {
                    findings.Add(Block("handoff-hash-mismatch", $"Manifest entry hash does not match artifact: {entry.Path}", entry.Path));
                }
            }
        }
    }

    private static void AddJsonArtifactFindings(
        string root,
        RequiredHandoffArtifact artifact,
        string path,
        List<AgentHandoffReadinessFinding> findings,
        IVisualSchemaValidator validator)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            var node = System.Text.Json.Nodes.JsonNode.Parse(json);
            if (node is not null)
            {
                validator.Validate(artifact.ArtifactKind, node);
            }

            if (!document.RootElement.TryGetProperty("artifactKind", out var kind) ||
                !string.Equals(kind.GetString(), artifact.ArtifactKind, StringComparison.Ordinal))
            {
                findings.Add(Block("artifact-kind-mismatch", $"Artifact kind mismatch for {artifact.RelativePath}.", artifact.RelativePath));
            }

            if (document.RootElement.TryGetProperty("projectId", out var projectId) &&
                TryReadProjectId(root) is { } expected &&
                !string.Equals(projectId.GetString(), expected, StringComparison.Ordinal))
            {
                findings.Add(Block("project-id-mismatch", $"Artifact projectId mismatch for {artifact.RelativePath}.", artifact.RelativePath));
            }
        }
        catch (JsonException)
        {
            findings.Add(Block("invalid-agent-handoff-schema", $"JSON handoff artifact could not be parsed: {artifact.RelativePath}", artifact.RelativePath));
        }
        catch (InvalidOperationException exception)
        {
            findings.Add(Block("schema-validation-failed", exception.Message, artifact.RelativePath));
        }
    }

    private static string? TryReadProjectId(string root)
    {
        var path = Path.Combine(root, "project.json");
        if (!File.Exists(path))
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("projectId", out var projectId) ? projectId.GetString() : null;
    }

    private static void AddAllowedProtectedFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var allowedPath = Path.Combine(root, "analysis", "agent-handoff", "allowed-files.json");
        if (!File.Exists(allowedPath))
        {
            return;
        }

        var allowed = JsonSerializer.Deserialize<AgentHandoffFileManifest>(File.ReadAllText(allowedPath), VisualJson.Options);
        if (allowed is null)
        {
            findings.Add(Block("schema-validation-failed", "Allowed files manifest could not be parsed.", "analysis/agent-handoff/allowed-files.json"));
            return;
        }

        foreach (var path in allowed.Paths)
        {
            if (IsProtectedPath(path))
            {
                findings.Add(Block("protected-path-target", $"Allowed file manifest contains protected target '{path}'.", "analysis/agent-handoff/allowed-files.json"));
            }
        }

        var protectedPath = Path.Combine(root, "analysis", "agent-handoff", "protected-files.json");
        if (!File.Exists(protectedPath))
        {
            return;
        }

        var protectedFiles = JsonSerializer.Deserialize<AgentHandoffFileManifest>(File.ReadAllText(protectedPath), VisualJson.Options);
        if (protectedFiles is null)
        {
            findings.Add(Block("schema-validation-failed", "Protected files manifest could not be parsed.", "analysis/agent-handoff/protected-files.json"));
            return;
        }

        var overlap = allowed.Paths.Intersect(protectedFiles.Paths, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var path in overlap)
        {
            findings.Add(Block("allowed-protected-overlap", $"Allowed file path also appears in protected files: {path}", "analysis/agent-handoff/allowed-files.json"));
        }
    }

    private static void AddEvidenceManifestFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var manifestPath = Path.Combine(root, "analysis", "agent-handoff", "evidence-manifest.json");
        if (!File.Exists(manifestPath))
        {
            return;
        }

        var manifest = JsonSerializer.Deserialize<AgentHandoffEvidenceManifest>(File.ReadAllText(manifestPath), VisualJson.Options);
        if (manifest is null)
        {
            findings.Add(Block("schema-validation-failed", "Evidence manifest could not be parsed.", "analysis/agent-handoff/evidence-manifest.json"));
            return;
        }

        foreach (var screenshot in manifest.Pages.SelectMany(page => page.Screenshots))
        {
            ValidateEvidenceFile(root, screenshot.HandoffPath, screenshot.Sha256, "missing-handoff-screenshot", findings);
            if (screenshot.OriginalityRestrictions.Any(rule => string.Equals(rule, "production-safe", StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(Block("evidence-labeled-production-safe", $"Screenshot evidence must not be labeled production-safe: {screenshot.HandoffPath}", screenshot.HandoffPath));
            }
        }

        foreach (var section in manifest.Pages.SelectMany(page => page.Sections))
        {
            ValidateEvidenceFile(root, section.HandoffPath, section.Sha256, "missing-section-screenshot", findings);
            if (section.OriginalityRestrictions.Any(rule => string.Equals(rule, "production-safe", StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(Block("evidence-labeled-production-safe", $"Section evidence must not be labeled production-safe: {section.HandoffPath}", section.HandoffPath));
            }
        }
    }

    private static void AddTaskContractFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var path = Path.Combine(root, "analysis", "agent-handoff", "task.md");
        if (!File.Exists(path))
        {
            return;
        }

        var text = File.ReadAllText(path);
        foreach (var heading in RequiredTaskHeadings())
        {
            if (!text.Contains("## " + heading, StringComparison.Ordinal))
            {
                findings.Add(Block("missing-task-section", $"Handoff task is missing mandatory section '{heading}'.", "analysis/agent-handoff/task.md"));
            }
        }
    }

    private static IReadOnlyList<string> RequiredTaskHeadings() =>
    [
        "Objective",
        "Inputs",
        "Source of Truth Priority",
        "Allowed File Operations",
        "Protected Files",
        "Required Page Slots",
        "Optional Page Slots",
        "Section Order",
        "Responsive Evidence",
        "Interaction Evidence",
        "Originality Restrictions",
        "Forbidden Behavior",
        "Unsupported Handling",
        "Validation Commands",
        "Stop Conditions"
    ];

    private static void ValidateEvidenceFile(
        string root,
        string handoffPath,
        string expectedHash,
        string missingCode,
        List<AgentHandoffReadinessFinding> findings)
    {
        if (!handoffPath.StartsWith("analysis/agent-handoff/", StringComparison.Ordinal))
        {
            findings.Add(Block("handoff-path-escape", $"Evidence path escapes handoff root: {handoffPath}", handoffPath));
            return;
        }

        var path = Path.Combine(root, handoffPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            findings.Add(Block(missingCode, $"Evidence file is missing: {handoffPath}", handoffPath));
            if (string.Equals(missingCode, "missing-section-screenshot", StringComparison.Ordinal))
            {
                findings.Add(Block("missing-required-section-crop", $"Required section crop is missing: {handoffPath}", handoffPath));
            }

            return;
        }

        var actual = FileHash(path);
        if (!string.Equals(actual, expectedHash, StringComparison.Ordinal))
        {
            findings.Add(Block("evidence-hash-mismatch", $"Evidence hash mismatch for {handoffPath}.", handoffPath));
        }
    }

    private void AddSemanticContractFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        AddPageCompositionSlotFindings(root, findings);
        AddReviewHashFindings(root, findings);
        AddBlueprintReferenceFindings(root, findings);
        AddMappingCatalogFindings(root, findings);
        AddStorefrontPatternFindings(root, findings);
    }

    private void AddPageCompositionSlotFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        foreach (var finding in new PageCompositionSlotValidator(repoRoot).Validate(root).Where(finding => finding.Severity == "blocking"))
        {
            findings.Add(Block(finding.Code, finding.Message, finding.ArtifactPath));
        }
    }

    private static void AddReviewHashFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var queue = Read<ReviewQueue>(root, "review/review-queue.json");
        var decisions = Read<ReviewDecisions>(root, "review/review-decisions.json");
        var manifest = Read<ReviewResolutionManifest>(root, "analysis/resolved/review-resolution-manifest.json");
        if (queue is null || decisions is null || manifest is null)
        {
            return;
        }

        var queueHash = StableHash(queue);
        if (!string.Equals(queueHash, manifest.SourceReviewQueueHash, StringComparison.Ordinal))
        {
            findings.Add(Block("reviewed-artifact-source-hash-mismatch", "Review resolution manifest source queue hash does not match current review queue.", "analysis/resolved/review-resolution-manifest.json"));
        }

        var decisionHash = StableHash(decisions);
        if (!string.Equals(decisionHash, manifest.DecisionBundleHash, StringComparison.Ordinal))
        {
            findings.Add(Block("reviewed-artifact-source-hash-mismatch", "Review resolution manifest decision bundle hash does not match current review decisions.", "analysis/resolved/review-resolution-manifest.json"));
        }

        var queueById = queue.Items.ToDictionary(item => item.ItemId, StringComparer.Ordinal);
        foreach (var decision in decisions.Decisions)
        {
            if (queueById.TryGetValue(decision.ItemId, out var item) &&
                (!string.Equals(decision.SourceArtifactId, item.SourceArtifactId, StringComparison.Ordinal) ||
                 !string.Equals(decision.SourceArtifactHash, item.SourceArtifactHash, StringComparison.Ordinal)))
            {
                findings.Add(Block("decision-source-hash-mismatch", $"Review decision '{decision.ItemId}' is stale for source artifact '{item.SourceArtifactId}'.", "review/review-decisions.json"));
            }
        }
    }

    private static void AddBlueprintReferenceFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var blueprint = Read<VisualBlueprintV1>(root, "analysis/agent-handoff/visual-blueprint.json");
        if (blueprint is null)
        {
            return;
        }

        foreach (var reference in AllBlueprintReferences(blueprint))
        {
            if (reference.Contains(".draft.json", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(Block("reviewed-blueprint-references-draft", "Reviewed handoff blueprint references draft artifacts.", "analysis/agent-handoff/visual-blueprint.json"));
                return;
            }
        }
    }

    private static void AddMappingCatalogFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var mappings = Read<PresentationMappingsDocument>(root, "analysis/resolved/presentation-mappings.reviewed.json");
        var catalog = Read<PresentationComponentCatalog>(root, "presentation-catalog/presentation-component-catalog.json");
        if (mappings is null || catalog is null)
        {
            return;
        }

        var components = catalog.Components.ToDictionary(component => component.ComponentId, StringComparer.Ordinal);
        foreach (var mapping in mappings.Mappings)
        {
            if (!components.TryGetValue(mapping.PresentationComponentId, out var component))
            {
                findings.Add(Block("mapping-target-missing", $"Presentation mapping '{mapping.SourceCandidateId}' targets missing component '{mapping.PresentationComponentId}'.", "analysis/resolved/presentation-mappings.reviewed.json"));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(mapping.StarterSlotId) &&
                !component.Slots.Contains(mapping.StarterSlotId, StringComparer.Ordinal))
            {
                findings.Add(Block("invalid-section-slot-mapping", $"Presentation mapping '{mapping.SourceCandidateId}' maps to slot '{mapping.StarterSlotId}' not exposed by component '{component.ComponentId}'.", "analysis/resolved/presentation-mappings.reviewed.json"));
            }

            if (component.AllowedFilePatterns.Count > 0 &&
                !component.AllowedFilePatterns.Contains(mapping.TargetGeneratedPath, StringComparer.Ordinal))
            {
                findings.Add(Block("slot-target-path-mismatch", $"Presentation mapping '{mapping.SourceCandidateId}' target path is not approved by its component catalog entry.", "analysis/resolved/presentation-mappings.reviewed.json"));
            }
        }
    }

    private static void AddStorefrontPatternFindings(string root, List<AgentHandoffReadinessFinding> findings)
    {
        var pattern = Read<StorefrontPatternContract>(root, "analysis/agent-handoff/storefront-pattern.json");
        if (pattern is null)
        {
            return;
        }

        var slots = pattern.Slots.Select(slot => slot.SlotId).ToHashSet(StringComparer.Ordinal);
        foreach (var page in pattern.PageContracts)
        {
            var contractSlots = page.RequiredSlotIds
                .Concat(page.OptionalSlotIds)
                .Concat(page.RepeatableSlotIds)
                .Concat(page.AllowedAdditionalSlotIds)
                .ToArray();
            foreach (var slot in contractSlots.Where(slot => !slots.Contains(slot)).Distinct(StringComparer.Ordinal))
            {
                findings.Add(Block("unknown-slot", $"Page contract '{page.PageId}' references unknown slot '{slot}'.", "analysis/agent-handoff/storefront-pattern.json"));
            }

            var allowed = page.AllowedVisualSlots.ToHashSet(StringComparer.Ordinal);
            foreach (var slot in page.RequiredSlotIds.Concat(page.OptionalSlotIds).Where(slot => allowed.Count > 0 && !allowed.Contains(slot)).Distinct(StringComparer.Ordinal))
            {
                findings.Add(Block("invalid-section-slot-mapping", $"Page contract '{page.PageId}' uses slot '{slot}' outside its allowed visual slots.", "analysis/agent-handoff/storefront-pattern.json"));
            }
        }
    }

    private static T? Read<T>(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), VisualJson.Options)
            : default;
    }

    private static string StableHash(object value)
    {
        var json = JsonSerializer.Serialize(value, VisualJson.Options);
        return FileHash(System.Text.Encoding.UTF8.GetBytes(json));
    }

    private static string FileHash(string path) =>
        FileHash(File.ReadAllBytes(path));

    private static string FileHash(byte[] bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();

    private static IEnumerable<string> AllBlueprintReferences(VisualBlueprintV1 blueprint)
    {
        foreach (var reference in blueprint.SourceProvenance) yield return reference;
        foreach (var reference in blueprint.PageArchetypes) yield return reference;
        yield return blueprint.Tokens;
        foreach (var reference in blueprint.Sections) yield return reference;
        foreach (var reference in blueprint.ResponsiveBehavior) yield return reference;
        foreach (var reference in blueprint.InteractionModels) yield return reference;
        yield return blueprint.ComponentDefinitions;
        yield return blueprint.ComponentInstances;
        foreach (var reference in blueprint.EcommerceRegions) yield return reference;
        yield return blueprint.PresentationMappings;
        yield return blueprint.UnsupportedPatterns;
        yield return blueprint.OriginalityRestrictions;
        yield return blueprint.Confidence;
        yield return blueprint.ReviewState;
    }

    private void AddStaticBoundaryFindings(List<AgentHandoffReadinessFinding> findings)
    {
        var builderRoot = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontBuilder");
        if (Directory.Exists(builderRoot) &&
            Directory.EnumerateFiles(builderRoot, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                .Any(path => File.ReadAllText(path).Contains("analysis/agent-handoff", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(Block("missing-agent-handoff-artifact", "StorefrontBuilder consumes Phase 3C handoff artifacts before Phase 4 approval.", "tools/BlazorShop.AI.StorefrontBuilder"));
        }

        var reverseRoot = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering");
        if (Directory.Exists(reverseRoot))
        {
            foreach (var path in Directory.EnumerateFiles(reverseRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.EndsWith(nameof(AgentHandoffReadinessValidator) + ".cs", StringComparison.OrdinalIgnoreCase)))
            {
                var text = File.ReadAllText(path);
                if (text.Contains("plan.Pages.First()", StringComparison.Ordinal) || text.Contains("captures/home", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(Block("single-page-hardcode-detected", $"Single-page hardcode detected in {Path.GetRelativePath(repoRoot, path)}.", Path.GetRelativePath(repoRoot, path).Replace(Path.DirectorySeparatorChar, '/')));
                }
            }
        }
    }

    private static string NormalizeBlockingCode(string code) =>
        code switch
        {
            "missing-review-decisions" => "unresolved-critical-region",
            "missing-mapping-for-critical-region" => "unresolved-critical-region",
            "missing-page-evidence" => "missing-required-page",
            _ => code
        };

    private static bool IsProtectedPath(string path) =>
        path.Contains("BlazorShop.Storefront.Presentation", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("BlazorShop.Storefront.Runtime", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("BlazorShop.Storefront.Client", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("BlazorShop.Storefront.V2", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("CommerceNode", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("ControlPlane", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("StorefrontPackageVersions.props", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("starter-generation.contract.yaml", StringComparison.OrdinalIgnoreCase);

    private static AgentHandoffReadinessFinding Block(string code, string message, string? path) => new(code, "blocking", message, path);

    private static async Task WriteAsync(string root, AgentHandoffReadinessReport report, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, "analysis", "agent-handoff", "handoff-readiness.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, VisualJson.Options) + Environment.NewLine, cancellationToken);
    }
}
