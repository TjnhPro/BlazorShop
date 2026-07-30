namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;

public sealed record RawDesignTokenDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceSnapshotPath,
    IReadOnlyList<RawDesignToken> Tokens,
    IReadOnlyList<RawDesignTokenIssue> Issues);

public sealed record RawDesignToken(
    string TokenId,
    string Group,
    string PropertyName,
    string NormalizedValue,
    IReadOnlyList<string> LiteralValues,
    int ProjectFrequency,
    IReadOnlyList<TokenFrequency> PageFrequencies,
    IReadOnlyList<TokenFrequency> ViewportFrequencies,
    IReadOnlyList<string> SourceEvidenceIds,
    IReadOnlyList<string> SourceArtifactPaths,
    bool Outlier,
    string? NearDuplicateClusterId,
    IReadOnlyList<string> Hints);

public sealed record TokenFrequency(
    string ScopeId,
    int Count);

public sealed record RawDesignTokenIssue(
    string Code,
    string Severity,
    string Message,
    string? EvidenceId = null);

public sealed record RawDesignTokenFrequencyReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    int TotalTokenCount,
    IReadOnlyList<TokenGroupFrequency> Groups,
    IReadOnlyList<RawDesignTokenIssue> Issues);

public sealed record TokenGroupFrequency(
    string Group,
    int TokenCount,
    int ObservationCount);
