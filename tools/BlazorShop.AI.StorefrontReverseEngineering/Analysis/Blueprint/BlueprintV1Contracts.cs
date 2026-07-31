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
    ReviewedPageCompositionProvenance Provenance,
    SiteBlueprint Site,
    IReadOnlyList<PageBlueprint> Pages,
    IReadOnlyList<PageComposition> Compositions);

public sealed record ReviewedPageCompositionProvenance(
    string ReviewResolutionManifestPath,
    string ReviewBundleHash,
    IReadOnlyDictionary<string, string> SourceResolvedArtifactHashes,
    IReadOnlyList<string> ReviewedInputArtifactPaths,
    IReadOnlyDictionary<string, string> ReviewedInputArtifactKinds);

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
    IReadOnlyList<PageCompositionNode> Children,
    string StableFingerprint,
    string? EcommerceRole,
    string? ParentNodeId,
    IReadOnlyList<string> ChildNodeIds,
    IReadOnlyDictionary<string, string> ViewportBoundingBoxes,
    IReadOnlyList<string> VisualStyleTokenRefs,
    string? ComponentMappingRef,
    string? TargetFilePath,
    string? TargetGeneratedZone,
    IReadOnlyList<string> AllowedOperations,
    IReadOnlyList<string> ProtectedBehaviorMarkers,
    IReadOnlyList<string> ScreenshotReferences,
    IReadOnlyList<string> CropReferences,
    string? ApprovedVisualExtensionId,
    string? ApprovedVisualExtensionReason,
    string? RepeatedGroupId,
    IReadOnlyList<string> StateExpectations,
    IReadOnlyList<string> ResponsiveTransformationRules,
    IReadOnlyList<string> UnresolvedIssues);

public sealed record PageComposition(
    string PageId,
    string PageArchetype,
    string? TargetViewSlot,
    IReadOnlyList<PageCompositionNode> SectionTree,
    IReadOnlyList<string> LayoutZones,
    IReadOnlyList<PageRepeatedGroup> RepeatedGroups,
    IReadOnlyList<string> ResponsiveTransformationRules,
    IReadOnlyList<string> SourceEvidenceLinks,
    IReadOnlyList<string> UnresolvedIssues);

public sealed record PageRepeatedGroup(
    string GroupId,
    string SemanticRole,
    IReadOnlyList<string> SectionIds,
    string? TargetFilePath);

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
