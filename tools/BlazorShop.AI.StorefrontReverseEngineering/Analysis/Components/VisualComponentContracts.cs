namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;

public sealed record ComponentCandidatesDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<VisualComponentCandidate> Candidates,
    IReadOnlyList<ComponentDetectionIssue> Issues);

public sealed record VisualComponentCandidate(
    string FamilyId,
    string Family,
    string VariantId,
    decimal Confidence,
    IReadOnlyList<string> InstanceIds,
    IReadOnlyList<ComponentSlot> Slots,
    IReadOnlyList<string> TokenReferences,
    IReadOnlyList<string> LocalOverrideIds,
    IReadOnlyList<string> ResponsiveBehaviorRefs,
    IReadOnlyList<string> InteractionBehaviorRefs,
    IReadOnlyList<string> Alternatives,
    bool HumanReviewRequired,
    IReadOnlyList<string> EvidenceIds);

public sealed record ComponentInstancesDocument(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<VisualComponentInstance> Instances);

public sealed record VisualComponentInstance(
    string InstanceId,
    string FamilyId,
    string VariantId,
    string PageId,
    string ViewportId,
    string Selector,
    IReadOnlyList<string> EvidenceIds);

public sealed record ComponentSlot(
    string SlotName,
    string SlotKind,
    IReadOnlyList<string> EvidenceIds,
    decimal Confidence);

public sealed record ComponentDetectionIssue(
    string Code,
    string Severity,
    string Message,
    IReadOnlyList<string> EvidenceIds);
