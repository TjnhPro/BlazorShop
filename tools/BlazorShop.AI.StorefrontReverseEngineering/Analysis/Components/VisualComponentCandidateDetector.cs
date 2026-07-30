using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;

public sealed class VisualComponentCandidateDetector
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public VisualComponentCandidateDetector(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<(ComponentCandidatesDocument Candidates, ComponentInstancesDocument Instances)> DetectAsync(
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var snapshot = await store.ReadJsonAsync<EvidenceSnapshot>(ArtifactPath.Create("analysis/evidence-snapshot.json"), "evidence-snapshot", cancellationToken);
        var semantic = await store.ReadJsonAsync<SemanticTokenDocument>(ArtifactPath.Create("analysis/tokens/semantic-tokens.draft.json"), "semantic-tokens", cancellationToken);
        var responsiveRefs = await ReadResponsiveRefsAsync(store, snapshot, cancellationToken);
        var interactionRefs = await ReadInteractionRefsAsync(store, snapshot, cancellationToken);
        var instances = new List<VisualComponentInstance>();
        var candidates = new List<VisualComponentCandidate>();
        var groups = snapshot.Pages
            .SelectMany(page => page.Viewports.SelectMany(viewport => viewport.Elements.Select(element => (page.PageId, viewport.ViewportId, Element: element))))
            .GroupBy(item => FamilyFor(item.Element), StringComparer.Ordinal)
            .Where(group => group.Key != "unknown")
            .ToArray();

        foreach (var group in groups)
        {
            var familyId = $"family-{group.Key.Replace(' ', '-')}";
            var variantId = $"{familyId}-default";
            var groupInstances = group.Select((item, index) => new VisualComponentInstance(
                $"instance-{familyId}-{index + 1:000}",
                familyId,
                variantId,
                item.PageId,
                item.ViewportId,
                item.Element.Selector,
                [item.Element.EvidenceId])).ToArray();
            instances.AddRange(groupInstances);
            var evidenceIds = group.Select(item => item.Element.EvidenceId).Distinct(StringComparer.Ordinal).ToArray();
            candidates.Add(new VisualComponentCandidate(
                familyId,
                group.Key,
                variantId,
                ConfidenceFor(group.Key, group.Count()),
                groupInstances.Select(instance => instance.InstanceId).ToArray(),
                DetectSlots(group.Select(item => item.Element).ToArray()),
                semantic.Tokens.Where(token => token.EvidenceIds.Any(evidenceIds.Contains)).Select(token => token.Role).Distinct(StringComparer.Ordinal).ToArray(),
                semantic.PageLocalOverrides.Where(overrideItem => overrideItem.EvidenceIds.Any(evidenceIds.Contains)).Select(overrideItem => overrideItem.Role).ToArray(),
                responsiveRefs.Where(refItem => refItem.EvidenceIds.Any(evidenceIds.Contains)).Select(refItem => refItem.RefId).Distinct(StringComparer.Ordinal).ToArray(),
                interactionRefs.Where(refItem => refItem.EvidenceIds.Any(evidenceIds.Contains)).Select(refItem => refItem.RefId).Distinct(StringComparer.Ordinal).ToArray(),
                AlternativesFor(group.Key),
                group.Count() < 2 && group.Key is "product card" or "testimonial card",
                evidenceIds));
        }

        var candidateDocument = new ComponentCandidatesDocument(
            "1.0",
            "component-candidates",
            $"component-candidates-{snapshot.ProjectId}",
            DateTimeOffset.UtcNow,
            snapshot.ProjectId,
            candidates.OrderBy(candidate => candidate.FamilyId, StringComparer.Ordinal).ToArray(),
            []);
        var instanceDocument = new ComponentInstancesDocument(
            "1.0",
            "component-instances",
            $"component-instances-{snapshot.ProjectId}",
            DateTimeOffset.UtcNow,
            snapshot.ProjectId,
            instances.OrderBy(instance => instance.InstanceId, StringComparer.Ordinal).ToArray());
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/components/component-candidates.json"), "component-candidates", candidateDocument, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/components/component-instances.json"), "component-instances", instanceDocument, cancellationToken);
        return (candidateDocument, instanceDocument);
    }

    private static string FamilyFor(EvidenceSnapshotElement element)
    {
        var selector = element.Selector.ToLowerInvariant();
        var text = (element.TextSnippet ?? "").ToLowerInvariant();
        if (selector.Contains("announcement", StringComparison.Ordinal)) return "announcement bar";
        if (selector.Contains("header", StringComparison.Ordinal)) return "header";
        if (selector.Contains("nav", StringComparison.Ordinal)) return "navigation";
        if (element.Category == "product-card-candidate" || selector.Contains("product-card", StringComparison.Ordinal)) return "product card";
        if (selector.Contains("search", StringComparison.Ordinal)) return "search trigger";
        if (selector.Contains("account", StringComparison.Ordinal)) return "account trigger";
        if (selector.Contains("cart", StringComparison.Ordinal)) return "cart trigger";
        if (selector.Contains("footer", StringComparison.Ordinal)) return "footer";
        if (selector.Contains("product-grid", StringComparison.Ordinal)) return "product grid";
        if (selector.Contains("carousel", StringComparison.Ordinal)) return "product carousel";
        if (selector.Contains("price", StringComparison.Ordinal) || text.Contains("$", StringComparison.Ordinal)) return "price display";
        if (selector.Contains("badge", StringComparison.Ordinal)) return "product badge";
        if (selector.Contains("image", StringComparison.Ordinal) || selector.Contains("img", StringComparison.Ordinal)) return "product image";
        if (selector.Contains("gallery", StringComparison.Ordinal)) return "product gallery";
        if (selector.Contains("variant", StringComparison.Ordinal)) return "variant selector visual";
        if (selector.Contains("quantity", StringComparison.Ordinal)) return "quantity selector visual";
        if (selector.Contains("add-to-cart", StringComparison.Ordinal) || text.Contains("add to cart", StringComparison.Ordinal)) return "purchase action visual";
        if (selector.Contains("rating", StringComparison.Ordinal) || selector.Contains("review", StringComparison.Ordinal)) return "rating/review card";
        if (selector.Contains("breadcrumb", StringComparison.Ordinal) || selector.Contains("category", StringComparison.Ordinal)) return "breadcrumb/category card";
        if (selector.Contains("filter", StringComparison.Ordinal)) return "filter trigger/panel";
        if (selector.Contains("sort", StringComparison.Ordinal)) return "sort selector";
        if (selector.Contains("pagination", StringComparison.Ordinal)) return "pagination";
        if (selector.Contains("line-item", StringComparison.Ordinal)) return "cart line visual";
        if (selector.Contains("order-summary", StringComparison.Ordinal)) return "order summary visual";
        if (selector.Contains("hero", StringComparison.Ordinal)) return "hero";
        if (selector.Contains("promo", StringComparison.Ordinal)) return "promo banner";
        if (selector.Contains("feature", StringComparison.Ordinal)) return "feature list";
        if (selector.Contains("media-text", StringComparison.Ordinal)) return "media/text split";
        if (selector.Contains("newsletter", StringComparison.Ordinal)) return "newsletter visual";
        if (selector.Contains("faq", StringComparison.Ordinal)) return "FAQ item";
        if (selector.Contains("testimonial", StringComparison.Ordinal)) return "testimonial card";
        return "unknown";
    }

    private static IReadOnlyList<ComponentSlot> DetectSlots(IReadOnlyList<EvidenceSnapshotElement> elements)
    {
        var slots = new List<ComponentSlot>();
        AddSlot("image", "media", elements.Where(element => element.Selector.Contains("image", StringComparison.OrdinalIgnoreCase) || element.Selector.Contains("img", StringComparison.OrdinalIgnoreCase)));
        AddSlot("title", "text", elements.Where(element => element.Selector.Contains("title", StringComparison.OrdinalIgnoreCase) || element.Category == "heading"));
        AddSlot("price", "text", elements.Where(element => element.Selector.Contains("price", StringComparison.OrdinalIgnoreCase) || (element.TextSnippet ?? "").Contains("$", StringComparison.Ordinal)));
        AddSlot("action", "command-visual", elements.Where(element => element.Selector.Contains("button", StringComparison.OrdinalIgnoreCase) || (element.TextSnippet ?? "").Contains("add to cart", StringComparison.OrdinalIgnoreCase)));
        if (slots.Count == 0)
        {
            AddSlot("root", "container", elements);
        }

        return slots;

        void AddSlot(string name, string kind, IEnumerable<EvidenceSnapshotElement> source)
        {
            var ids = source.Select(element => element.EvidenceId).Distinct(StringComparer.Ordinal).ToArray();
            if (ids.Length > 0)
            {
                slots.Add(new ComponentSlot(name, kind, ids, 0.64m));
            }
        }
    }

    private static decimal ConfidenceFor(string family, int count) =>
        count >= 3 ? 0.78m : count == 2 ? 0.66m : family.Contains("product", StringComparison.Ordinal) ? 0.54m : 0.60m;

    private static IReadOnlyList<string> AlternativesFor(string family) =>
        family switch
        {
            "product card" => ["content card"],
            "product grid" => ["product carousel"],
            "purchase action visual" => ["presentation button"],
            _ => []
        };

    private static async Task<IReadOnlyList<(string RefId, IReadOnlyList<string> EvidenceIds)>> ReadResponsiveRefsAsync(
        FileSystemVisualArtifactStore store,
        EvidenceSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var refs = new List<(string, IReadOnlyList<string>)>();
        foreach (var page in snapshot.Pages)
        {
            var path = $"analysis/pages/{page.PageId}/responsive-behavior.json";
            try
            {
                var responsive = await store.ReadJsonAsync<ResponsiveBehaviorDocument>(ArtifactPath.Create(path), "responsive-behavior", cancellationToken);
                refs.AddRange(responsive.Sections.Select(section => ($"{page.PageId}:{section.CrossViewportIdentityKey}", section.EvidenceIds)));
            }
            catch (Exception)
            {
                // Component detection can still run without responsive references in isolated tests.
            }
        }

        return refs;
    }

    private static async Task<IReadOnlyList<(string RefId, IReadOnlyList<string> EvidenceIds)>> ReadInteractionRefsAsync(
        FileSystemVisualArtifactStore store,
        EvidenceSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var refs = new List<(string, IReadOnlyList<string>)>();
        foreach (var page in snapshot.Pages)
        {
            var path = $"analysis/pages/{page.PageId}/interaction-model.json";
            try
            {
                var interaction = await store.ReadJsonAsync<InteractionModelDocument>(ArtifactPath.Create(path), "interaction-model", cancellationToken);
                refs.AddRange(interaction.Interactions.Select(item => ($"{page.PageId}:{item.StateName}", item.EvidenceIds)));
            }
            catch (Exception)
            {
                // Component detection can still run without interaction references in isolated tests.
            }
        }

        return refs;
    }
}
