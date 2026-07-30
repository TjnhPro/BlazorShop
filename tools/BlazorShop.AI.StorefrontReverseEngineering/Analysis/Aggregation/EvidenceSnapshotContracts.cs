using BlazorShop.AI.StorefrontReverseEngineering.Evidence;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;

public sealed record EvidenceSnapshot(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string? LatestRunId,
    string ReadinessReportPath,
    IReadOnlyList<string> SourceArtifactPaths,
    IReadOnlyList<string> SourceEvidenceIds,
    IReadOnlyList<EvidenceSnapshotPage> Pages,
    IReadOnlyList<EvidenceSnapshotIssue> Issues);

public sealed record EvidenceSnapshotPage(
    string PageId,
    string Url,
    string Label,
    IReadOnlyList<EvidenceSnapshotViewport> Viewports,
    IReadOnlyList<string> SourceArtifactPaths);

public sealed record EvidenceSnapshotViewport(
    string ViewportId,
    int ViewportWidth,
    int ViewportHeight,
    int DocumentWidth,
    int DocumentHeight,
    string? CaptureCorrelationId,
    string CaptureMethod,
    bool QualityPassed,
    IReadOnlyList<EvidenceSnapshotElement> Elements,
    IReadOnlyList<EvidenceSnapshotAsset> Assets,
    IReadOnlyList<string> SourceArtifactPaths,
    IReadOnlyList<EvidenceSnapshotIssue> Issues);

public sealed record EvidenceSnapshotElement(
    string EvidenceId,
    string Selector,
    string Category,
    string? TextSnippet,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> StyleGroups,
    ElementBox? Box,
    string SourceArtifactPath);

public sealed record EvidenceSnapshotAsset(
    string EvidenceId,
    string Url,
    string MediaType,
    int? Width,
    int? Height,
    string SourceElement,
    bool ReferenceOnly,
    string SourceArtifactPath);

public sealed record EvidenceSnapshotIssue(
    string Code,
    string Severity,
    string Message,
    string? PageId = null,
    string? ViewportId = null,
    string? ArtifactPath = null);
