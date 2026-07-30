using BlazorShop.AI.StorefrontReverseEngineering.Evidence;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis;

public sealed class RuleBasedVisualAnalysisProvider : IVisualAnalysisProvider
{
    public Task<VisualAnalysisResult> AnalyzeAsync(AnalysisContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var evidenceIds = context.ElementEvidence.Elements.Select(element => element.EvidenceId).ToArray();
        var shell = BuildShell(context.ElementEvidence);
        var sections = BuildSections(context.ElementEvidence);
        var warnings = new List<string>();
        if (context.ElementEvidence.Elements.Count == 0)
        {
            warnings.Add("No element evidence was available for topology inference.");
        }

        if (context.AiEnabled && string.IsNullOrWhiteSpace(context.AiProviderName))
        {
            warnings.Add("AI analysis was requested but no provider was configured; rule-based fallback was used.");
        }

        var topology = new PageTopologyDraft(
            "1.0",
            "page-topology-draft",
            $"page-topology-{context.ProjectId}-{context.PageId}",
            DateTimeOffset.UtcNow,
            context.ProjectId,
            context.PageId,
            shell,
            sections,
            warnings);
        var pageSpec = new PageSpecificationDraft(
            "1.0",
            "page-specification-draft",
            $"page-specification-{context.ProjectId}-{context.PageId}",
            DateTimeOffset.UtcNow,
            context.ProjectId,
            context.PageId,
            "storefront-home",
            evidenceIds.Length == 0 ? 0.1m : 0.72m,
            evidenceIds,
            warnings);
        var components = sections.Select(section => new ComponentSpecificationDraft(
            "1.0",
            "component-specification-draft",
            $"component-specification-{context.ProjectId}-{context.PageId}-{section.SectionId}",
            DateTimeOffset.UtcNow,
            context.ProjectId,
            context.PageId,
            section.SectionId,
            $"{section.Category}-candidate",
            section.Confidence,
            section.EvidenceIds,
            [])).ToArray();
        var blueprint = new VisualBlueprintDraft(
            "1.0",
            "visual-blueprint-draft",
            $"visual-blueprint-{context.ProjectId}-{context.PageId}",
            DateTimeOffset.UtcNow,
            context.ProjectId,
            context.PageId,
            [pageSpec.ArtifactId],
            components.Select(component => component.ArtifactId).ToArray(),
            evidenceIds,
            ["Do not reuse reference assets by default.", "Do not generate Razor/CSS in Phase 3A."],
            components.Length == 0 ? 0.25m : 0.70m);

        AiInferenceLog? log = string.IsNullOrWhiteSpace(context.AiProviderName)
            ? null
            : new AiInferenceLog("1.0", "ai-inference-log", $"ai-inference-{context.ProjectId}-{context.PageId}", DateTimeOffset.UtcNow, context.ProjectId, context.PageId, context.AiProviderName, []);

        return Task.FromResult(new VisualAnalysisResult(topology, pageSpec, components, blueprint, log));
    }

    private static IReadOnlyList<GlobalShellCandidate> BuildShell(ElementEvidenceIndex evidence)
    {
        return evidence.Elements
            .Where(element => element.Category == "semantic-landmark")
            .Take(3)
            .Select(element => new GlobalShellCandidate($"shell-{element.Selector}", element.Selector, 0.68m, [element.EvidenceId]))
            .ToArray();
    }

    private static IReadOnlyList<SectionCandidate> BuildSections(ElementEvidenceIndex evidence)
    {
        return evidence.Elements
            .Where(element => element.Category is "section" or "product-card-candidate" or "heading" or "article")
            .Take(12)
            .Select((element, index) => new SectionCandidate($"section-{index + 1:00}", element.Category, element.Category == "product-card-candidate" ? 0.74m : 0.62m, [element.EvidenceId]))
            .ToArray();
    }
}
