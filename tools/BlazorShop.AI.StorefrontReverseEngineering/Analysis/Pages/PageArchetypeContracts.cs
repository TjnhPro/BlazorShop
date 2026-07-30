namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;

public sealed record PageArchetypeDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string PrimaryArchetype,
    decimal Confidence,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<PageArchetypeCandidate> Alternatives);

public sealed record PageArchetypeCandidate(
    string Archetype,
    decimal Confidence,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> ReasonCodes);
