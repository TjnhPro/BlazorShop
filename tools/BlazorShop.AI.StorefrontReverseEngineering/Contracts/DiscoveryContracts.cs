namespace BlazorShop.AI.StorefrontReverseEngineering.Contracts;

public sealed record ReconnaissanceReport(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceUrl,
    IReadOnlyList<ReconnaissanceBlocker> Blockers,
    IReadOnlyList<string> Warnings,
    int MaximumPages);

public sealed record ReconnaissanceBlocker(
    string Code,
    string Severity,
    string Message);
