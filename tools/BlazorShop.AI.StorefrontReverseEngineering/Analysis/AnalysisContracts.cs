using BlazorShop.AI.StorefrontReverseEngineering.Evidence;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis;

public interface IVisualAnalysisProvider
{
    Task<VisualAnalysisResult> AnalyzeAsync(AnalysisContext context, CancellationToken cancellationToken);
}

public sealed record AnalysisContext(
    string ProjectId,
    string PageId,
    ElementEvidenceIndex ElementEvidence,
    AssetInventoryEvidence? AssetInventory,
    bool AiEnabled = false,
    string? AiProviderName = null);

public sealed record VisualAnalysisResult(
    PageTopologyDraft PageTopology,
    PageSpecificationDraft PageSpecification,
    IReadOnlyList<ComponentSpecificationDraft> ComponentSpecifications,
    VisualBlueprintDraft VisualBlueprint,
    AiInferenceLog? AiInferenceLog);

public sealed record PageTopologyDraft(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    IReadOnlyList<GlobalShellCandidate> GlobalShell,
    IReadOnlyList<SectionCandidate> Sections,
    IReadOnlyList<string> UnsupportedPatternWarnings);

public sealed record SectionCandidate(
    string SectionId,
    string Category,
    decimal Confidence,
    IReadOnlyList<string> EvidenceIds);

public sealed record GlobalShellCandidate(
    string ShellId,
    string Category,
    decimal Confidence,
    IReadOnlyList<string> EvidenceIds);

public sealed record PageSpecificationDraft(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string PageRole,
    decimal Confidence,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> Warnings);

public sealed record ComponentSpecificationDraft(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string CandidateId,
    string NeutralName,
    decimal Confidence,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> Warnings);

public sealed record VisualBlueprintDraft(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    IReadOnlyList<string> PageSpecificationIds,
    IReadOnlyList<string> ComponentSpecificationIds,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> GenerationRestrictions,
    decimal Confidence);

public sealed record AiInferenceLog(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string Provider,
    IReadOnlyList<string> InferenceIds);
