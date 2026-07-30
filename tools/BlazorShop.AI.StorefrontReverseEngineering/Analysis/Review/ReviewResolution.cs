using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;

public interface IReviewArtifactResolver
{
    bool CanResolve(ReviewQueueItem item);

    Task ResolveAsync(
        ReviewResolutionContext context,
        ReviewQueueItem item,
        ReviewDecision decision,
        CancellationToken cancellationToken);
}

public sealed class ReviewResolutionContext
{
    public ReviewResolutionContext(
        string projectRoot,
        FileSystemVisualArtifactStore artifactStore,
        string sourceReviewQueueId,
        string sourceReviewQueueHash,
        string decisionBundleHash)
    {
        ProjectRoot = projectRoot;
        ArtifactStore = artifactStore;
        SourceReviewQueueId = sourceReviewQueueId;
        SourceReviewQueueHash = sourceReviewQueueHash;
        DecisionBundleHash = decisionBundleHash;
    }

    public string ProjectRoot { get; }

    public FileSystemVisualArtifactStore ArtifactStore { get; }

    public string SourceReviewQueueId { get; }

    public string SourceReviewQueueHash { get; }

    public string DecisionBundleHash { get; }

    public List<ResolvedReviewItem> ResolvedItems { get; } = [];

    public List<string> ResolvedArtifacts { get; } = [];

    public List<string> BlockedItems { get; } = [];
}

public sealed record ResolvedReviewItem(
    string ItemId,
    string Family,
    string Status,
    bool Blocking,
    JsonNode OriginalValue,
    JsonNode? ReviewedValue,
    string? ReviewerNote,
    DateTimeOffset DecidedUtc);

internal sealed class SemanticTokenReviewResolver : PrefixReviewResolver
{
    public SemanticTokenReviewResolver() : base("token") { }
}

internal sealed class PresentationMappingReviewResolver : PrefixReviewResolver
{
    public PresentationMappingReviewResolver() : base("mapping") { }
}

internal sealed class EcommerceRegionReviewResolver : PrefixReviewResolver
{
    public EcommerceRegionReviewResolver() : base("region") { }
}

internal sealed class PageArchetypeReviewResolver : PrefixReviewResolver
{
    public PageArchetypeReviewResolver() : base("page") { }
}

internal sealed class PageSectionReviewResolver : PrefixReviewResolver
{
    public PageSectionReviewResolver() : base("section") { }
}

internal sealed class ComponentCandidateReviewResolver : PrefixReviewResolver
{
    public ComponentCandidateReviewResolver() : base("component") { }
}

internal sealed class UnsupportedPatternReviewResolver : PrefixReviewResolver
{
    public UnsupportedPatternReviewResolver() : base("unsupported") { }
}

internal sealed class OriginalityRestrictionReviewResolver : PrefixReviewResolver
{
    public OriginalityRestrictionReviewResolver() : base("originality") { }
}

internal abstract class PrefixReviewResolver : IReviewArtifactResolver
{
    private readonly string family;

    protected PrefixReviewResolver(string family)
    {
        this.family = family;
    }

    public bool CanResolve(ReviewQueueItem item) =>
        item.ItemId.StartsWith(family + ":", StringComparison.Ordinal);

    public Task ResolveAsync(
        ReviewResolutionContext context,
        ReviewQueueItem item,
        ReviewDecision decision,
        CancellationToken cancellationToken)
    {
        if (decision.Status == "Modified")
        {
            ValidateModifiedValue(item, decision);
        }

        var reviewedValue = decision.Status switch
        {
            "Approved" => ToNode(item.OriginalProposal),
            "Modified" => ToNode(decision.ModifiedValue!),
            "Rejected" or "Deferred" => null,
            _ => throw new InvalidOperationException($"Unknown review decision status '{decision.Status}' for '{item.ItemId}'.")
        };

        if (item.Blocking && decision.Status is "Rejected" or "Deferred")
        {
            context.BlockedItems.Add(item.ItemId);
        }

        context.ResolvedItems.Add(new ResolvedReviewItem(
            item.ItemId,
            family,
            decision.Status,
            item.Blocking,
            ToNode(item.OriginalProposal),
            reviewedValue,
            decision.ReviewerNote,
            decision.DecidedUtc));
        return Task.CompletedTask;
    }

    private static void ValidateModifiedValue(ReviewQueueItem item, ReviewDecision decision)
    {
        var modified = ToNode(decision.ModifiedValue!);
        if (modified is not JsonObject)
        {
            throw new InvalidOperationException($"Modified review decision '{item.ItemId}' must use a JSON object value.");
        }
    }

