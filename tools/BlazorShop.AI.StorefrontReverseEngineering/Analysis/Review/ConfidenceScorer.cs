using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
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
                .Select(item => new ReviewQueueItem(
                    item.ItemId,
                    item.ItemType,
                    item.Confidence,
                    item.Proposal,
                    item.EvidenceIds,
                    item.Critical,
                    SourceArtifactId(item.ItemId),
                    StableHash(item.Proposal)))
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
        string.Join(Environment.NewLine, queue.Items.Select(item => $"- `{item.ItemId}` ({item.ItemType}) confidence `{item.OriginalConfidence}` source `{item.SourceArtifactId}` hash `{item.SourceArtifactHash}`")) +
        Environment.NewLine;

    private static string SourceArtifactId(string itemId) =>
        itemId.Split(':')[0] switch
        {
            "token" => "analysis/tokens/semantic-tokens.draft.json",
            "component" => "analysis/components/component-candidates.json",
            "mapping" => "analysis/mapping/presentation-mappings.draft.json",
            "unsupported" => "analysis/mapping/unsupported-patterns.json",
            "page" => "analysis/pages/*/page-archetype.json",
            "section" => "analysis/pages/*/sections.draft.json",
            "region" => "analysis/pages/*/ecommerce-regions.json",
            _ => "review/review-queue.json"
        };

    private static string StableHash(object value)
    {
        var json = JsonSerializer.Serialize(value, VisualJson.Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
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
        ValidateDecisions(queue, decisions);
        var reviewed = queue.Items.Select(item =>
        {
            var decision = decisions.Decisions.FirstOrDefault(candidate => candidate.ItemId == item.ItemId)
                ?? new ReviewDecision(item.ItemId, "Deferred", null, "No decision recorded.", DateTimeOffset.UtcNow, "system", item.SourceArtifactId, item.SourceArtifactHash, $"missing-{item.ItemId}");
            return new ReviewedItem(item.ItemId, decision.Status, item.OriginalProposal, item.OriginalConfidence, decision.ModifiedValue, decision.ReviewerNote, decision.DecidedUtc);
        }).ToArray();
        var output = new ReviewedItems("1.0", "reviewed-items", $"reviewed-items-{queue.ProjectId}", DateTimeOffset.UtcNow, queue.ProjectId, reviewed, reviewed.Any(item => item.Status is "Rejected" or "Deferred"));
        await store.WriteJsonAsync(ArtifactPath.Create("review/reviewed-items.json"), "reviewed-items", output, cancellationToken);
        await new ResolvedReviewArtifactWriter(root)
            .WriteAsync(store, queue, decisions, output, cancellationToken);
        return output;
    }

    private static void ValidateDecisions(ReviewQueue queue, ReviewDecisions decisions)
    {
        var queueById = queue.Items.ToDictionary(item => item.ItemId, StringComparer.Ordinal);
        var grouped = decisions.Decisions.GroupBy(decision => decision.ItemId, StringComparer.Ordinal);
        foreach (var group in grouped)
        {
            if (!queueById.ContainsKey(group.Key))
            {
                throw new InvalidOperationException($"Review decision targets unknown item '{group.Key}'.");
            }

            var decisionsForItem = group.ToArray();
            if (decisionsForItem.Length > 1 && decisionsForItem.Any(decision => string.IsNullOrWhiteSpace(decision.SupersedesDecisionId)))
            {
                throw new InvalidOperationException($"Duplicate review decisions for '{group.Key}' must explicitly supersede earlier decisions.");
            }
        }

        foreach (var decision in decisions.Decisions)
        {
            var item = queueById[decision.ItemId];
            if (decision.Status is not ("Approved" or "Modified" or "Rejected" or "Deferred"))
            {
                throw new InvalidOperationException($"Unknown review decision status '{decision.Status}' for '{decision.ItemId}'.");
            }

            if (decision.Status == "Modified" && decision.ModifiedValue is null)
            {
                throw new InvalidOperationException($"Modified review decision '{decision.ItemId}' must include modifiedValue.");
            }

            if (decision.Status is "Rejected" or "Deferred" && string.IsNullOrWhiteSpace(decision.ReviewerNote))
            {
                throw new InvalidOperationException($"Review decision '{decision.ItemId}' must include a reason.");
            }

            if (string.IsNullOrWhiteSpace(decision.Reviewer) ||
                string.IsNullOrWhiteSpace(decision.SourceArtifactId) ||
                string.IsNullOrWhiteSpace(decision.SourceArtifactHash) ||
                string.IsNullOrWhiteSpace(decision.DecisionId))
            {
                throw new InvalidOperationException($"Review decision '{decision.ItemId}' is missing reviewer metadata.");
            }

            if (!string.Equals(decision.SourceArtifactId, item.SourceArtifactId, StringComparison.Ordinal) ||
                !string.Equals(decision.SourceArtifactHash, item.SourceArtifactHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Review decision '{decision.ItemId}' is stale for source artifact '{item.SourceArtifactId}'.");
            }
        }
    }
}
