namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record AgentHandoffManifest(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceProjectPath,
    string? SourceRunId,
    string? SourceCommitSha,
    string HandoffSchemaVersion,
    bool ReadinessPassed,
    IReadOnlyList<string> ArtifactList,
    string RequiredConsumerContract,
    IReadOnlyList<string> UnsupportedPatternSummary);

public sealed record AgentHandoffFileManifest(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<string> Paths,
    IReadOnlyList<string> Rules);

public sealed record AgentHandoffUnresolvedRegions(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<string> BlockingRegions,
    IReadOnlyList<string> WarningRegions);

public sealed record AgentHandoffReadinessReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    bool Passed,
    IReadOnlyList<AgentHandoffReadinessFinding> Findings,
    string? AgentHandoffPath);

public sealed record AgentHandoffReadinessFinding(
    string Code,
    string Severity,
    string Message,
    string? ArtifactPath = null);
