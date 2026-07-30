namespace BlazorShop.AI.StorefrontReverseEngineering.Evidence;

public sealed record ElementEvidenceIndex(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string ViewportId,
    string? RunId,
    IReadOnlyList<ElementEvidenceItem> Elements);

public sealed record ElementEvidenceItem(
    string EvidenceId,
    string Selector,
    string Category,
    string? TextSnippet,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> StyleGroups,
    ElementBox? Box);

public sealed record ElementBox(decimal X, decimal Y, decimal Width, decimal Height);

public sealed record AssetInventoryEvidence(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string ViewportId,
    string? RunId,
    IReadOnlyList<AssetEvidenceItem> Assets);

public sealed record AssetEvidenceItem(
    string EvidenceId,
    string Url,
    string MediaType,
    int? Width,
    int? Height,
    string SourceElement,
    bool ReferenceOnly);

public sealed record PageCaptureManifest(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    string? RunId,
    IReadOnlyList<string> ViewportManifestPaths,
    IReadOnlyList<string> EvidenceArtifactPaths);

public sealed record EvidenceExtractionOptions(
    int MaximumElements = 80,
    int MaximumDomDepth = 10);
