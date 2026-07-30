using BlazorShop.AI.StorefrontReverseEngineering.Contracts;

namespace BlazorShop.AI.StorefrontReverseEngineering.Provenance;

public sealed record OriginalityAuditReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    OriginalityPolicy Policy,
    IReadOnlyList<ReferenceOnlyAsset> ReferenceOnlyAssets,
    IReadOnlyList<ProvenanceWarning> Warnings,
    IReadOnlyList<GenerationRestriction> GenerationRestrictions);

public sealed record ReferenceOnlyAsset(
    string EvidenceId,
    string Url,
    string Reason,
    bool LikelyBrandAsset);

public sealed record ProvenanceWarning(
    string Code,
    string Severity,
    string Message,
    IReadOnlyList<string> EvidenceIds);

public sealed record GenerationRestriction(
    string Code,
    string Scope,
    string Rule,
    IReadOnlyList<string> EvidenceIds);
