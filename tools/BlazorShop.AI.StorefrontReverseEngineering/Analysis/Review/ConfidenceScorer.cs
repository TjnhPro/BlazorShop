using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;

public sealed class ConfidenceScorer
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public ConfidenceScorer(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<ConfidenceReport> ScoreAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var semantic = await store.ReadJsonAsync<SemanticTokenDocument>(ArtifactPath.Create("analysis/tokens/semantic-tokens.draft.json"), "semantic-tokens", cancellationToken);
        var components = await store.ReadJsonAsync<ComponentCandidatesDocument>(ArtifactPath.Create("analysis/components/component-candidates.json"), "component-candidates", cancellationToken);
        var mappings = await store.ReadJsonAsync<PresentationMappingsDocument>(ArtifactPath.Create("analysis/mapping/presentation-mappings.draft.json"), "presentation-mappings", cancellationToken);
        var unsupported = await store.ReadJsonAsync<UnsupportedPatternsDocument>(ArtifactPath.Create("analysis/mapping/unsupported-patterns.json"), "unsupported-patterns", cancellationToken);
        var items = new List<ConfidenceItem>();
        items.AddRange(semantic.Tokens.Select(token => Item($"token:{token.Role}", "semantic tokens", token.Confidence, token.HumanReviewRequired, ["token-consistency", "rule-strength"], token.EvidenceIds, token)));
        items.AddRange(components.Candidates.Select(component => Item($"component:{component.FamilyId}", "component families", component.Confidence, component.HumanReviewRequired, ["structural-similarity", "repetition-count", "cross-viewport-consistency"], component.EvidenceIds, component)));
        items.AddRange(mappings.Mappings.Select(mapping => Item($"mapping:{mapping.SourceCandidateId}", "Presentation mappings", mapping.Confidence, mapping.HumanReviewRequired, ["catalog-compatibility", "rule-strength"], mapping.EvidenceIds, mapping)));
        items.AddRange(unsupported.Patterns.Select(pattern => Item($"unsupported:{pattern.SourceCandidateId}", "unsupported patterns", 0.20m, true, ["ambiguity", "catalog-compatibility"], pattern.EvidenceIds, pattern)));

        foreach (var pagePath in Directory.EnumerateFiles(Path.Combine(root, "analysis", "pages"), "page-archetype.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, pagePath).Replace(Path.DirectorySeparatorChar, '/');
            var page = await store.ReadJsonAsync<PageArchetypeDocument>(ArtifactPath.Create(relative), "page-archetype", cancellationToken);
            items.Add(Item($"page:{page.PageId}", "page archetype", page.Confidence, page.PrimaryArchetype == "unknown", ["evidence-completeness", "rule-strength"], page.EvidenceIds, page));
        }

        foreach (var sectionPath in Directory.EnumerateFiles(Path.Combine(root, "analysis", "pages"), "sections.draft.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, sectionPath).Replace(Path.DirectorySeparatorChar, '/');
            var sections = await store.ReadJsonAsync<SectionsDraftDocument>(ArtifactPath.Create(relative), "sections", cancellationToken);
            items.AddRange(sections.Sections.Select(section => Item($"section:{sections.PageId}:{section.SectionId}", "sections", section.Confidence, section.SectionType == "unknown section", ["evidence-completeness", "structural-similarity"], section.EvidenceIds, section)));
        }

        foreach (var regionPath in Directory.EnumerateFiles(Path.Combine(root, "analysis", "pages"), "ecommerce-regions.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, regionPath).Replace(Path.DirectorySeparatorChar, '/');
            var regions = await store.ReadJsonAsync<EcommerceRegionsDocument>(ArtifactPath.Create(relative), "ecommerce-regions", cancellationToken);
            items.AddRange(regions.Regions.Select(region => Item($"region:{regions.PageId}:{region.RegionId}", "ecommerce roles", region.Unsupported ? 0.30m : 0.68m, region.Unsupported, ["data-dependency", "behavior-ownership"], region.EvidenceIds, region)));
        }

        var report = new ConfidenceReport(
            "1.0",
            "confidence-report",
            $"confidence-{semantic.ProjectId}",
            DateTimeOffset.UtcNow,
            semantic.ProjectId,
            items.Count == 0 ? 0m : Math.Round(items.Average(item => item.Confidence), 3),
            items.OrderBy(item => item.ItemId, StringComparer.Ordinal).ToArray(),
            new ConfidenceThresholds());
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/confidence/confidence-report.json"), "confidence-report", report, cancellationToken);
        await new ReviewQueueBuilder().BuildAsync(root, store, report, cancellationToken);
        return report;
    }

    private static ConfidenceItem Item(string id, string type, decimal confidence, bool critical, IReadOnlyList<string> factors, IReadOnlyList<string> evidenceIds, object proposal) =>
        new(id, type, confidence, critical, factors, evidenceIds, proposal);
}

public sealed class ReviewQueueBuilder
{
    public async Task<ReviewQueue> BuildAsync(
        string root,
        FileSystemVisualArtifactStore store,
        ConfidenceReport report,
        CancellationToken cancellationToken)
    {
        var queue = new ReviewQueue(
            "1.0",
            "review-queue",
            $"review-queue-{report.ProjectId}",
            DateTimeOffset.UtcNow,
            report.ProjectId,
            report.Items
                .Where(item => item.Critical || item.Confidence < report.Thresholds.CriticalReviewThreshold)
                .Select(item => new ReviewQueueItem(item.ItemId, item.ItemType, item.Confidence, item.Proposal, item.EvidenceIds, item.Critical))
                .ToArray());
        var decisionsPath = Path.Combine(root, "review", "review-decisions.json");
        if (!File.Exists(decisionsPath))
        {
            await store.WriteJsonAsync(ArtifactPath.Create("review/review-decisions.json"), "review-decisions", new ReviewDecisions("1.0", "review-decisions", $"review-decisions-{report.ProjectId}", DateTimeOffset.UtcNow, report.ProjectId, []), cancellationToken);
        }

        await store.WriteJsonAsync(ArtifactPath.Create("review/review-queue.json"), "review-queue", queue, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "review", "review-pack.md"), WritePack(queue), cancellationToken);
        return queue;
    }

    private static string WritePack(ReviewQueue queue) =>
        "# Review Pack" + Environment.NewLine + Environment.NewLine +
        string.Join(Environment.NewLine, queue.Items.Select(item => $"- `{item.ItemId}` ({item.ItemType}) confidence `{item.OriginalConfidence}`")) +
        Environment.NewLine;
}

public sealed class ReviewDecisionApplier
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public ReviewDecisionApplier(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<ReviewedItems> ApplyAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var queue = await store.ReadJsonAsync<ReviewQueue>(ArtifactPath.Create("review/review-queue.json"), "review-queue", cancellationToken);
        var decisions = await store.ReadJsonAsync<ReviewDecisions>(ArtifactPath.Create("review/review-decisions.json"), "review-decisions", cancellationToken);
        var reviewed = queue.Items.Select(item =>
        {
            var decision = decisions.Decisions.FirstOrDefault(candidate => candidate.ItemId == item.ItemId)
                ?? new ReviewDecision(item.ItemId, "Deferred", null, "No decision recorded.", DateTimeOffset.UtcNow);
            return new ReviewedItem(item.ItemId, decision.Status, item.OriginalProposal, item.OriginalConfidence, decision.ModifiedValue, decision.ReviewerNote, decision.DecidedUtc);
        }).ToArray();
        var output = new ReviewedItems("1.0", "reviewed-items", $"reviewed-items-{queue.ProjectId}", DateTimeOffset.UtcNow, queue.ProjectId, reviewed, reviewed.Any(item => item.Status is "Rejected" or "Deferred"));
        await store.WriteJsonAsync(ArtifactPath.Create("review/reviewed-items.json"), "reviewed-items", output, cancellationToken);
        return output;
    }
}
