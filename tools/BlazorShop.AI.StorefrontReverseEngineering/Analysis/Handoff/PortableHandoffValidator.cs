using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record PortableHandoffValidationFinding(
    string Code,
    string Severity,
    string Message,
    string? ArtifactPath = null,
    string? Problem = null,
    string? Cause = null,
    string? FixSuggestion = null);

public sealed record PortableHandoffValidationReport(
    string HandoffRoot,
    string? ProjectId,
    bool ReadinessPassed,
    string? PackageHash,
    int ArtifactCount,
    int SchemaCount,
    int ConsumerReferenceCount,
    int DiagnosticProvenanceCount,
    IReadOnlyList<PortableHandoffValidationFinding> Findings);

public sealed class PortableHandoffValidator
{
    private readonly IVisualSchemaRegistry? schemaRegistry;

    public PortableHandoffValidator(IVisualSchemaRegistry? schemaRegistry = null)
    {
        this.schemaRegistry = schemaRegistry;
    }

    public async Task<PortableHandoffValidationReport> ValidateAsync(string handoffRoot, string schemaRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = Path.GetFullPath(handoffRoot);
        var schemaRootPath = Path.GetFullPath(schemaRoot);
        var findings = new List<PortableHandoffValidationFinding>();

        if (!Directory.Exists(root))
        {
            findings.Add(Block("portable-handoff-root-missing", $"Portable handoff root does not exist: {root}", root,
                "Portable handoff root is missing.",
                "The copied package root is not available.",
                "Copy analysis/agent-handoff together with its parent root before validation."));
            return Report(root, null, false, null, 0, 0, 0, 0, findings);
        }

        if (!Directory.Exists(schemaRootPath) || !Directory.EnumerateFiles(schemaRootPath, "*.schema.json").Any())
        {
            findings.Add(Block("portable-handoff-schema-root-missing", $"Portable schema root does not exist or has no schema files: {schemaRootPath}", schemaRootPath,
                "Portable schema root is missing.",
                "The schema directory was not copied or is empty.",
                "Copy the reverse-engineering schema files beside the portable package before validation."));
            return Report(root, null, false, null, 0, 0, 0, 0, findings);
        }

        var registry = schemaRegistry ?? new VisualSchemaRegistry(schemaRootPath);
        var validator = new VisualSchemaValidator(registry);
        var manifestPath = Path.Combine(root, AgentHandoffContract.HandoffRoot, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            findings.Add(Block("portable-handoff-manifest-missing", "Portable handoff manifest is missing.", manifestPath,
                "Portable handoff manifest is missing.",
                "The package copy does not contain analysis/agent-handoff/manifest.json.",
                "Copy the full portable handoff package before validating."));
            return Report(root, null, false, null, 0, registry.Schemas.Count, 0, 0, findings);
        }

        AgentHandoffManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<AgentHandoffManifest>(await File.ReadAllTextAsync(manifestPath, cancellationToken), VisualJson.Options);
        }
        catch (Exception exception)
        {
            findings.Add(Block("portable-handoff-manifest-invalid", $"Portable handoff manifest could not be parsed: {exception.Message}", manifestPath,
                "Portable handoff manifest is invalid.",
                "The manifest JSON is malformed or does not match the portable contract.",
                "Regenerate the handoff manifest."));
            return Report(root, null, false, null, 0, registry.Schemas.Count, 0, 0, findings);
        }

        if (manifest is null)
        {
            findings.Add(Block("portable-handoff-manifest-invalid", "Portable handoff manifest could not be parsed.", manifestPath,
                "Portable handoff manifest is invalid.",
                "The manifest JSON is malformed or empty.",
                "Regenerate the handoff manifest."));
            return Report(root, null, false, null, 0, registry.Schemas.Count, 0, 0, findings);
        }

        try
        {
            validator.Validate("agent-handoff-manifest", JsonNode.Parse(await File.ReadAllTextAsync(manifestPath, cancellationToken))!);
        }
        catch (Exception exception)
        {
            findings.Add(Block("portable-handoff-schema-validation-failed", exception.Message, manifestPath,
                "Portable handoff manifest failed schema validation.",
                "The manifest content does not satisfy the registered schema.",
                "Regenerate the manifest from the current portable contract."));
        }

