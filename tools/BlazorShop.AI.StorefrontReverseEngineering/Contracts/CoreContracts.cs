using BlazorShop.AI.StorefrontReverseEngineering.Domain;

namespace BlazorShop.AI.StorefrontReverseEngineering.Contracts;

public sealed record VisualArtifactMetadata(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string? PageId = null,
    string? ViewportId = null,
    string? RunId = null,
    string? SourceUrl = null,
    IReadOnlyList<string>? EvidenceIds = null);

public sealed record CapturePolicy(
    int TimeoutMilliseconds = 30000,
    int MaximumPageHeight = 12000,
    int MaximumPages = 1,
    bool PreserveViewportSegments = false,
    bool StrictWarnings = false,
    bool EnableAutomaticStitchedFallback = true,
    double MaximumSingleColorRatio = 0.98,
    int MaximumEvidenceElements = 80,
    int MaximumEvidenceAssets = 80,
    int MaximumTextLength = 160,
    int MaximumSegmentCount = 50,
    int SegmentOverlapPixels = 80,
    int ScrollSettleMilliseconds = 100,
    int FinalSettleMilliseconds = 150,
    IReadOnlyList<string>? NoiseSelectors = null);

public static class CapturePolicyDefaults
{
    private static readonly string[] DefaultNoiseSelectors = [".cookie-banner", "[data-capture-noise]"];

    public static IReadOnlyList<string> ResolveNoiseSelectors(CapturePolicy policy) =>
        policy.NoiseSelectors is { Count: > 0 } ? policy.NoiseSelectors : DefaultNoiseSelectors;

    public static void Validate(CapturePolicy policy)
    {
        if (policy.TimeoutMilliseconds <= 0 ||
            policy.MaximumPageHeight <= 0 ||
            policy.MaximumPages <= 0 ||
            policy.MaximumEvidenceElements <= 0 ||
            policy.MaximumEvidenceAssets <= 0 ||
            policy.MaximumTextLength <= 0 ||
            policy.MaximumSegmentCount <= 0 ||
            policy.SegmentOverlapPixels < 0 ||
            policy.ScrollSettleMilliseconds < 0 ||
            policy.FinalSettleMilliseconds < 0 ||
            policy.MaximumSingleColorRatio is < 0 or > 1)
        {
            throw new InvalidOperationException("[SRE-POLICY-001] Capture policy contains invalid limits. Problem: one or more numeric capture policy values are outside supported bounds. Cause: Phase 3A capture must run with positive limits and a single-color ratio between 0 and 1. Fix: update configuration.json with reviewed capturePolicy values.");
        }
    }
}

public sealed record OriginalityPolicy(
    bool TreatExternalAssetsAsReferenceOnly = true,
    bool FlagLikelyBrandAssets = true,
    bool RequireHumanReviewForSourceCopy = true);

public sealed record VisualProjectConfiguration(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string Name,
    string ReferenceUrl,
    string OutputRoot,
    CapturePolicy CapturePolicy,
    OriginalityPolicy OriginalityPolicy,
    IReadOnlyList<ViewportDefinition> Viewports);

public sealed record VisualProject(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string Name,
    string ReferenceUrl,
    string ArtifactRoot,
    VisualProjectStatus Status,
    string? LatestRunId = null,
    DateTimeOffset? UpdatedUtc = null);

public sealed record ReferenceSiteProfile(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    string SourceUrl,
    string? Title,
    string? CanonicalUrl,
    string? Language,
    string? MetaViewport,
    int DocumentWidth,
    int DocumentHeight);

public sealed record CapturePlan(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    IReadOnlyList<CapturePlanPage> Pages,
    IReadOnlyList<ViewportDefinition> Viewports);

public sealed record CapturePlanPage(string PageId, string Url, string Label);
