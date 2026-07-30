namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;

public sealed record EcommerceRegionsDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    IReadOnlyList<EcommerceRegion> Regions);

public sealed record EcommerceRegion(
    string RegionId,
    string Role,
    string DataDependency,
    string BehaviorContractRequirement,
    bool SeoRelevant,
    bool PresentationOnly,
    bool Unsupported,
    IReadOnlyList<string> SourceSectionIds,
    IReadOnlyList<string> SourceComponentFamilyIds,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> Alternatives);
