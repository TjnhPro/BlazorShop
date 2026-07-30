using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Provenance;

public sealed class OriginalityAuditService
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public OriginalityAuditService(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<OriginalityAuditReport> WriteAuditAsync(
        string projectRoot,
        string projectId,
        string pageId,
        AssetInventoryEvidence assetInventory,
        ElementEvidenceIndex elementEvidence,
        OriginalityPolicy policy,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var referenceOnlyAssets = assetInventory.Assets
            .Select(asset => new ReferenceOnlyAsset(
                asset.EvidenceId,
                asset.Url,
                policy.TreatExternalAssetsAsReferenceOnly ? "External/source asset defaults to reference-only." : "Asset reuse was not explicitly allowed.",
                IsLikelyBrandAsset(asset.Url)))
            .ToArray();

        var warnings = new List<ProvenanceWarning>();
        var brandAssets = referenceOnlyAssets.Where(asset => asset.LikelyBrandAsset).ToArray();
        if (brandAssets.Length > 0)
        {
            warnings.Add(new("likely-brand-asset", "review", "Likely logo/brand asset detected; do not reuse by default.", brandAssets.Select(asset => asset.EvidenceId).ToArray()));
        }

        var sourceCopy = elementEvidence.Elements
            .Where(element => !string.IsNullOrWhiteSpace(element.TextSnippet) && element.TextSnippet.Length > 20)
            .Select(element => element.EvidenceId)
            .ToArray();
        if (sourceCopy.Length > 0)
        {
            warnings.Add(new("source-copy-review", "review", "Source text block should be rewritten or reviewed before generation.", sourceCopy));
        }

        warnings.Add(new("common-visual-grammar", "info", "Common layout grammar such as headers, sections, grids, and cards may inform neutral structure but not distinctive source expression.", elementEvidence.Elements.Select(element => element.EvidenceId).Take(8).ToArray()));

        var restrictions = new List<GenerationRestriction>
        {
            new("reference-assets-not-reusable", "assets", "Treat all source assets as reference-only unless a human explicitly approves reuse.", referenceOnlyAssets.Select(asset => asset.EvidenceId).ToArray()),
            new("rewrite-source-copy", "copy", "Do not copy source text blocks directly into generated storefront output.", sourceCopy),
            new("avoid-distinctive-brand-expression", "visual", "Use evidence to understand common grammar, not to reproduce distinctive logos, marks, or brand-specific composition.", brandAssets.Select(asset => asset.EvidenceId).ToArray())
        };

        var report = new OriginalityAuditReport(
            "1.0",
            "originality-audit",
            $"originality-audit-{projectId}-{pageId}",
            DateTimeOffset.UtcNow,
            projectId,
            pageId,
            policy,
            referenceOnlyAssets,
            warnings,
            restrictions);

        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/originality-audit.json"), "originality-audit", report, cancellationToken);
        Directory.CreateDirectory(Path.Combine(root, "reports"));
        await File.WriteAllTextAsync(Path.Combine(root, "reports", "originality-audit.md"), WriteMarkdown(report), cancellationToken);
        return report;
    }

    private static bool IsLikelyBrandAsset(string url) =>
        url.Contains("logo", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("brand", StringComparison.OrdinalIgnoreCase);

    private static string WriteMarkdown(OriginalityAuditReport report)
    {
        var lines = new List<string>
        {
            "# Originality Audit",
            "",
            $"Project: `{report.ProjectId}`",
            $"Page: `{report.PageId}`",
            "",
            "This report is guidance. The machine-readable source is `analysis/originality-audit.json`.",
            "",
            "## Restrictions"
        };

        lines.AddRange(report.GenerationRestrictions.Select(restriction => $"- `{restriction.Code}`: {restriction.Rule}"));
        lines.Add("");
        lines.Add("## Warnings");
        lines.AddRange(report.Warnings.Select(warning => $"- `{warning.Code}` ({warning.Severity}): {warning.Message}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
