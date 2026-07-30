namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record AgentHandoffManifest(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceProjectPath,
    string SourceProjectPathRole,
    string HandoffRoot,
    string? SourceRunId,
    string? SourceCommitSha,
    string HandoffSchemaVersion,
    bool ReadinessPassed,
    string? ReviewBundleHash,
    string? StorefrontPatternHash,
    string? PresentationCatalogHash,
    string? VisualBlueprintHash,
    string? PageCompositionsHash,
    string? EvidenceManifestHash,
    IReadOnlyList<string> ArtifactList,
    IReadOnlyList<AgentHandoffArtifactEntry> ArtifactEntries,
    string RequiredConsumerContract,
    IReadOnlyList<string> UnsupportedPatternSummary);

public sealed record AgentHandoffArtifactEntry(
    string Path,
    string ArtifactKind,
    string Sha256,
    long SizeBytes,
    bool Required);

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

public sealed record AgentHandoffEvidenceManifest(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<AgentHandoffEvidencePage> Pages);

public sealed record AgentHandoffEvidencePage(
    string PageId,
    string SourceUrl,
    IReadOnlyList<AgentHandoffScreenshotEvidence> Screenshots,
    IReadOnlyList<AgentHandoffSectionEvidence> Sections);

public sealed record AgentHandoffScreenshotEvidence(
    string ViewportId,
    string HandoffPath,
    string SourcePath,
    string Sha256,
    int ViewportWidth,
    int ViewportHeight,
    int DocumentWidth,
    int DocumentHeight,
    string Scale,
    IReadOnlyList<string> OriginalityRestrictions);

public sealed record AgentHandoffSectionEvidence(
    string SectionId,
    string? SlotId,
    string ViewportId,
    string HandoffPath,
    string SourcePath,
    string Sha256,
    string Bounds,
    string InteractionState,
    IReadOnlyList<string> OriginalityRestrictions);