    private static JsonNode ToNode(object value) =>
        JsonSerializer.SerializeToNode(value, VisualJson.Options)
        ?? throw new InvalidOperationException("Review value could not be converted to JSON.");
}

internal sealed class ResolvedReviewArtifactWriter
{
    private static readonly IReviewArtifactResolver[] Resolvers =
    [
        new SemanticTokenReviewResolver(),
        new PresentationMappingReviewResolver(),
        new EcommerceRegionReviewResolver(),
        new PageArchetypeReviewResolver(),
        new PageSectionReviewResolver(),
        new ComponentCandidateReviewResolver(),
        new UnsupportedPatternReviewResolver(),
        new OriginalityRestrictionReviewResolver()
    ];

    private readonly string root;

    public ResolvedReviewArtifactWriter(string root)
    {
        this.root = root;
    }

    public async Task WriteAsync(
        FileSystemVisualArtifactStore store,
        ReviewQueue queue,
        ReviewDecisions decisions,
        ReviewedItems reviewed,
        CancellationToken cancellationToken)
    {
        var context = new ReviewResolutionContext(
            root,
            store,
            queue.ArtifactId,
            StableHash(queue),
            StableHash(decisions));
        var decisionsByItem = decisions.Decisions
            .GroupBy(decision => decision.ItemId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(decision => decision.DecidedUtc).Last(),
                StringComparer.Ordinal);

        foreach (var item in queue.Items)
        {
            var resolver = Resolvers.FirstOrDefault(candidate => candidate.CanResolve(item))
                ?? throw new InvalidOperationException($"Unknown review item family for '{item.ItemId}'.");
            var decision = decisionsByItem.GetValueOrDefault(item.ItemId)
                ?? new ReviewDecision(item.ItemId, "Deferred", null, "No decision recorded.", DateTimeOffset.UtcNow, "system", item.SourceArtifactId, item.SourceArtifactHash, $"missing-{item.ItemId}");
            await resolver.ResolveAsync(context, item, decision, cancellationToken);
        }

        await WriteSemanticTokensAsync(context, cancellationToken);
        await WritePageArchetypesAsync(context, cancellationToken);
        await WritePageSectionsAsync(context, cancellationToken);
        await WriteComponentCandidatesAsync(context, cancellationToken);
        await WritePresentationMappingsAsync(context, cancellationToken);
        await WriteEcommerceRegionsAsync(context, cancellationToken);
        await WriteUnsupportedPatternDecisionsAsync(context, cancellationToken);
        await WriteOriginalityRestrictionsAsync(context, cancellationToken);
        await WriteManifestAsync(context, queue.ProjectId, cancellationToken);
    }

