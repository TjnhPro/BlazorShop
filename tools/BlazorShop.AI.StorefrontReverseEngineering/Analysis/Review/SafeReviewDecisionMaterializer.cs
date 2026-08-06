using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;

public sealed class SafeReviewDecisionMaterializer
{
    private readonly ApprovedArtifactRootResolver resolver;

    public SafeReviewDecisionMaterializer(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
    }

    public async Task<SafeReviewDecisionMaterializationSummary> MaterializeAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var queuePath = Path.Combine(root, "review", "review-queue.json");
        if (!File.Exists(queuePath))
        {
            throw new InvalidOperationException($"[SRE-REVIEW-001] Review queue not found. Problem: '{queuePath}' is missing. Cause: safe review resolution requires score-confidence-review output. Fix: run or resume through score-confidence-review first.");
        }

        var queue = JsonSerializer.Deserialize<ReviewQueue>(await File.ReadAllTextAsync(queuePath, cancellationToken), VisualJson.Options)
            ?? throw new InvalidOperationException("[SRE-REVIEW-002] Review queue did not deserialize.");
        var decisionsPath = Path.Combine(root, "review", "review-decisions.json");
        var existing = File.Exists(decisionsPath)
            ? JsonSerializer.Deserialize<ReviewDecisions>(await File.ReadAllTextAsync(decisionsPath, cancellationToken), VisualJson.Options)?.Decisions.ToList() ?? []
            : [];
        var activeExisting = existing
            .Where(decision => string.IsNullOrWhiteSpace(decision.SupersedesDecisionId))
            .GroupBy(decision => decision.ItemId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var generated = new List<ReviewDecision>();
        var items = new List<SafeReviewDecisionMaterializationItem>();
        var approved = 0;
        var modified = 0;
        var blocked = 0;
        var skipped = 0;
        var stale = 0;

        foreach (var item in queue.Items.OrderBy(item => item.ItemId, StringComparer.Ordinal))
        {
            if (activeExisting.TryGetValue(item.ItemId, out var current))
            {
                if (current.Any(decision => !MatchesSource(item, decision)))
                {
                    stale++;
                    blocked++;
                    items.Add(new SafeReviewDecisionMaterializationItem(item.ItemId, "Blocked", "Existing decision source metadata is stale."));
                    continue;
                }

                skipped++;
                items.Add(new SafeReviewDecisionMaterializationItem(item.ItemId, "Skipped", "Decision already exists with matching source metadata."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.SourceArtifactId) || string.IsNullOrWhiteSpace(item.SourceArtifactHash))
            {
                blocked++;
                items.Add(new SafeReviewDecisionMaterializationItem(item.ItemId, "Blocked", "Review queue item has no source artifact metadata."));
                continue;
            }

            var safety = ClassifySafety(item);
            if (!safety.Safe)
            {
                blocked++;
                items.Add(new SafeReviewDecisionMaterializationItem(item.ItemId, "Blocked", safety.Reason));
                continue;
            }

            generated.Add(new ReviewDecision(
                item.ItemId,
                "Approved",
                null,
                "Auto-approved safe visual-only review item; source artifact metadata matched the review queue.",
                DateTimeOffset.UtcNow,
                "storefront-reverse-engineering-safe-review",
                item.SourceArtifactId,
                item.SourceArtifactHash,
                StableDecisionId(item, "Approved")));
            approved++;
            items.Add(new SafeReviewDecisionMaterializationItem(item.ItemId, "Approved", "Safe visual-only item."));
        }

        var allDecisions = existing.Concat(generated).ToArray();
        var document = new ReviewDecisions("1.0", "review-decisions", "review-decisions-safe-materialized", DateTimeOffset.UtcNow, queue.ProjectId, allDecisions);
        Directory.CreateDirectory(Path.GetDirectoryName(decisionsPath)!);
        await File.WriteAllTextAsync(decisionsPath, JsonSerializer.Serialize(document, VisualJson.Options) + Environment.NewLine, cancellationToken);

        var summaryPath = Path.Combine(root, "review", "review-decision-summary.json");
        var summary = new SafeReviewDecisionMaterializationSummary(
            "1.0",
            "safe-review-decision-summary",
            "safe-review-decision-summary",
            DateTimeOffset.UtcNow,
            queue.ProjectId,
            approved,
            modified,
            blocked,
            skipped,
            stale,
            Path.GetRelativePath(root, decisionsPath).Replace(Path.DirectorySeparatorChar, '/'),
            Path.GetRelativePath(root, summaryPath).Replace(Path.DirectorySeparatorChar, '/'),
            items);
        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary, VisualJson.Options) + Environment.NewLine, cancellationToken);
        return summary;
    }

    private static bool MatchesSource(ReviewQueueItem item, ReviewDecision decision) =>
        string.Equals(item.SourceArtifactId, decision.SourceArtifactId, StringComparison.Ordinal) &&
        string.Equals(item.SourceArtifactHash, decision.SourceArtifactHash, StringComparison.Ordinal);

    private static SafeReviewSafety ClassifySafety(ReviewQueueItem item)
    {
        if (item.ItemId.StartsWith("unsupported:", StringComparison.Ordinal) ||
            item.ItemType.Contains("unsupported", StringComparison.OrdinalIgnoreCase))
        {
            return new SafeReviewSafety(false, "Unsupported patterns require explicit review.");
        }

        var proposal = JsonSerializer.Serialize(item.OriginalProposal, VisualJson.Options);
        if (proposal.Contains("/api/storefront/", StringComparison.OrdinalIgnoreCase) ||
            proposal.Contains("api/storefront/stores/", StringComparison.OrdinalIgnoreCase))
        {
            return new SafeReviewSafety(false, "Direct Storefront API browser actions cannot be auto-approved.");
        }

        if (proposal.Contains("runtime-owned", StringComparison.OrdinalIgnoreCase) ||
            proposal.Contains("runtime-business-behavior", StringComparison.OrdinalIgnoreCase))
        {
            return new SafeReviewSafety(false, "Runtime-owned behavior cannot be auto-approved as visual-only.");
        }

        if (proposal.Contains("protected-path", StringComparison.OrdinalIgnoreCase) ||
            proposal.Contains("starter-generation.contract.yaml", StringComparison.OrdinalIgnoreCase))
        {
            return new SafeReviewSafety(false, "Protected target paths cannot be auto-approved.");
        }

        if (item.ItemType.Contains("Presentation mappings", StringComparison.OrdinalIgnoreCase) &&
            (proposal.Contains("\"sourcePageId\":\"unknown\"", StringComparison.OrdinalIgnoreCase) ||
             proposal.Contains("\"sourceSectionId\":\"unknown\"", StringComparison.OrdinalIgnoreCase)))
        {
            return new SafeReviewSafety(false, "Unknown source provenance cannot be auto-approved.");
        }

        return new SafeReviewSafety(true, "safe");
    }

    private static string StableDecisionId(ReviewQueueItem item, string status)
    {
        var input = $"{item.ItemId}|{status}|{item.SourceArtifactId}|{item.SourceArtifactHash}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
        return "safe-" + hash[..16];
    }

    private sealed record SafeReviewSafety(bool Safe, string Reason);
}

public sealed record SafeReviewDecisionMaterializationSummary(
    string SchemaVersion,
    string ArtifactKind,
    string ArtifactId,
    DateTimeOffset CreatedUtc,
    string ProjectId,
    int Approved,
    int Modified,
    int Blocked,
    int Skipped,
    int Stale,
    string DecisionPath,
    string SummaryPath,
    IReadOnlyList<SafeReviewDecisionMaterializationItem> Items);

public sealed record SafeReviewDecisionMaterializationItem(
    string ItemId,
    string Status,
    string Reason);
