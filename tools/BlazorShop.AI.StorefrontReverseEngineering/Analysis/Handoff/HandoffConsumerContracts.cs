using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record HandoffDiagnosticReference(
    string Path,
    string Role,
    bool ConsumerReadable);

public sealed record HandoffVisualBlueprint(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyDictionary<string, string> ConsumerReferences,
    IReadOnlyList<HandoffDiagnosticReference> DiagnosticProvenance,
    IReadOnlyDictionary<string, string> SourceArtifactHashes,
    IReadOnlyList<string> Pages,
    IReadOnlyList<string> GenerationRestrictions);

public sealed record HandoffPageCompositions(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<HandoffDiagnosticReference> DiagnosticProvenance,
    SiteBlueprint Site,
    IReadOnlyList<PageBlueprint> Pages,
    IReadOnlyList<PageComposition> Compositions);

public sealed record HandoffPresentationCatalog(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    IReadOnlyList<PresentationCatalogEntry> Components,
    IReadOnlyList<HandoffDiagnosticReference> DiagnosticProvenance);

public sealed record HandoffSemanticTokens(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<SemanticToken> Tokens,
    IReadOnlyList<SemanticTokenOverride> PageLocalOverrides,
    IReadOnlyList<SemanticTokenOverride> ComponentLocalOverrides,
    bool HumanReviewRequired,
    IReadOnlyList<string> ReviewReasons,
    IReadOnlyList<HandoffDiagnosticReference> DiagnosticProvenance);

public sealed record HandoffResponsiveBehaviorDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string ReviewStatus,
    IReadOnlyList<HandoffResponsiveBehaviorPage> Pages);

public sealed record HandoffResponsiveBehaviorPage(
    string PageId,
    IReadOnlyList<ResponsiveSectionBehavior> Sections,
    IReadOnlyList<string> InferredBreakpointRanges,
    IReadOnlyList<ResponsiveBehaviorIssue> Issues);

public sealed record HandoffInteractionModelsDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string ReviewStatus,
    IReadOnlyList<HandoffInteractionModelPage> Pages);

public sealed record HandoffInteractionModelPage(
    string PageId,
    IReadOnlyList<InteractionPattern> Interactions,
    IReadOnlyList<ResponsiveBehaviorIssue> Issues);

public sealed record HandoffReviewResolution(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceReviewQueueId,
    string SourceReviewQueueHash,
    string DecisionBundleHash,
    int ResolvedItemCount,
    int BlockingUnresolvedCount,
    IReadOnlyList<string> ResolvedArtifactReferences,
    IReadOnlyList<string> BlockedItems,
    IReadOnlyList<HandoffDiagnosticReference> DiagnosticProvenance);
