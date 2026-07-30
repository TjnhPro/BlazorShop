using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;

public sealed class BlueprintV1Assembler
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public BlueprintV1Assembler(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<(VisualBlueprintV1 Draft, VisualBlueprintV1 Reviewed, GenerationReadinessReport Readiness)> AssembleAsync(
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var reviewed = await new ReviewDecisionApplier(repoRoot)
            .ApplyAsync(root, cancellationToken);
        var draft = Build(project, root, reviewedPath: "review/review-queue.json", reviewed: false);
        var reviewedBlueprint = Build(project, root, reviewedPath: "review/reviewed-items.json", reviewed: true);
        var readiness = Validate(project.ProjectId, root, reviewed);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/visual-blueprint.v1.draft.json"), "visual-blueprint-v1", draft, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/visual-blueprint.v1.reviewed.json"), "visual-blueprint-v1", reviewedBlueprint, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("reports/generation-readiness.json"), "generation-readiness", readiness, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "reports", "generation-readiness.md"), WriteMarkdown(readiness), cancellationToken);
        return (draft, reviewedBlueprint, readiness);
    }

    private static VisualBlueprintV1 Build(VisualProject project, string root, string reviewedPath, bool reviewed)
    {
        var pageRoot = Path.Combine(root, "analysis", "pages");
        IReadOnlyList<string> Files(string pattern) =>
            Directory.Exists(pageRoot)
                ? Directory.EnumerateFiles(pageRoot, pattern, SearchOption.AllDirectories).Select(path => Rel(root, path)).Order(StringComparer.Ordinal).ToArray()
                : [];
        return new VisualBlueprintV1(
            "1.0",
            "visual-blueprint-v1",
            reviewed ? $"visual-blueprint-v1-reviewed-{project.ProjectId}" : $"visual-blueprint-v1-draft-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            new Dictionary<string, string> { ["name"] = project.Name, ["referenceUrl"] = project.ReferenceUrl },
            ["analysis/evidence-snapshot.json", "presentation-catalog/presentation-component-catalog.json"],
            Files("page-archetype.json").Select(path => path.Split('/')[2]).Distinct(StringComparer.Ordinal).ToArray(),
            Files("page-archetype.json"),
            "analysis/tokens/semantic-tokens.draft.json",
            Files("sections.draft.json"),
            Files("responsive-behavior.json"),
            Files("interaction-model.json"),
            "analysis/components/component-candidates.json",
            "analysis/components/component-instances.json",
            Files("ecommerce-regions.json"),
            "analysis/mapping/presentation-mappings.draft.json",
            "analysis/mapping/unsupported-patterns.json",
            "analysis/originality-audit.json",
            "analysis/confidence/confidence-report.json",
            reviewedPath,
            ["Do not reuse reference assets by default.", "Do not generate unsupported runtime behavior."]);
    }

    private GenerationReadinessReport Validate(string projectId, string root, ReviewedItems reviewed)
    {
        var findings = new List<GenerationReadinessFinding>();
        foreach (var path in RequiredArtifacts())
        {
            if (!File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))))
            {
                findings.Add(new GenerationReadinessFinding("missing-required-artifact", "blocking", $"Required artifact is missing: {path}", path));
            }
        }

        if (reviewed.BlocksReadiness)
        {
            findings.Add(new GenerationReadinessFinding("missing-review-decisions", "blocking", "Review queue contains rejected or deferred blocking items.", "review/reviewed-items.json"));
        }

        var unsupportedPath = Path.Combine(root, "analysis", "mapping", "unsupported-patterns.json");
        if (File.Exists(unsupportedPath) && File.ReadAllText(unsupportedPath).Contains("\"humanReviewRequired\": true", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new GenerationReadinessFinding("missing-mapping-for-critical-region", "blocking", "Unsupported critical pattern requires review before generation.", "analysis/mapping/unsupported-patterns.json"));
        }

        var pagesRoot = Path.Combine(root, "analysis", "pages");
        if (Directory.Exists(pagesRoot) &&
            Directory.EnumerateFiles(pagesRoot, "sections.draft.json", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Any(text => text.Contains("invalid-peer-overlap", StringComparison.Ordinal)))
        {
            findings.Add(new GenerationReadinessFinding("invalid-section-segmentation", "blocking", "Section segmentation has blocking overlap findings."));
        }

        return new GenerationReadinessReport("1.0", "generation-readiness", $"generation-readiness-{projectId}", DateTimeOffset.UtcNow, projectId, findings.All(finding => finding.Severity != "blocking"), findings);
    }

    private static IReadOnlyList<string> RequiredArtifacts() =>
    [
        "analysis/tokens/semantic-tokens.draft.json",
        "analysis/components/component-candidates.json",
        "analysis/mapping/presentation-mappings.draft.json",
        "analysis/confidence/confidence-report.json",
        "presentation-catalog/presentation-component-catalog.json"
    ];

    private static string WriteMarkdown(GenerationReadinessReport report) =>
        "# Generation Readiness" + Environment.NewLine + Environment.NewLine +
        $"Passed: `{report.Passed}`" + Environment.NewLine + Environment.NewLine +
        string.Join(Environment.NewLine, report.Findings.Select(finding => $"- `{finding.Code}` ({finding.Severity}): {finding.Message}")) + Environment.NewLine;

    private static string Rel(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
}
