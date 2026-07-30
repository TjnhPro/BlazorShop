namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;

public sealed record PresentationMappingsDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<PresentationMapping> Mappings);

public sealed record PresentationMapping(
    string SourceCandidateId,
    string PresentationComponentId,
    string? StarterSlotId,
    string Variant,
    IReadOnlyList<string> SlotAssignments,
    IReadOnlyList<string> ResponsiveProperties,
    IReadOnlyList<string> TokenBindings,
    IReadOnlyList<string> InteractionBindings,
    IReadOnlyList<string> DataRequirements,
    string BehaviorOwnership,
    decimal Confidence,
    IReadOnlyList<string> EvidenceIds,
    string MappingReason,
    IReadOnlyList<string> AlternativeMappings,
    bool HumanReviewRequired);

public sealed record UnsupportedPatternsDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<UnsupportedPattern> Patterns);

public sealed record UnsupportedPattern(
    string SourceCandidateId,
    string Group,
    string Reason,
    IReadOnlyList<string> EvidenceIds,
    bool HumanReviewRequired);
