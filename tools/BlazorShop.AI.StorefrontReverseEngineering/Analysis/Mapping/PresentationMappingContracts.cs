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
    bool HumanReviewRequired,
    string SourcePageId,
    string SourceSectionId,
    string EcommerceRegionId,
    string PageArchetype,
    string TargetGeneratedPath,
    string GeneratedZone,
    string RouteOwnership,
    IReadOnlyList<string> ReasonCodes,
    string ReviewState);

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

public static class PresentationMappingReviewFilter
{
    public static IReadOnlyList<PresentationMapping> ForAgentHandoff(IEnumerable<PresentationMapping> mappings) =>
        mappings
            .Where(mapping => !string.Equals(mapping.ReviewState, "Rejected", StringComparison.Ordinal))
            .OrderBy(mapping => mapping.SourcePageId, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.SourceSectionId, StringComparer.Ordinal)
            .ThenBy(mapping => mapping.SourceCandidateId, StringComparer.Ordinal)
            .ToArray();
}
