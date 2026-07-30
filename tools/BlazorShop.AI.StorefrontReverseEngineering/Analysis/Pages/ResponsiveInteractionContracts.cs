namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;

public sealed record ResponsiveBehaviorDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    IReadOnlyList<ResponsiveSectionBehavior> Sections,
    IReadOnlyList<string> InferredBreakpointRanges,
    IReadOnlyList<ResponsiveBehaviorIssue> Issues);

public sealed record ResponsiveSectionBehavior(
    string CrossViewportIdentityKey,
    IReadOnlyList<ResponsiveViewportObservation> Viewports,
    IReadOnlyList<string> BehaviorFlags,
    IReadOnlyList<string> EvidenceIds);

public sealed record ResponsiveViewportObservation(
    string ViewportId,
    decimal? X,
    decimal? Y,
    decimal? Width,
    decimal? Height,
    string? Display,
    string? Visibility,
    string? Position,
    string? Gap,
    string? FontSize,
    int AssetCount);

public sealed record ResponsiveBehaviorIssue(
    string Code,
    string Severity,
    string Message,
    IReadOnlyList<string> EvidenceIds);

public sealed record InteractionModelDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    IReadOnlyList<InteractionPattern> Interactions,
    IReadOnlyList<ResponsiveBehaviorIssue> Issues);

public sealed record InteractionPattern(
    string StateName,
    string InteractionType,
    string Classification,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> ReasonCodes,
    string? BeforeStylesPath,
    string? AfterStylesPath);