    private async Task WriteSemanticTokensAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, "analysis", "tokens", "semantic-tokens.draft.json");
        var document = ReadObjectOrDefault(path, "semantic-tokens");
        MarkReviewed(document, "reviewed-semantic-tokens");
        ApplyArrayReviews(document, "tokens", context.ResolvedItems.Where(item => item.Family == "token"), item => item.ItemId["token:".Length..], "role");
        await WriteResolvedAsync(context, "analysis/resolved/semantic-tokens.reviewed.json", document, cancellationToken);
    }

    private async Task WritePageArchetypesAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var pages = new JsonArray(ReadPageArtifacts("page-archetype.json").Select(pair => pair.Node).ToArray<JsonNode>());
        ApplyItemReviews(pages, context.ResolvedItems.Where(item => item.Family == "page"), item => item.ItemId["page:".Length..], "pageId");
        var document = NewResolvedDocument("reviewed-page-archetypes", new JsonObject { ["pages"] = pages });
        await WriteResolvedAsync(context, "analysis/resolved/page-archetypes.reviewed.json", document, cancellationToken);
    }

    private async Task WritePageSectionsAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var pages = new JsonArray();
        foreach (var (relative, node) in ReadPageArtifacts("sections.draft.json"))
        {
            var pageId = StringValue(node, "pageId") ?? PageIdFromRelative(relative);
            var sections = new JsonArray((node["sections"]?.AsArray().Select(section => section?.DeepClone()).OfType<JsonNode>().ToArray() ?? []));
            ApplyItemReviews(sections, context.ResolvedItems.Where(item => item.Family == "section"), item => SectionId(item.ItemId), "sectionId");
            pages.Add(new JsonObject
            {
                ["pageId"] = pageId,
                ["sections"] = sections
            });
        }

        var document = NewResolvedDocument("reviewed-page-sections", new JsonObject { ["pages"] = pages });
        await WriteResolvedAsync(context, "analysis/resolved/page-sections.reviewed.json", document, cancellationToken);
    }

    private async Task WriteComponentCandidatesAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, "analysis", "components", "component-candidates.json");
        var document = ReadObjectOrDefault(path, "component-candidates");
        MarkReviewed(document, "reviewed-component-candidates");
        ApplyArrayReviews(document, "candidates", context.ResolvedItems.Where(item => item.Family == "component"), item => item.ItemId["component:".Length..], "familyId");
        await WriteResolvedAsync(context, "analysis/resolved/component-candidates.reviewed.json", document, cancellationToken);
    }

    private async Task WritePresentationMappingsAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, "analysis", "mapping", "presentation-mappings.draft.json");
        var document = ReadObjectOrDefault(path, "presentation-mappings");
        MarkReviewed(document, "reviewed-presentation-mappings");
        ApplyArrayReviews(document, "mappings", context.ResolvedItems.Where(item => item.Family == "mapping"), item => item.ItemId["mapping:".Length..], "sourceCandidateId");
        await WriteResolvedAsync(context, "analysis/resolved/presentation-mappings.reviewed.json", document, cancellationToken);
    }

    private async Task WriteEcommerceRegionsAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var pages = new JsonArray();
        foreach (var (relative, node) in ReadPageArtifacts("ecommerce-regions.json"))
        {
            var pageId = StringValue(node, "pageId") ?? PageIdFromRelative(relative);
            var regions = new JsonArray((node["regions"]?.AsArray().Select(region => region?.DeepClone()).OfType<JsonNode>().ToArray() ?? []));
            ApplyItemReviews(regions, context.ResolvedItems.Where(item => item.Family == "region"), item => RegionId(item.ItemId), "regionId");
            pages.Add(new JsonObject
            {
                ["pageId"] = pageId,
                ["regions"] = regions
            });
        }

        var document = NewResolvedDocument("reviewed-ecommerce-regions", new JsonObject { ["pages"] = pages });
        await WriteResolvedAsync(context, "analysis/resolved/ecommerce-regions.reviewed.json", document, cancellationToken);
    }

    private async Task WriteUnsupportedPatternDecisionsAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var decisions = new JsonArray(context.ResolvedItems
            .Where(item => item.Family == "unsupported")
            .Select(item => new JsonObject
            {
                ["itemId"] = item.ItemId,
                ["status"] = item.Status,
                ["reviewerNote"] = item.ReviewerNote,
                ["decidedUtc"] = item.DecidedUtc,
                ["originalValue"] = item.OriginalValue.DeepClone(),
                ["reviewedValue"] = item.ReviewedValue?.DeepClone()
            })
            .ToArray<JsonNode>());
        var document = NewResolvedDocument("unsupported-pattern-decisions", new JsonObject { ["decisions"] = decisions });
        await WriteResolvedAsync(context, "analysis/resolved/unsupported-pattern-decisions.json", document, cancellationToken);
    }

    private async Task WriteOriginalityRestrictionsAsync(ReviewResolutionContext context, CancellationToken cancellationToken)
    {
        var decisions = new JsonArray(context.ResolvedItems
            .Where(item => item.Family == "originality")
            .Select(item => new JsonObject
            {
                ["itemId"] = item.ItemId,
                ["status"] = item.Status,
                ["originalValue"] = item.OriginalValue.DeepClone(),
                ["reviewedValue"] = item.ReviewedValue?.DeepClone()
            })
            .ToArray<JsonNode>());
        var document = NewResolvedDocument("reviewed-originality-restrictions", new JsonObject { ["decisions"] = decisions });
        await WriteResolvedAsync(context, "analysis/resolved/originality-restrictions.reviewed.json", document, cancellationToken);
    }

    private async Task WriteManifestAsync(ReviewResolutionContext context, string projectId, CancellationToken cancellationToken)
    {
        var manifest = new ReviewResolutionManifest(
            "1.0",
            "review-resolution-manifest",
            $"review-resolution-{projectId}",
            DateTimeOffset.UtcNow,
            projectId,
            context.SourceReviewQueueId,
            context.SourceReviewQueueHash,
            context.DecisionBundleHash,
            context.ResolvedItems.Count(item => item.Status is "Approved" or "Modified"),
            context.BlockedItems.Count,
            context.ResolvedArtifacts.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            context.BlockedItems.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
        await context.ArtifactStore.WriteJsonAsync(ArtifactPath.Create("analysis/resolved/review-resolution-manifest.json"), "review-resolution-manifest", manifest, cancellationToken);
    }

    private async Task WriteResolvedAsync(ReviewResolutionContext context, string relativePath, JsonObject document, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, document.ToJsonString(VisualJson.Options) + Environment.NewLine, cancellationToken);
        context.ResolvedArtifacts.Add(relativePath);
    }

    private static void ApplyArrayReviews(
        JsonObject document,
        string arrayName,
        IEnumerable<ResolvedReviewItem> reviews,
        Func<ResolvedReviewItem, string> reviewKey,
        string targetKey)
    {
        var array = document[arrayName]?.AsArray();
        if (array is null)
        {
            document[arrayName] = array = [];
        }

        ApplyItemReviews(array, reviews, reviewKey, targetKey);
        document["reviewedItems"] = ReviewedItemsArray(reviews);
    }

    private static void ApplyItemReviews(
        JsonArray nodes,
        IEnumerable<ResolvedReviewItem> reviews,
        Func<ResolvedReviewItem, string> reviewKey,
        string targetKey)
    {
        foreach (var review in reviews)
        {
            var targetIndex = FindIndex(nodes, targetKey, reviewKey(review));
            var target = targetIndex >= 0 && targetIndex < nodes.Count ? nodes[targetIndex] as JsonObject : null;
            if (review.Status == "Rejected" && target is not null)
            {
                nodes.RemoveAt(targetIndex);
                continue;
            }

            if (review.Status == "Modified" && target is not null && review.ReviewedValue is JsonObject modified)
            {
                MergeObject(target, modified);
            }

            if (target is not null)
            {
                target["reviewState"] = review.Status;
                target["originalProposal"] = review.OriginalValue.DeepClone();
                target["reviewedValue"] = review.ReviewedValue?.DeepClone();
            }
        }
    }

    private static int FindIndex(JsonArray nodes, string targetKey, string targetValue)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            if (nodes[index] is JsonObject node && StringValue(node, targetKey) == targetValue)
            {
                return index;
            }
        }

        return -1;
    }

    private static JsonArray ReviewedItemsArray(IEnumerable<ResolvedReviewItem> reviews) =>
        new(reviews.Select(review => new JsonObject
        {
            ["itemId"] = review.ItemId,
            ["status"] = review.Status,
            ["originalValue"] = review.OriginalValue.DeepClone(),
            ["reviewedValue"] = review.ReviewedValue?.DeepClone(),
            ["reviewerNote"] = review.ReviewerNote,
            ["decidedUtc"] = review.DecidedUtc
        }).ToArray<JsonNode>());

    private static void MergeObject(JsonObject target, JsonObject modified)
    {
        foreach (var property in modified)
        {
            target[property.Key] = property.Value?.DeepClone();
        }
    }

    private JsonObject ReadObjectOrDefault(string path, string artifactKind)
    {
        if (File.Exists(path))
        {
            return JsonNode.Parse(File.ReadAllText(path))?.AsObject()
                ?? throw new InvalidOperationException($"Artifact is not a JSON object: {path}");
        }

        return NewResolvedDocument(artifactKind, new JsonObject());
    }

    private IEnumerable<(string Relative, JsonObject Node)> ReadPageArtifacts(string fileName)
    {
        var pagesRoot = Path.Combine(root, "analysis", "pages");
        if (!Directory.Exists(pagesRoot))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(pagesRoot, fileName, SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            yield return (
                Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'),
                JsonNode.Parse(File.ReadAllText(path))?.AsObject()
                    ?? throw new InvalidOperationException($"Artifact is not a JSON object: {path}"));
        }
    }

    private static JsonObject NewResolvedDocument(string artifactKind, JsonObject properties)
    {
        var document = new JsonObject
        {
            ["schemaVersion"] = "1.0",
            ["artifactKind"] = artifactKind,
            ["artifactId"] = artifactKind,
            ["createdUtc"] = DateTimeOffset.UtcNow
        };
        foreach (var property in properties)
        {
            document[property.Key] = property.Value?.DeepClone();
        }

        return document;
    }

    private static void MarkReviewed(JsonObject document, string artifactKind)
    {
        document["artifactKind"] = artifactKind;
        document["artifactId"] = artifactKind;
    }

    private static string StableHash(object value)
    {
        var json = JsonSerializer.Serialize(value, VisualJson.Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string? StringValue(JsonNode? node, string propertyName) =>
        node is JsonObject obj &&
        obj.TryGetPropertyValue(propertyName, out var value) &&
        value is not null &&
        value.GetValueKind() == JsonValueKind.String
            ? value.GetValue<string>()
            : null;

    private static string PageIdFromRelative(string relative)
    {
        var parts = relative.Split('/');
        var index = Array.IndexOf(parts, "pages");
        return index >= 0 && index + 1 < parts.Length ? parts[index + 1] : "unknown";
    }

    private static string SectionId(string itemId)
    {
        var parts = itemId.Split(':');
        return parts.Length >= 3 ? parts[2] : itemId;
    }

    private static string RegionId(string itemId)
    {
        var parts = itemId.Split(':');
        return parts.Length >= 3 ? parts[2] : itemId;
    }
}
