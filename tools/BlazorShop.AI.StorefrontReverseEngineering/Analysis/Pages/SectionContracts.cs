namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;

public sealed record SectionsDraftDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string PageId,
    IReadOnlyList<SectionDraft> Sections,
    IReadOnlyList<SectionSegmentationIssue> Issues);

public sealed record SectionDraft(
    string SectionId,
    string SectionType,
    int Order,
    decimal Confidence,
    SectionBounds Bounds,
    string? ParentSectionId,
    IReadOnlyList<string> ChildSectionIds,
    string CrossViewportIdentityKey,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyDictionary<string, SectionBounds>? ViewportBoundingBoxes = null);

public sealed record SectionBounds(
    decimal X,
    decimal Y,
    decimal Width,
    decimal Height);

public sealed record SectionSegmentationIssue(
    string Code,
    string Severity,
    string Message,
    IReadOnlyList<string> EvidenceIds);
