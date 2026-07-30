using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
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

    private static void AddContractArtifactFindings(string root, List<AgentHandoffReadinessFinding> findings)
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
                AddJsonArtifactFindings(root, artifact, path, findings);
            }

            var entry = manifest?.ArtifactEntries.FirstOrDefault(candidate => string.Equals(candidate.Path, artifact.RelativePath, StringComparison.Ordinal));
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
        }
    }

    private static void AddJsonArtifactFindings(
        string root,
        RequiredHandoffArtifact artifact,
        string path,
        List<AgentHandoffReadinessFinding> findings)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
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
            return;
        }

        var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        if (!string.Equals(actual, expectedHash, StringComparison.Ordinal))
        {
            findings.Add(Block("evidence-hash-mismatch", $"Evidence hash mismatch for {handoffPath}.", handoffPath));
        }
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