        var portableArtifactEntries = manifest.ArtifactEntries.Select(ToPortableArtifactEntry).ToArray();
        var packageHash = PortableHandoffPackageHasher.ComputePackageHash(portableArtifactEntries, manifest.SchemaRequirements);
        if (!string.Equals(packageHash, manifest.PackageHash, StringComparison.Ordinal))
        {
            findings.Add(Block("portable-handoff-package-hash-mismatch", "Portable handoff package hash does not match manifest.", manifestPath,
                "Portable handoff package hash mismatch.",
                "A file-level artifact or required schema changed after packaging.",
                "Reassemble the handoff package."));
        }

        AddManifestOrderFindings(manifest, manifestPath, findings);
        AddCanonicalContractFindings(manifest, manifestPath, findings);

        foreach (var entry in manifest.ArtifactEntries.OrderBy(entry => entry.Path, StringComparer.Ordinal))
        {
            var entryPath = Path.Combine(root, entry.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(entryPath) && !Directory.Exists(entryPath))
            {
                if (entry.Required)
                {
                    findings.Add(Block("portable-handoff-artifact-missing", $"Portable artifact is missing: {entry.Path}", entry.Path,
                        "Required portable artifact is missing.",
                        "The copied package is incomplete.",
                        "Copy the full analysis/agent-handoff package."));
                }

                continue;
            }

            if (entry.IncludeInPackageHash && File.Exists(entryPath))
            {
                var actualHash = PortableHandoffPackageHasher.ComputeFileHash(entryPath);
                if (!string.Equals(actualHash, entry.Sha256, StringComparison.Ordinal))
                {
                    findings.Add(Block("portable-handoff-artifact-hash-mismatch", $"Portable artifact hash mismatch: {entry.Path}", entry.Path,
                        "Portable artifact hash mismatch.",
                        "A required file changed after packaging.",
                        "Reassemble the portable package."));
                }
            }

            if (File.Exists(entryPath) &&
                string.Equals(Path.GetExtension(entryPath), ".json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var node = JsonNode.Parse(await File.ReadAllTextAsync(entryPath, cancellationToken));
                    if (node is not null)
                    {
                        validator.Validate(entry.ArtifactKind, node);
                    }
                }
                catch (Exception exception)
                {
                    findings.Add(Block("portable-handoff-artifact-invalid", exception.Message, entry.Path,
                        "Portable artifact failed schema validation.",
                        "The artifact JSON does not satisfy the registered schema.",
                        "Regenerate the portable package."));
                }
            }
        }

        foreach (var schema in manifest.SchemaRequirements.Where(schema => schema.Required))
        {
            var schemaPath = Path.Combine(schemaRootPath, schema.SchemaFileName);
            if (!File.Exists(schemaPath))
            {
                findings.Add(Block("portable-handoff-schema-missing", $"Portable schema file is missing: {schema.SchemaFileName}", schema.SchemaFileName,
                    "Portable schema file is missing.",
                    "The schema root does not contain the required contract.",
                    "Copy the exact schema files used to produce the portable package."));
                continue;
            }

            var actualHash = PortableHandoffPackageHasher.ComputeFileHash(schemaPath);
            if (!string.Equals(actualHash, schema.Sha256, StringComparison.Ordinal))
            {
                findings.Add(Block("portable-handoff-schema-hash-mismatch", $"Portable schema hash mismatch: {schema.SchemaFileName}", schema.SchemaFileName,
                    "Portable schema hash mismatch.",
                    "The schema root does not match the packaged contract set.",
                    "Use the schema set that matches the packaged manifest."));
            }
        }

        var scanner = new HandoffReferenceScanner();
        var (observations, scanFindings) = scanner.Scan(root);
        findings.AddRange(scanFindings.Select(ConvertFinding));

        var readinessPath = Path.Combine(root, AgentHandoffContract.HandoffRoot, "handoff-readiness.json");
        AgentHandoffReadinessReport? readiness = null;
        if (File.Exists(readinessPath))
        {
            try
            {
                readiness = JsonSerializer.Deserialize<AgentHandoffReadinessReport>(await File.ReadAllTextAsync(readinessPath, cancellationToken), VisualJson.Options);
            }
            catch (Exception exception)
            {
                findings.Add(Block("portable-handoff-readiness-invalid", exception.Message, readinessPath,
                    "Portable handoff readiness report is invalid.",
                    "The readiness report cannot be parsed.",
                    "Regenerate the handoff readiness report."));
            }
        }
        else
        {
            findings.Add(Block("portable-handoff-readiness-missing", "Portable handoff readiness report is missing.", readinessPath,
                "Portable handoff readiness report is missing.",
                "The portable package is incomplete.",
                "Copy analysis/agent-handoff/handoff-readiness.json into the portable root."));
        }

