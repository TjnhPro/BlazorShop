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

public sealed record ReviewedPageCompositionsDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    SiteBlueprint Site,
    IReadOnlyList<PageBlueprint> Pages);

public sealed record SiteBlueprint(
    string SiteId,
    IReadOnlyList<string> SourceUrls,
    string StoreArchetypeSummary,
    IReadOnlyDictionary<string, string> SharedVisualLanguage,
    IReadOnlyList<string> SharedLayoutSystem,
    IReadOnlyList<string> SharedResponsiveRules,
    IReadOnlyList<string> PageIds,
    IReadOnlyList<string> UnresolvedSiteLevelIssues);

public sealed record PageBlueprint(
    string PageId,
    string Archetype,
    string SourceUrl,
    IReadOnlyList<string> CaptureArtifactPaths,
    IReadOnlyList<string> ViewportCoverage,
    IReadOnlyList<string> EcommerceRegions,
    IReadOnlyList<string> PresentationMappings,
    IReadOnlyList<PageCompositionNode> CompositionTree,
    IReadOnlyDictionary<string, string> PageTokenOverrides,
    string? TargetViewSlot,
    string? TargetGeneratedFilePath,
    IReadOnlyList<string> UnsupportedOrBlockedRegions);

public sealed record PageCompositionNode(
    string NodeId,
    string Role,
    string? PresentationMappingId,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<PageCompositionNode> Children);

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
