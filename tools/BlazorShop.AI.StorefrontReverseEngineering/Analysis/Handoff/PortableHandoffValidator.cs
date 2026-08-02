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
