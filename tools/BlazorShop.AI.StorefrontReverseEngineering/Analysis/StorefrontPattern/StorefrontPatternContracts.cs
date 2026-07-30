namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;

public sealed record StorefrontPatternContract(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    StorefrontPatternMetadata Metadata,
    StorefrontGenerationZones GenerationZones,
    IReadOnlyList<StorefrontBehaviorBoundary> BehaviorBoundaries,
    IReadOnlyList<StorefrontPageContract> PageContracts,
    IReadOnlyList<StorefrontSlotContract> Slots,
    IReadOnlyList<StorefrontRouteContract> Routes,
    IReadOnlyList<StorefrontActionContract> Actions,
    IReadOnlyList<StorefrontProtectedFileContract> ProtectedFiles,
    IReadOnlyList<StorefrontGeneratedFileContract> GeneratedFiles,
    IReadOnlyDictionary<string, string> Extensions);

public sealed record StorefrontPatternMetadata(
    string ContractVersion,
    string StarterTemplateVersion,
    string TargetFramework,
    string GeneratedProjectNamingConvention,
    string GeneratedProjectOutputRoot,
    IReadOnlyDictionary<string, string> PackageVersionMetadata,
    string FileOverwritePolicy);

public sealed record StorefrontGenerationZones(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<string> ManagedZones,
    IReadOnlyList<string> GeneratedZones,
    IReadOnlyList<string> ProtectedZones,
    IReadOnlyList<string> AssetZones,
    string AnalysisArtifactZone);

public sealed record StorefrontBehaviorBoundariesDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<StorefrontBehaviorBoundary> Boundaries,
    IReadOnlyList<StorefrontActionContract> Actions);

public sealed record StorefrontPageContractsDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<StorefrontPageContract> Pages);

public sealed record StorefrontBehaviorBoundary(
    string BoundaryId,
    string Owner,
    string Policy,
    IReadOnlyList<string> ProtectedDescriptors,
    IReadOnlyList<string> RequiredSameOriginBffActions,
    IReadOnlyList<string> ProhibitedBehavior);

public sealed record StorefrontPageContract(
    string PageId,
    string StablePageArchetype,
    string RouteOwnership,
    IReadOnlyList<string> Routes,
    IReadOnlyList<string> AllowedVisualSlots,
    IReadOnlyList<string> RequiredVisualRegions,
    IReadOnlyList<string> OptionalVisualRegions,
    IReadOnlyList<string> ProhibitedBehavior,
    IReadOnlyList<string> ProtectedActionDescriptors,
    IReadOnlyList<string> TargetGeneratedPathRules,
    IReadOnlyList<string> SupportedResponsiveZones);

public sealed record StorefrontRouteContract(
    string Route,
    string Path,
    string RenderOwner,
    string HydrationMode,
    string PageId);

public sealed record StorefrontSlotContract(
    string SlotId,
    string Owner,
    string Path,
    string? Action,
    string Category,
    string GeneratedZone,
    bool VisualGenerationTarget);

public sealed record StorefrontActionContract(
    string ActionId,
    string Owner,
    string? Descriptor,
    string? Route,
    string? RouteSource,
    bool SameOriginBffOnly);

public sealed record StorefrontProtectedFileContract(
    string Path,
    string Reason);

public sealed record StorefrontGeneratedFileContract(
    string PathPattern,
    string Zone,
    IReadOnlyList<string> AllowedOperations);
