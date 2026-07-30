using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;

public sealed class PageArchetypeClassifier
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public PageArchetypeClassifier(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<IReadOnlyList<PageArchetypeDocument>> ClassifyAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var snapshot = await store.ReadJsonAsync<EvidenceSnapshot>(
            ArtifactPath.Create("analysis/evidence-snapshot.json"),
            "evidence-snapshot",
            cancellationToken);
        var documents = snapshot.Pages.Select(page => ClassifyPage(snapshot.ProjectId, page)).ToArray();
        foreach (var document in documents)
        {
            await store.WriteJsonAsync(
                ArtifactPath.Create($"analysis/pages/{document.PageId}/page-archetype.json"),
                "page-archetype",
                document,
                cancellationToken);
        }

        return documents;
    }

    private static PageArchetypeDocument ClassifyPage(string projectId, EvidenceSnapshotPage page)
    {
        var scores = new Dictionary<string, Score>(StringComparer.Ordinal)
        {
            ["home"] = new(),
            ["product-listing"] = new(),
            ["search-results"] = new(),
            ["product-detail"] = new(),
            ["cart-shell"] = new(),
            ["checkout-shell"] = new(),
            ["account-auth-shell"] = new(),
            ["content"] = new()
        };
        var elements = page.Viewports.SelectMany(viewport => viewport.Elements).ToArray();
        var text = string.Join(' ', elements.Select(element => element.TextSnippet).Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();
        var selectors = string.Join(' ', elements.Select(element => element.Selector)).ToLowerInvariant();
        var url = page.Url.ToLowerInvariant();

        AddRouteScores(url, page.PageId, scores);
        AddTextScore(scores, text, "search-results", ["search results", "results for"], "search-text", 0.35m, elements);
        AddTextScore(scores, text, "product-detail", ["add to cart", "buy now", "product details"], "pdp-text", 0.45m, elements);
        AddTextScore(scores, text, "cart-shell", ["cart", "subtotal", "order summary"], "cart-text", 0.45m, elements);
        AddTextScore(scores, text, "checkout-shell", ["checkout", "shipping", "payment"], "checkout-text", 0.45m, elements);
        AddTextScore(scores, text, "account-auth-shell", ["login", "sign in", "register", "account"], "auth-text", 0.45m, elements);
        if (elements.Count(element => element.Category == "product-card-candidate") >= 3)
        {
            Add(scores, "product-listing", 0.45m, "repeated-product-card-signals", elements.Where(element => element.Category == "product-card-candidate").Select(element => element.EvidenceId));
        }

        if (selectors.Contains("gallery", StringComparison.Ordinal) &&
            (text.Contains("add to cart", StringComparison.Ordinal) || text.Contains("$", StringComparison.Ordinal)))
        {
            Add(scores, "product-detail", 0.40m, "gallery-price-cart-signals", elements.Select(element => element.EvidenceId));
        }

        if (selectors.Contains("form", StringComparison.Ordinal) || selectors.Contains("input", StringComparison.Ordinal))
        {
            Add(scores, text.Contains("checkout", StringComparison.Ordinal) ? "checkout-shell" : "account-auth-shell", 0.25m, "form-density-signal", elements.Select(element => element.EvidenceId));
        }

        if (url.EndsWith("/", StringComparison.Ordinal) || page.PageId == "home")
        {
            Add(scores, "home", 0.50m, "home-route-signal", elements.Select(element => element.EvidenceId));
        }

        if (elements.Any(element => element.Category == "semantic-landmark") &&
            elements.Any(element => element.Category == "section"))
        {
            Add(scores, "home", 0.20m, "landmark-section-home-signal", elements.Select(element => element.EvidenceId));
        }

        if (elements.Any(element => element.Category == "article"))
        {
            Add(scores, "content", 0.45m, "article-signal", elements.Select(element => element.EvidenceId));
        }

        var ranked = scores
            .Select(pair => new PageArchetypeCandidate(
                pair.Key,
                Math.Min(0.99m, pair.Value.Value),
                pair.Value.EvidenceIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                pair.Value.Reasons.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()))
            .OrderByDescending(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.Archetype, StringComparer.Ordinal)
            .ToArray();
        var primary = ranked.FirstOrDefault();
        var archetype = primary is not null && primary.Confidence >= 0.50m ? primary.Archetype : "unknown";
        var confidence = primary is not null && primary.Confidence >= 0.50m ? primary.Confidence : 0.30m;
        var evidenceIds = primary?.EvidenceIds ?? [];
        var reasons = archetype == "unknown" ? ["below-confidence-threshold"] : primary?.ReasonCodes ?? [];

        return new PageArchetypeDocument(
            "1.0",
            "page-archetype",
            $"page-archetype-{projectId}-{page.PageId}",
            DateTimeOffset.UtcNow,
            projectId,
            page.PageId,
            archetype,
            confidence,
            evidenceIds,
            reasons,
            ranked.Where(candidate => candidate.Archetype != archetype && candidate.Confidence > 0).Take(4).ToArray());
    }

    private static void AddRouteScores(string url, string pageId, Dictionary<string, Score> scores)
    {
        if (url.Contains("search", StringComparison.Ordinal) || pageId.Contains("search", StringComparison.OrdinalIgnoreCase))
        {
            Add(scores, "search-results", 0.60m, "search-route-signal", []);
        }
        else if (url.Contains("product/", StringComparison.Ordinal) || pageId.Contains("pdp", StringComparison.OrdinalIgnoreCase))
        {
            Add(scores, "product-detail", 0.55m, "product-detail-route-signal", []);
        }
        else if (url.Contains("collections", StringComparison.Ordinal) || url.Contains("category", StringComparison.Ordinal) || pageId.Contains("plp", StringComparison.OrdinalIgnoreCase))
        {
            Add(scores, "product-listing", 0.55m, "listing-route-signal", []);
        }
        else if (url.Contains("cart", StringComparison.Ordinal))
        {
            Add(scores, "cart-shell", 0.60m, "cart-route-signal", []);
        }
        else if (url.Contains("checkout", StringComparison.Ordinal))
        {
            Add(scores, "checkout-shell", 0.60m, "checkout-route-signal", []);
        }
        else if (url.Contains("login", StringComparison.Ordinal) || url.Contains("account", StringComparison.Ordinal))
        {
            Add(scores, "account-auth-shell", 0.60m, "auth-route-signal", []);
        }
    }

    private static void AddTextScore(
        Dictionary<string, Score> scores,
        string text,
        string archetype,
        IReadOnlyList<string> needles,
        string reason,
        decimal value,
        IReadOnlyList<EvidenceSnapshotElement> elements)
    {
        if (needles.Any(needle => text.Contains(needle, StringComparison.Ordinal)))
        {
            Add(scores, archetype, value, reason, elements.Where(element => needles.Any(needle => (element.TextSnippet ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase))).Select(element => element.EvidenceId));
        }
    }

    private static void Add(Dictionary<string, Score> scores, string archetype, decimal value, string reason, IEnumerable<string> evidenceIds)
    {
        var score = scores[archetype];
        score.Value += value;
        score.Reasons.Add(reason);
        score.EvidenceIds.AddRange(evidenceIds);
    }

    private sealed class Score
    {
        public decimal Value { get; set; }

        public List<string> Reasons { get; } = [];

        public List<string> EvidenceIds { get; } = [];
    }
}
