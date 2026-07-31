using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Evidence;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;

public sealed class SectionSegmenter
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public SectionSegmenter(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<IReadOnlyList<SectionsDraftDocument>> SegmentAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var snapshot = await store.ReadJsonAsync<EvidenceSnapshot>(ArtifactPath.Create("analysis/evidence-snapshot.json"), "evidence-snapshot", cancellationToken);
        var documents = snapshot.Pages.Select(page => SegmentPage(snapshot.ProjectId, page)).ToArray();
        foreach (var document in documents)
        {
            await store.WriteJsonAsync(ArtifactPath.Create($"analysis/pages/{document.PageId}/sections.draft.json"), "sections", document, cancellationToken);
        }

        return documents;
    }

    private static SectionsDraftDocument SegmentPage(string projectId, EvidenceSnapshotPage page)
    {
        var issues = new List<SectionSegmentationIssue>();
        var viewportSections = page.Viewports
            .Select(viewport => new
            {
                Viewport = viewport,
                Sections = SegmentViewport(viewport)
            })
            .ToArray();
        var primary = viewportSections
            .OrderByDescending(candidate => candidate.Viewport.ViewportWidth)
            .First();
        var sections = primary.Sections
            .Select(section =>
            {
                var viewportBounds = viewportSections
                    .Select(candidate => new
                    {
                        candidate.Viewport.ViewportId,
                        Match = candidate.Sections.FirstOrDefault(other =>
                            string.Equals(other.CrossViewportIdentityKey, section.CrossViewportIdentityKey, StringComparison.Ordinal) ||
                            other.Order == section.Order && string.Equals(other.SectionType, section.SectionType, StringComparison.Ordinal))
                    })
                    .Where(candidate => candidate.Match is not null)
                    .ToDictionary(candidate => candidate.ViewportId, candidate => candidate.Match!.Bounds, StringComparer.Ordinal);
                return section with { ViewportBoundingBoxes = viewportBounds };
            })
            .OrderBy(section => section.Bounds.Y)
            .ThenBy(section => section.Bounds.X)
            .Select((section, index) => section with { Order = index + 1, SectionId = $"section-{index + 1:00}" })
            .ToList();
        DetectOverlapAndAmbiguity(sections, issues);
        if (sections.Count == 0)
        {
            issues.Add(new SectionSegmentationIssue("missing-section-evidence", "blocking", "No boxed evidence was available for section segmentation.", []));
        }

        return new SectionsDraftDocument(
            "1.0",
            "sections",
            $"sections-{projectId}-{page.PageId}",
            DateTimeOffset.UtcNow,
            projectId,
            page.PageId,
            sections,
            issues);
    }

    private static List<SectionDraft> SegmentViewport(EvidenceSnapshotViewport viewport)
    {
        var elements = viewport.Elements
            .Where(element => element.Box is { Width: > 0, Height: > 0 })
            .OrderBy(element => element.Box!.Y)
            .ThenBy(element => element.Box!.X)
            .ToArray();
        var sections = new List<SectionDraft>();
        var productCards = elements.Where(element => Classify(element) == "product grid").ToArray();
        if (productCards.Length >= 3)
        {
            sections.Add(CreateSection("product grid", sections.Count + 1, productCards, "repeated-card-group", 0.76m));
        }

        foreach (var element in elements.Where(element => !productCards.Contains(element)))
        {
            var sectionType = Classify(element);
            sections.Add(CreateSection(sectionType, sections.Count + 1, [element], ReasonFor(sectionType, element), sectionType == "unknown section" ? 0.38m : 0.66m));
        }

        sections = sections
            .OrderBy(section => section.Bounds.Y)
            .ThenBy(section => section.Bounds.X)
            .Select((section, index) => section with { Order = index + 1, SectionId = $"section-{index + 1:00}" })
            .ToList();
        return sections;
    }

    private static SectionDraft CreateSection(
        string sectionType,
        int order,
        IReadOnlyList<EvidenceSnapshotElement> elements,
        string reasonCode,
        decimal confidence)
    {
        var bounds = Union(elements.Select(element => element.Box!).ToArray());
        return new SectionDraft(
            $"section-{order:00}",
            sectionType,
            order,
            confidence,
            bounds,
            ParentSectionId: null,
            ChildSectionIds: [],
            CrossViewportIdentityKey: $"{sectionType.Replace(' ', '-')}-{order:00}",
            elements.Select(element => element.EvidenceId).Distinct(StringComparer.Ordinal).ToArray(),
            [reasonCode]);
    }

    private static string Classify(EvidenceSnapshotElement element)
    {
        var selector = element.Selector.ToLowerInvariant();
        var text = (element.TextSnippet ?? "").ToLowerInvariant();
        if (selector.Contains("cookie", StringComparison.Ordinal) || selector.Contains("banner", StringComparison.Ordinal) && selector.Contains("overlay", StringComparison.Ordinal))
        {
            return "cookie/banner overlay";
        }

        if (selector.Contains("announcement", StringComparison.Ordinal)) return "announcement bar";
        if (selector.Contains("header", StringComparison.Ordinal)) return "header";
        if (selector.Contains("nav", StringComparison.Ordinal)) return "navigation";
        if (selector.Contains("hero", StringComparison.Ordinal)) return "hero";
        if (selector.Contains("promo", StringComparison.Ordinal) || text.Contains("sale", StringComparison.Ordinal)) return "promotional banner";
        if (selector.Contains("category", StringComparison.Ordinal)) return "category navigation";
        if (element.Category == "product-card-candidate") return "product grid";
        if (selector.Contains("carousel", StringComparison.Ordinal)) return "product carousel";
        if (selector.Contains("featured", StringComparison.Ordinal)) return "featured product";
        if (selector.Contains("gallery", StringComparison.Ordinal)) return "product gallery";
        if (selector.Contains("product-info", StringComparison.Ordinal) || selector.Contains("product-title", StringComparison.Ordinal)) return "product information";
        if (selector.Contains("add-to-cart", StringComparison.Ordinal) || text.Contains("add to cart", StringComparison.Ordinal)) return "purchase actions";
        if (selector.Contains("trust", StringComparison.Ordinal) || text.Contains("free shipping", StringComparison.Ordinal)) return "trust/benefit strip";
        if (element.Category == "article" || selector.Contains("editorial", StringComparison.Ordinal)) return "editorial/content block";
        if (selector.Contains("newsletter", StringComparison.Ordinal)) return "newsletter";
        if (selector.Contains("review", StringComparison.Ordinal) || selector.Contains("testimonial", StringComparison.Ordinal)) return "reviews/testimonials";
        if (selector.Contains("faq", StringComparison.Ordinal) || selector.Contains("accordion", StringComparison.Ordinal)) return "FAQ/accordion";
        if (selector.Contains("upsell", StringComparison.Ordinal) || selector.Contains("cross-sell", StringComparison.Ordinal)) return "cross-sell/upsell region";
        if (selector.Contains("footer", StringComparison.Ordinal)) return "footer";
        return "unknown section";
    }

    private static string ReasonFor(string sectionType, EvidenceSnapshotElement element)
    {
        if (sectionType == "unknown section")
        {
            return "unsupported-section-signal";
        }

        if (element.StyleGroups.TryGetValue("positioning", out var positioning) &&
            positioning.TryGetValue("position", out var position) &&
            position is "sticky" or "fixed")
        {
            return "sticky-fixed-region-signal";
        }

        if (element.StyleGroups.TryGetValue("color", out var colors) && colors.ContainsKey("background-color"))
        {
            return "background-change-boundary";
        }

        if (element.StyleGroups.TryGetValue("layout", out var layout) && layout.TryGetValue("display", out var display) && display is "grid" or "flex")
        {
            return "grid-flex-transition";
        }

        return "selector-heading-boundary";
    }

    private static void DetectOverlapAndAmbiguity(IReadOnlyList<SectionDraft> sections, List<SectionSegmentationIssue> issues)
    {
        for (var index = 0; index < sections.Count - 1; index++)
        {
            var current = sections[index];
            var next = sections[index + 1];
            var overlap = current.Bounds.Y + current.Bounds.Height - next.Bounds.Y;
            if (overlap > Math.Min(current.Bounds.Height, next.Bounds.Height) * 0.50m)
            {
                issues.Add(new SectionSegmentationIssue("invalid-peer-overlap", "blocking", $"Peer sections '{current.SectionId}' and '{next.SectionId}' overlap too much.", current.EvidenceIds.Concat(next.EvidenceIds).ToArray()));
            }
            else if (overlap > 0 || next.Bounds.Y - (current.Bounds.Y + current.Bounds.Height) < 8)
            {
                issues.Add(new SectionSegmentationIssue("merge-split-ambiguity", "warning", $"Sections '{current.SectionId}' and '{next.SectionId}' have an ambiguous boundary.", current.EvidenceIds.Concat(next.EvidenceIds).ToArray()));
            }
        }
    }

    private static SectionBounds Union(IReadOnlyList<ElementBox> boxes)
    {
        var x = boxes.Min(box => box.X);
        var y = boxes.Min(box => box.Y);
        var right = boxes.Max(box => box.X + box.Width);
        var bottom = boxes.Max(box => box.Y + box.Height);
        return new SectionBounds(x, y, right - x, bottom - y);
    }
}
