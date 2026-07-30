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

        foreach (var path in RequiredArtifacts())
        {
            if (!File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))))
            {
                findings.Add(Block("missing-agent-handoff-artifact", $"Required handoff artifact is missing: {path}", path));
            }
        }

        AddGenerationReadinessFindings(root, findings);
        AddAllowedProtectedFindings(root, findings);
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

    private static IReadOnlyList<string> RequiredArtifacts() =>
    [
        "analysis/evidence-snapshot.json",
        "reports/readiness-report.json",
        "analysis/tokens/semantic-tokens.draft.json",
        "presentation-catalog/presentation-component-catalog.json",
        "analysis/mapping/presentation-mappings.draft.json",
        "review/review-queue.json",
        "analysis/visual-blueprint.v1.draft.json",
        "analysis/storefront-pattern/storefront-pattern.json",
        "analysis/storefront-pattern/page-contracts.json",
        "analysis/resolved/page-compositions.reviewed.json",
        "analysis/resolved/presentation-mappings.reviewed.json",
        "analysis/agent-handoff/manifest.json",
        "analysis/agent-handoff/allowed-files.json",
        "analysis/agent-handoff/protected-files.json",
        "analysis/agent-handoff/unresolved-regions.json"
    ];

    private static async Task WriteAsync(string root, AgentHandoffReadinessReport report, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, "analysis", "agent-handoff", "handoff-readiness.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, VisualJson.Options) + Environment.NewLine, cancellationToken);
    }
}
