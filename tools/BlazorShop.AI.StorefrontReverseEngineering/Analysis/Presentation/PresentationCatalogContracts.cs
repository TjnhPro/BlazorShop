namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;

public sealed record PresentationComponentCatalog(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<PresentationCatalogEntry> Components,
    IReadOnlyList<string> SourcePaths);

public sealed record PresentationCatalogEntry(
    string ComponentId,
    string Category,
    IReadOnlyList<string> SupportedPageArchetypes,
    IReadOnlyList<string> SupportedRegionRoles,
    IReadOnlyList<string> Slots,
    IReadOnlyList<string> Variants,
    IReadOnlyList<string> VisualProperties,
    IReadOnlyList<string> ResponsiveCapabilities,
    IReadOnlyList<string> InteractionCapabilities,
    string DataContract,
    bool BehaviorOwnedByPresentation,
    bool BehaviorOwnedByRuntime,
    bool VisualOverrideAllowed,
    bool BehaviorOverrideAllowed,
    IReadOnlyList<string> RequiredChildren,
    IReadOnlyList<string> OptionalChildren,
    IReadOnlyList<string> UnsupportedPatterns,
    IReadOnlyList<string> SourceFiles,
    string ContractVersion);

public sealed record PresentationCatalogValidationReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    bool Passed,
    IReadOnlyList<PresentationCatalogValidationFinding> Findings);

public sealed record PresentationCatalogValidationFinding(
    string Code,
    string Severity,
    string Message);
