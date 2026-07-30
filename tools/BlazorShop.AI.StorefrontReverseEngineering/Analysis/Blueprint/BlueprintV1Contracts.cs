namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;

public sealed record VisualBlueprintV1(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyDictionary<string, string> ProjectMetadata,
    IReadOnlyList<string> SourceProvenance,
    IReadOnlyList<string> Pages,
    IReadOnlyList<string> PageArchetypes,
    string Tokens,
    IReadOnlyList<string> Sections,
    IReadOnlyList<string> ResponsiveBehavior,
    IReadOnlyList<string> InteractionModels,
    string ComponentDefinitions,
    string ComponentInstances,
    IReadOnlyList<string> EcommerceRegions,
    string PresentationMappings,
    string UnsupportedPatterns,
    string OriginalityRestrictions,
    string Confidence,
    string ReviewState,
    IReadOnlyList<string> GenerationRestrictions);

public sealed record GenerationReadinessReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    bool Passed,
    IReadOnlyList<GenerationReadinessFinding> Findings);

public sealed record GenerationReadinessFinding(
    string Code,
    string Severity,
    string Message,
    string? ArtifactPath = null);
