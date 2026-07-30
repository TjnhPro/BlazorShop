using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Aggregation;
using BlazorShop.AI.StorefrontReverseEngineering.Interactions;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;

public sealed class ResponsiveInteractionAnalyzer
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public ResponsiveInteractionAnalyzer(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<IReadOnlyList<(ResponsiveBehaviorDocument Responsive, InteractionModelDocument Interaction)>> AnalyzeAsync(
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var snapshot = await store.ReadJsonAsync<EvidenceSnapshot>(ArtifactPath.Create("analysis/evidence-snapshot.json"), "evidence-snapshot", cancellationToken);
        var results = new List<(ResponsiveBehaviorDocument, InteractionModelDocument)>();
        foreach (var page in snapshot.Pages)
        {
            var responsive = BuildResponsive(snapshot.ProjectId, page);
            var interaction = await BuildInteractionAsync(store, snapshot.ProjectId, page, snapshot.SourceArtifactPaths, cancellationToken);
            await store.WriteJsonAsync(ArtifactPath.Create($"analysis/pages/{page.PageId}/responsive-behavior.json"), "responsive-behavior", responsive, cancellationToken);
            await store.WriteJsonAsync(ArtifactPath.Create($"analysis/pages/{page.PageId}/interaction-model.json"), "interaction-model", interaction, cancellationToken);
            results.Add((responsive, interaction));
        }

        return results;
    }

    private static ResponsiveBehaviorDocument BuildResponsive(string projectId, EvidenceSnapshotPage page)
    {
        var issues = new List<ResponsiveBehaviorIssue>();
        var sectionGroups = page.Viewports
            .SelectMany(viewport => viewport.Elements.Select(element => (Viewport: viewport, Element: element, Key: IdentityKey(element))))
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .ToArray();
        var sections = sectionGroups.Select(group =>
        {
            var observations = group
                .OrderBy(item => item.Viewport.ViewportWidth)
                .Select(item => BuildObservation(item.Viewport.ViewportId, item.Element, item.Viewport.Assets.Count(asset => asset.SourceElement == item.Element.Selector)))
                .ToArray();
            return new ResponsiveSectionBehavior(
                group.Key,
                observations,
                DetectFlags(observations),
                group.Select(item => item.Element.EvidenceId).Distinct(StringComparer.Ordinal).ToArray());
        }).ToArray();
        if (sections.Any(section => section.CrossViewportIdentityKey == "navigation") &&
            sections.Any(section => section.CrossViewportIdentityKey == "navigation-mobile-menu"))
        {
            sections = sections
                .Select(section => section.CrossViewportIdentityKey == "navigation"
                    ? section with { BehaviorFlags = section.BehaviorFlags.Concat(["desktop-navigation-to-mobile-menu-replacement"]).Distinct(StringComparer.Ordinal).ToArray() }
                    : section)
                .ToArray();
        }

        foreach (var section in sections.Where(section => section.Viewports.Any(viewport => viewport.Width > 0 && viewport.Width > viewport.Height * 3)))
        {
            issues.Add(new ResponsiveBehaviorIssue("horizontal-overflow-or-carousel", "warning", $"Section '{section.CrossViewportIdentityKey}' may overflow horizontally.", section.EvidenceIds));
        }

        return new ResponsiveBehaviorDocument(
            "1.0",
            "responsive-behavior",
            $"responsive-{projectId}-{page.PageId}",
            DateTimeOffset.UtcNow,
            projectId,
            page.PageId,
            sections,
            ["between-observed-viewports"],
            issues);
    }

    private static async Task<InteractionModelDocument> BuildInteractionAsync(
        FileSystemVisualArtifactStore store,
        string projectId,
        EvidenceSnapshotPage page,
        IReadOnlyList<string> sourceArtifactPaths,
        CancellationToken cancellationToken)
    {
        var interactions = new List<InteractionPattern>();
        var issues = new List<ResponsiveBehaviorIssue>();
        foreach (var path in sourceArtifactPaths.Where(path => path.Contains($"/{page.PageId}/", StringComparison.Ordinal) && path.EndsWith("interaction-evidence.json", StringComparison.Ordinal)))
        {
            InteractionEvidence evidence;
            try
            {
                evidence = await store.ReadJsonAsync<InteractionEvidence>(ArtifactPath.Create(path), "interaction-evidence", cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                issues.Add(new ResponsiveBehaviorIssue("invalid-interaction-evidence", "blocking", exception.Message, []));
                continue;
            }

            var classification = ClassifyInteraction(evidence);
            interactions.Add(new InteractionPattern(
                evidence.StateName,
                evidence.InteractionModel.ToString(),
                classification,
                evidence.ChangedElementEvidenceIds,
                ReasonCodes(evidence, classification),
                evidence.BeforeStylesPath,
                evidence.AfterStylesPath));
        }

        return new InteractionModelDocument(
            "1.0",
            "interaction-model",
            $"interaction-model-{projectId}-{page.PageId}",
            DateTimeOffset.UtcNow,
            projectId,
            page.PageId,
            interactions,
            issues);
    }

    private static ResponsiveViewportObservation BuildObservation(string viewportId, EvidenceSnapshotElement element, int assetCount)
    {
        var styles = element.StyleGroups.Values.SelectMany(group => group).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        styles.TryGetValue("display", out var display);
        styles.TryGetValue("visibility", out var visibility);
        styles.TryGetValue("position", out var position);
        styles.TryGetValue("gap", out var gap);
        styles.TryGetValue("font-size", out var fontSize);
        return new ResponsiveViewportObservation(
            viewportId,
            element.Box?.X,
            element.Box?.Y,
            element.Box?.Width,
            element.Box?.Height,
            display,
            visibility,
            position,
            gap,
            fontSize,
            assetCount);
    }

    private static IReadOnlyList<string> DetectFlags(IReadOnlyList<ResponsiveViewportObservation> observations)
    {
        var flags = new List<string>();
        if (observations.Any(observation => observation.Display == "none" || observation.Visibility == "hidden"))
        {
            flags.Add("hidden-on-mobile");
        }

        if (observations.Select(observation => observation.Display).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            flags.Add("replacement-or-restyle");
        }

        if (observations.Any(observation => observation.Display == "grid") && observations.Any(observation => observation.Display is "block" or "flex"))
        {
            flags.Add("multi-column-to-stacked");
        }

        if (observations.Select(observation => observation.Gap).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            flags.Add("compact-spacing");
        }

        if (observations.Select(observation => observation.FontSize).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            flags.Add("typography-downscale");
        }

        if (observations.Select(observation => observation.AssetCount).Distinct().Count() > 1)
        {
            flags.Add("image-crop-or-asset-change");
        }

        return flags;
    }

    private static string IdentityKey(EvidenceSnapshotElement element)
    {
        var selector = element.Selector.ToLowerInvariant();
        if (selector.Contains("mobile-menu", StringComparison.Ordinal)) return "navigation-mobile-menu";
        if (selector.Contains("nav", StringComparison.Ordinal)) return "navigation";
        if (selector.Contains("product-card", StringComparison.Ordinal)) return "product-card";
        return selector.Split(':')[0];
    }

    private static string ClassifyInteraction(InteractionEvidence evidence)
    {
        var name = evidence.StateName.ToLowerInvariant();
        if (name.Contains("checkout", StringComparison.Ordinal) || name.Contains("payment", StringComparison.Ordinal) || name.Contains("delete", StringComparison.Ordinal))
        {
            return "unsupported/unsafe";
        }

        if (name.Contains("cart", StringComparison.Ordinal) || name.Contains("quantity", StringComparison.Ordinal) || name.Contains("option", StringComparison.Ordinal))
        {
            return "business behavior required";
        }

        return evidence.InteractionModel is InteractionModel.HoverDriven or InteractionModel.TimeDriven
            ? "visual-only"
            : "presentation interaction";
    }

    private static IReadOnlyList<string> ReasonCodes(InteractionEvidence evidence, string classification)
    {
        var reasons = new List<string> { "before-after-interaction-evidence" };
        if (evidence.StateName.Contains("menu", StringComparison.OrdinalIgnoreCase)) reasons.Add("mobile-menu");
        if (evidence.StateName.Contains("accordion", StringComparison.OrdinalIgnoreCase)) reasons.Add("accordion");
        if (evidence.StateName.Contains("tab", StringComparison.OrdinalIgnoreCase)) reasons.Add("tabs");
        if (evidence.StateName.Contains("carousel", StringComparison.OrdinalIgnoreCase)) reasons.Add("carousel-navigation");
        if (evidence.StateName.Contains("modal", StringComparison.OrdinalIgnoreCase) || evidence.StateName.Contains("drawer", StringComparison.OrdinalIgnoreCase)) reasons.Add("modal-drawer");
        if (evidence.StateName.Contains("sticky", StringComparison.OrdinalIgnoreCase)) reasons.Add("sticky-transition");
        if (evidence.StateName.Contains("focus", StringComparison.OrdinalIgnoreCase)) reasons.Add("focus-state");
        if (evidence.StateName.Contains("quantity", StringComparison.OrdinalIgnoreCase)) reasons.Add("quantity-select-visual-pattern");
        if (evidence.StateName.Contains("option", StringComparison.OrdinalIgnoreCase)) reasons.Add("product-option-selector-visual-pattern");
        reasons.Add(classification);
        return reasons;
    }
}
