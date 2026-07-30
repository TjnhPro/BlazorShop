using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Contracts;

public sealed record VisualArtifactMetadata(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string? PageId = null,
    string? ViewportId = null,
    string? RunId = null,
    string? SourceUrl = null,
    IReadOnlyList<string>? EvidenceIds = null);

public sealed record CapturePolicy(
    int TimeoutMilliseconds = 30000,
    int MaximumPageHeight = 12000,
    int MaximumPages = 1,
    bool PreserveViewportSegments = false,
    bool StrictWarnings = false,
    bool EnableAutomaticStitchedFallback = true,
    double MaximumSingleColorRatio = 0.98);

public sealed record OriginalityPolicy(
    bool TreatExternalAssetsAsReferenceOnly = true,
    bool FlagLikelyBrandAssets = true,
    bool RequireHumanReviewForSourceCopy = true);

public sealed record VisualProjectConfiguration(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string Name,
    string ReferenceUrl,
    string OutputRoot,
    CapturePolicy CapturePolicy,
    OriginalityPolicy OriginalityPolicy,
    IReadOnlyList<ViewportDefinition> Viewports);

public sealed record VisualProject(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string Name,
    string ReferenceUrl,
    string ArtifactRoot,
    VisualProjectStatus Status,
    string? LatestRunId = null,
    DateTimeOffset? UpdatedUtc = null);

public sealed record ReferenceSiteProfile(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceUrl,
    string? Title,
    string? CanonicalUrl,
    string? Language,
    string? MetaViewport,
    int DocumentWidth,
    int DocumentHeight);

public sealed record CapturePlan(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<CapturePlanPage> Pages,
    IReadOnlyList<ViewportDefinition> Viewports);

public sealed record CapturePlanPage(string PageId, string Url, string Label);