        if (readiness is not null && !readiness.Passed)
        {
            findings.Add(Block("portable-handoff-readiness-false", "Portable handoff readiness failed.", readinessPath,
                "Portable handoff readiness is false.",
                "The package still has blocking readiness findings.",
                "Fix the blocking findings before trying to hand off the package."));
        }

        if (readiness is not null && readiness.Passed != manifest.ReadinessPassed)
        {
            findings.Add(Block("portable-handoff-readiness-mismatch", "Portable manifest readiness and handoff readiness report do not agree.", readinessPath,
                "Portable readiness mismatch.",
                "The manifest readiness flag and analysis/agent-handoff/handoff-readiness.json passed flag differ.",
                "Revalidate and reassemble the handoff package so manifest readiness matches the packaged readiness report."));
        }

        return Report(
            root,
            manifest.ProjectId,
            readiness?.Passed ?? false,
            manifest.PackageHash,
            manifest.ArtifactEntries.Count,
            manifest.SchemaRequirements.Count,
            observations.Count(observation => observation.Category == PortableHandoffReferenceCategories.ConsumerDependency),
            observations.Count(observation => observation.Category == PortableHandoffReferenceCategories.DiagnosticProvenance),
            findings);
    }

    private static PortableHandoffArtifactEntry ToPortableArtifactEntry(AgentHandoffArtifactEntry entry) =>
        new(
            entry.Path,
            entry.ArtifactKind,
            entry.SchemaKind,
            entry.SchemaVersion,
            entry.Sha256,
            entry.SizeBytes,
            entry.Required,
            entry.IncludeInPackageHash);

    private static PortableHandoffValidationReport Report(
        string handoffRoot,
        string? projectId,
        bool readinessPassed,
        string? packageHash,
        int artifactCount,
        int schemaCount,
        int consumerReferenceCount,
        int diagnosticProvenanceCount,
        IReadOnlyList<PortableHandoffValidationFinding> findings) =>
        new(handoffRoot, projectId, readinessPassed, packageHash, artifactCount, schemaCount, consumerReferenceCount, diagnosticProvenanceCount, findings);

    private static PortableHandoffValidationFinding Block(
        string code,
        string message,
        string? artifactPath = null,
        string? problem = null,
        string? cause = null,
        string? fixSuggestion = null) =>
        new(code, "blocking", message, artifactPath, problem, cause, fixSuggestion);

    private static void AddManifestOrderFindings(
        AgentHandoffManifest manifest,
        string manifestPath,
        List<PortableHandoffValidationFinding> findings)
    {
        var artifactPaths = manifest.ArtifactEntries.Select(entry => entry.Path).ToArray();
        var canonicalArtifactPaths = artifactPaths.Order(StringComparer.Ordinal).ToArray();
        var schemaKinds = manifest.SchemaRequirements.Select(schema => schema.SchemaKind).ToArray();
        var canonicalSchemaKinds = schemaKinds.Order(StringComparer.Ordinal).ToArray();

        if (!artifactPaths.SequenceEqual(canonicalArtifactPaths, StringComparer.Ordinal) ||
            !schemaKinds.SequenceEqual(canonicalSchemaKinds, StringComparer.Ordinal))
        {
            findings.Add(Block(
                "portable-handoff-manifest-order-mismatch",
                "Portable handoff manifest order does not match the canonical portable package order.",
                manifestPath,
                "Portable handoff manifest order mismatch.",
                "The manifest lists are not written in the canonical sorted order expected by the portable contract.",
                "Regenerate the manifest without reordering artifact entries, artifact lists, or schema requirements."));
        }
    }

    private static void AddCanonicalContractFindings(
        AgentHandoffManifest manifest,
        string manifestPath,
        List<PortableHandoffValidationFinding> findings)
    {
        if (!string.Equals(manifest.PackageVersion, AgentHandoffContract.PackageVersion, StringComparison.Ordinal))
        {
            findings.Add(Block(
                "portable-handoff-package-version-mismatch",
                "Portable handoff packageVersion does not match the canonical contract.",
                manifestPath,
                "Portable package version mismatch.",
                "The manifest was produced by a different portable handoff contract.",
                "Reassemble the handoff package with the current ReverseEngineering tool."));
        }

        if (!string.Equals(manifest.HandoffRoot, AgentHandoffContract.HandoffRoot, StringComparison.Ordinal))
        {
            findings.Add(Block(
                "portable-handoff-root-mismatch",
                "Portable handoff root does not match the canonical contract.",
                manifestPath,
                "Portable handoff root mismatch.",
                "The manifest points at a non-canonical handoff root.",
                "Reassemble the package so handoffRoot is analysis/agent-handoff."));
        }

        AddCanonicalArtifactFindings(manifest, manifestPath, findings);
        AddCanonicalSchemaFindings(manifest, manifestPath, findings);
        AddCanonicalReferencePolicyFindings(manifest, manifestPath, findings);
    }

    private static void AddCanonicalArtifactFindings(
        AgentHandoffManifest manifest,
        string manifestPath,
        List<PortableHandoffValidationFinding> findings)
    {
        var requiredByPath = AgentHandoffContract.RequiredArtifacts.ToDictionary(artifact => artifact.RelativePath, StringComparer.Ordinal);
        var artifactList = manifest.ArtifactList.ToHashSet(StringComparer.Ordinal);
        var entryGroups = manifest.ArtifactEntries.GroupBy(entry => entry.Path, StringComparer.Ordinal).ToArray();
        var entriesByPath = entryGroups.ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var duplicate in entryGroups.Where(group => group.Count() > 1))
        {
            findings.Add(Block(
                "portable-handoff-canonical-artifact-duplicate",
                $"Portable manifest declares duplicate artifact entries: {duplicate.Key}",
                manifestPath,
                "Portable manifest has duplicate artifact entries.",
                "The same artifact path is listed more than once.",
                "Regenerate the manifest from the canonical handoff contract."));
        }

        foreach (var required in AgentHandoffContract.RequiredArtifacts)
        {
            if (!artifactList.Contains(required.RelativePath))
            {
                findings.Add(Block(
                    "portable-handoff-canonical-artifact-missing",
                    $"Portable manifest artifactList is missing canonical artifact: {required.RelativePath}",
                    manifestPath,
                    "Portable manifest is missing a canonical artifact.",
                    "The copied package contract is incomplete.",
                    "Copy the full analysis/agent-handoff package and regenerate the manifest."));
            }

            if (!entriesByPath.TryGetValue(required.RelativePath, out var entries) || entries.Length == 0)
            {
                findings.Add(Block(
                    "portable-handoff-canonical-artifact-missing",
                    $"Portable manifest artifactEntries is missing canonical artifact: {required.RelativePath}",
                    manifestPath,
                    "Portable manifest is missing a canonical artifact entry.",
                    "The validator cannot prove this required artifact from the copied package contract.",
                    "Copy the full analysis/agent-handoff package and regenerate the manifest."));
                continue;
            }

            var entry = entries[0];
            var expectedIncludeInPackageHash = required.HashRequired &&
                required.RelativePath is not ("analysis/agent-handoff/manifest.json" or "analysis/agent-handoff/handoff-readiness.json");
            if (!string.Equals(entry.ArtifactKind, required.ArtifactKind, StringComparison.Ordinal) ||
                !string.Equals(entry.SchemaKind, required.SchemaName, StringComparison.Ordinal) ||
                entry.Required != true ||
                entry.IsDirectory != required.IsDirectory ||
                entry.IncludeInPackageHash != expectedIncludeInPackageHash)
            {
                findings.Add(Block(
                    "portable-handoff-canonical-artifact-mismatch",
                    $"Portable manifest canonical artifact metadata does not match contract: {required.RelativePath}",
                    manifestPath,
                    "Portable manifest artifact metadata mismatch.",
                    "The artifact entry no longer matches the canonical required artifact contract.",
                    "Regenerate the manifest from the current handoff contract."));
            }
        }

        foreach (var path in artifactList.Where(path => !requiredByPath.ContainsKey(path)))
        {
            findings.Add(Block(
                "portable-handoff-canonical-artifact-extra",
                $"Portable manifest artifactList contains a non-canonical artifact: {path}",
                manifestPath,
                "Portable manifest contains a non-canonical artifact.",
                "artifactList must contain only the required portable contract roots.",
                "Remove the extra artifact from artifactList and regenerate the manifest."));
        }

        foreach (var entry in manifest.ArtifactEntries.Where(entry => !requiredByPath.ContainsKey(entry.Path) && !IsEvidenceFileEntry(entry)))
        {
            findings.Add(Block(
                "portable-handoff-canonical-artifact-extra",
                $"Portable manifest artifactEntries contains a non-canonical artifact entry: {entry.Path}",
                manifestPath,
                "Portable manifest contains a non-canonical artifact entry.",
                "Only canonical artifacts and evidence files under the packaged evidence directories are allowed.",
                "Remove the extra artifact entry and regenerate the manifest."));
        }
    }

    private static bool IsEvidenceFileEntry(AgentHandoffArtifactEntry entry) =>
        string.Equals(entry.ArtifactKind, "agent-handoff-evidence-file", StringComparison.Ordinal) &&
        !entry.IsDirectory &&
        entry.Required &&
        entry.IncludeInPackageHash &&
        (entry.Path.StartsWith("analysis/agent-handoff/screenshots/", StringComparison.Ordinal) ||
            entry.Path.StartsWith("analysis/agent-handoff/section-screenshots/", StringComparison.Ordinal));

    private static void AddCanonicalSchemaFindings(
        AgentHandoffManifest manifest,
        string manifestPath,
        List<PortableHandoffValidationFinding> findings)
    {
        var requiredByKind = AgentHandoffContract.RequiredSchemaKinds.ToDictionary(schema => schema.SchemaKind, StringComparer.Ordinal);
        var groups = manifest.SchemaRequirements.GroupBy(schema => schema.SchemaKind, StringComparer.Ordinal).ToArray();
        var byKind = groups.ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var duplicate in groups.Where(group => group.Count() > 1))
        {
            findings.Add(Block(
                "portable-handoff-canonical-schema-duplicate",
                $"Portable manifest declares duplicate schema requirements: {duplicate.Key}",
                manifestPath,
                "Portable manifest has duplicate schema requirements.",
                "The same schema kind is listed more than once.",
                "Regenerate the manifest from the canonical handoff contract."));
        }

        foreach (var required in AgentHandoffContract.RequiredSchemaKinds)
        {
            if (!byKind.TryGetValue(required.SchemaKind, out var schemas) || schemas.Length == 0)
            {
                findings.Add(Block(
                    "portable-handoff-canonical-schema-missing",
                    $"Portable manifest is missing canonical schema requirement: {required.SchemaKind}",
                    manifestPath,
                    "Portable manifest is missing a canonical schema.",
                    "The copied package contract does not include every required schema kind.",
                    "Copy the exact schema set and regenerate the manifest."));
                continue;
            }

            var schema = schemas[0];
            if (!string.Equals(schema.ArtifactKind, required.ArtifactKind, StringComparison.Ordinal) ||
                !string.Equals(schema.SchemaFileName, required.SchemaFileName, StringComparison.Ordinal) ||
                !string.Equals(schema.SchemaVersion, required.SchemaVersion, StringComparison.Ordinal) ||
                schema.Required != required.Required)
            {
                findings.Add(Block(
                    "portable-handoff-canonical-schema-mismatch",
                    $"Portable manifest schema metadata does not match contract: {required.SchemaKind}",
                    manifestPath,
                    "Portable manifest schema metadata mismatch.",
                    "A required schema entry no longer matches the canonical schema contract.",
                    "Regenerate the manifest from the current handoff contract."));
            }
        }

        foreach (var schema in manifest.SchemaRequirements.Where(schema => !requiredByKind.ContainsKey(schema.SchemaKind)))
        {
            findings.Add(Block(
                "portable-handoff-canonical-schema-extra",
                $"Portable manifest contains a non-canonical schema requirement: {schema.SchemaKind}",
                manifestPath,
                "Portable manifest contains a non-canonical schema.",
                "The portable validator accepts only the registered handoff schema set.",
                "Remove the extra schema requirement and regenerate the manifest."));
        }
    }

    private static void AddCanonicalReferencePolicyFindings(
        AgentHandoffManifest manifest,
        string manifestPath,
        List<PortableHandoffValidationFinding> findings)
    {
        if (!string.Equals(manifest.ConsumerReferencePolicy.HandoffRoot, AgentHandoffContract.HandoffRoot, StringComparison.Ordinal) ||
            !manifest.ConsumerReferencePolicy.RejectAbsoluteConsumerPaths ||
            !manifest.ConsumerReferencePolicy.RejectConsumerPathEscape ||
            !manifest.ConsumerReferencePolicy.RejectDraftConsumerReferences)
        {
            findings.Add(Block(
                "portable-handoff-reference-policy-mismatch",
                "Portable manifest reference policy does not match the canonical handoff policy.",
                manifestPath,
                "Portable reference policy mismatch.",
                "The manifest no longer enforces the required consumer reference guardrails.",
                "Regenerate the manifest from the current handoff contract."));
        }

        var requiredCategories = PortableHandoffReferenceCategories.All.ToDictionary(category => category.Category, StringComparer.Ordinal);
        var actualCategoryGroups = manifest.ConsumerReferencePolicy.Categories.GroupBy(category => category.Category, StringComparer.Ordinal).ToArray();
        var actualCategories = actualCategoryGroups.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var duplicate in actualCategoryGroups.Where(group => group.Count() > 1))
        {
            findings.Add(Block(
                "portable-handoff-reference-policy-mismatch",
                $"Portable manifest reference policy declares duplicate category: {duplicate.Key}",
                manifestPath,
                "Portable reference category mismatch.",
                "The same reference category is listed more than once.",
                "Regenerate the manifest from the current handoff contract."));
        }

        foreach (var required in PortableHandoffReferenceCategories.All)
        {
            if (!actualCategories.TryGetValue(required.Category, out var actual) ||
                actual.RequiredFileDependency != required.RequiredFileDependency ||
                actual.MustStayInsideHandoffRoot != required.MustStayInsideHandoffRoot)
            {
                findings.Add(Block(
                    "portable-handoff-reference-policy-mismatch",
                    $"Portable manifest reference policy is missing or changes category: {required.Category}",
                    manifestPath,
                    "Portable reference category mismatch.",
                    "Consumer dependency, diagnostic provenance, generated target path, and external URL references must remain separate categories.",
                    "Regenerate the manifest from the current handoff contract."));
            }
        }

        foreach (var actual in manifest.ConsumerReferencePolicy.Categories.Where(category => !requiredCategories.ContainsKey(category.Category)))
        {
            findings.Add(Block(
                "portable-handoff-reference-policy-mismatch",
                $"Portable manifest reference policy contains a non-canonical category: {actual.Category}",
                manifestPath,
                "Portable reference category mismatch.",
                "The portable manifest contains an unknown reference category.",
                "Remove the unknown category and regenerate the manifest."));
        }
    }

    private static PortableHandoffValidationFinding ConvertFinding(HandoffReferenceScanFinding finding) =>
        finding.Code switch
        {
            "handoff-consumer-reference-absolute" => Block(finding.Code, finding.Message, finding.ArtifactPath, "Consumer reference is absolute.", "The portable package contains a file path rooted outside the copied package.", "Rewrite the reference to a handoff-local relative path."),
            "handoff-consumer-reference-draft" => Block(finding.Code, finding.Message, finding.ArtifactPath, "Consumer reference points at a draft artifact.", "A .draft.json file was used as a consumer dependency.", "Reference the reviewed portable artifact instead."),
            "handoff-consumer-reference-escape" => Block(finding.Code, finding.Message, finding.ArtifactPath, "Consumer reference escapes the handoff package.", "A consumer field points outside analysis/agent-handoff.", "Rewrite the reference to stay inside the portable package."),
            "handoff-diagnostic-reference-used-as-consumer" => Block(finding.Code, finding.Message, finding.ArtifactPath, "Diagnostic path used as consumer dependency.", "A diagnostics-only path was treated as a required package dependency.", "Move the path back to a diagnostics field."),
            "handoff-consumer-reference-missing" => Block(finding.Code, finding.Message, finding.ArtifactPath, "Consumer reference target is missing.", "A required file dependency does not exist in the copied package.", "Copy the missing portable artifact."),
            "handoff-reference-category-mismatch" => Block(finding.Code, finding.Message, finding.ArtifactPath, "Reference category mismatch.", "A URL or path appeared in the wrong reference category.", "Move the value to the correct field."),
            "handoff-artifact-reference-cycle" => Block(finding.Code, finding.Message, finding.ArtifactPath, "Consumer artifact cycle.", "Package references form a cycle.", "Break the cycle so the package remains portable."),
            _ => Block(finding.Code, finding.Message, finding.ArtifactPath)
        };
}
