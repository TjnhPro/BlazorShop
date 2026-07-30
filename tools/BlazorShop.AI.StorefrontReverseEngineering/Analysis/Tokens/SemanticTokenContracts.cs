namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;

public sealed record SemanticTokenDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceRawTokensPath,
    IReadOnlyList<SemanticToken> Tokens,
    IReadOnlyList<SemanticTokenOverride> PageLocalOverrides,
    IReadOnlyList<SemanticTokenOverride> ComponentLocalOverrides,
    bool HumanReviewRequired,
    IReadOnlyList<string> ReviewReasons);

public sealed record SemanticToken(
    string Role,
    string Group,
    IReadOnlyList<string> Values,
    IReadOnlyList<string> RawTokenIds,
    IReadOnlyList<string> EvidenceIds,
    decimal Confidence,
    IReadOnlyList<string> ReasonCodes,
    bool HumanReviewRequired);

public sealed record SemanticTokenOverride(
    string ScopeId,
    string Role,
    IReadOnlyList<string> RawTokenIds,
    IReadOnlyList<string> EvidenceIds,
    string ReasonCode);

public sealed record SemanticTokenConflictReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<SemanticTokenConflict> Conflicts);

public sealed record SemanticTokenConflict(
    string Role,
    string Group,
    IReadOnlyList<string> CandidateRawTokenIds,
    IReadOnlyList<string> CandidateValues,
    string ReasonCode,
    bool HumanReviewRequired);
