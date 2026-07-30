namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;

public sealed record ConfidenceReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    decimal ProjectConfidence,
    IReadOnlyList<ConfidenceItem> Items,
    ConfidenceThresholds Thresholds);

public sealed record ConfidenceThresholds(decimal CriticalReviewThreshold = 0.60m, decimal WarningThreshold = 0.75m);

public sealed record ConfidenceItem(
    string ItemId,
    string ItemType,
    decimal Confidence,
    bool Critical,
    IReadOnlyList<string> FactorCodes,
    IReadOnlyList<string> EvidenceIds,
    object Proposal);

public sealed record ReviewQueue(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<ReviewQueueItem> Items);

public sealed record ReviewQueueItem(
    string ItemId,
    string ItemType,
    decimal OriginalConfidence,
    object OriginalProposal,
    IReadOnlyList<string> EvidenceIds,
    bool Blocking,
    string SourceArtifactId = "",
    string SourceArtifactHash = "");

public sealed record ReviewDecisions(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<ReviewDecision> Decisions);

public sealed record ReviewDecision(
    string ItemId,
    string Status,
    object? ModifiedValue,
    string? ReviewerNote,
    DateTimeOffset DecidedUtc,
    string Reviewer = "",
    string SourceArtifactId = "",
    string SourceArtifactHash = "",
    string DecisionId = "",
    string? SupersedesDecisionId = null);

public sealed record ReviewedItems(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<ReviewedItem> Items,
    bool BlocksReadiness);

public sealed record ReviewedItem(
    string ItemId,
    string Status,
    object OriginalProposal,
    decimal OriginalConfidence,
    object? ModifiedValue,
    string? ReviewerNote,
    DateTimeOffset DecidedUtc);
