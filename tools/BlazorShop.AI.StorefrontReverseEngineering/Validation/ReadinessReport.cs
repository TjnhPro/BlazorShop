namespace BlazorShop.AI.StorefrontReverseEngineering.Validation;

public sealed record ReadinessReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    bool Passed,
    IReadOnlyList<ReadinessFinding> Findings,
    IReadOnlyList<string> RequiredArtifacts);

public sealed record ReadinessFinding(
    string Code,
    string Severity,
    string Message);
