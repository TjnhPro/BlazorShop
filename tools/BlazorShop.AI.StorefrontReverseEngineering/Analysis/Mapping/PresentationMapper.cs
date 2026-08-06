using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;

public sealed class PresentationMapper
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public PresentationMapper(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<PresentationMappingsDocument> MapAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var components = await store.ReadJsonAsync<ComponentCandidatesDocument>(ArtifactPath.Create("analysis/components/component-candidates.json"), "component-candidates", cancellationToken);
        var catalog = await store.ReadJsonAsync<PresentationComponentCatalog>(ArtifactPath.Create("presentation-catalog/presentation-component-catalog.json"), "presentation-component-catalog", cancellationToken);
        var semantic = await store.ReadJsonAsync<SemanticTokenDocument>(ArtifactPath.Create("analysis/tokens/semantic-tokens.draft.json"), "semantic-tokens", cancellationToken);
        var ecommerce = await ReadRegionsAsync(root, store, cancellationToken);
        var sections = await ReadSectionsAsync(root, store, cancellationToken);
        var pageArchetypes = await ReadPageArchetypesAsync(root, store, cancellationToken);
        var mappings = new List<PresentationMapping>();
        var unsupported = new List<UnsupportedPattern>();
        foreach (var candidate in components.Candidates)
        {
            var regionContexts = ecommerce
                .Where(region => region.Region.SourceComponentFamilyIds.Contains(candidate.FamilyId))
                .OrderBy(region => region.PageId, StringComparer.Ordinal)
                .ThenBy(region => region.Region.RegionId, StringComparer.Ordinal)
                .ToArray();
            var sectionContexts = sections
                .Where(section => section.Section.EvidenceIds.Any(candidate.EvidenceIds.Contains))
                .OrderBy(section => section.PageId, StringComparer.Ordinal)
                .ThenBy(section => section.Section.Order)
                .ThenBy(section => section.Section.SectionId, StringComparer.Ordinal)
                .ToArray();
            var source = SelectSource(candidate, regionContexts, sectionContexts);
            var roles = source.Region is not null
                ? [source.Region.Role]
                : regionContexts.Select(region => region.Region.Role).Distinct(StringComparer.Ordinal).ToArray();
            var sourcePageId = source.PageId ?? "unknown";
            var pageArchetype = pageArchetypes.GetValueOrDefault(sourcePageId, "unknown");
            var sourceSectionId = source.SectionId ?? "unknown";
            var ecommerceRegionId = source.Region?.RegionId ?? "unknown";
            var preferredCatalogId = PreferredCatalogId(candidate.Family, pageArchetype);
            var candidateMatches = catalog.Components
                .Where(entry => entry.ComponentId == preferredCatalogId || entry.SupportedRegionRoles.Any(role => roles.Contains(role, StringComparer.Ordinal)))
                .Where(entry => entry.ComponentId == preferredCatalogId || IsVisualMappingTarget(entry) || entry.IntentCategory == "presentation action binding")
                .Where(entry => IsPageArchetypeCompatible(entry, pageArchetype))
                .Where(entry => IsRoleCompatible(entry, roles, preferredCatalogId))
                .OrderByDescending(entry => entry.ComponentId == preferredCatalogId)
                .ThenByDescending(IsVisualMappingTarget)
                .ThenBy(entry => entry.ComponentId, StringComparer.Ordinal)
                .ToArray();
            var match = candidateMatches.FirstOrDefault();
            if (match is null)
            {
                unsupported.Add(new UnsupportedPattern(candidate.FamilyId, "missing component", $"No compatible catalog component supports candidate family '{candidate.Family}' on page archetype '{pageArchetype}'.", candidate.EvidenceIds, true));
                continue;
            }

            var validation = ValidateMapping(candidate, match, roles, candidateMatches.Length > 1);
            if (validation.BlockingReason is not null)
            {
                unsupported.Add(new UnsupportedPattern(candidate.FamilyId, validation.Group, validation.BlockingReason, candidate.EvidenceIds, true));
                continue;
            }

            var targetGeneratedPath = match.AllowedFilePatterns.FirstOrDefault() ?? string.Empty;
            var confidence = Math.Min(candidate.Confidence, 0.82m);
            var humanReviewRequired = candidate.HumanReviewRequired || validation.HumanReviewRequired || confidence < 0.60m;
            mappings.Add(new PresentationMapping(
                candidate.FamilyId,
                match.ComponentId,
                match.Category is "starter visual slot" or "visual generation target" ? match.ComponentId : null,
                match.Variants.FirstOrDefault() ?? "default",
                candidate.Slots.Select(slot => $"{slot.SlotName}:{slot.SlotKind}").ToArray(),
                candidate.ResponsiveBehaviorRefs,
                semantic.Tokens.Where(token => token.EvidenceIds.Any(candidate.EvidenceIds.Contains)).Select(token => token.Role).Distinct(StringComparer.Ordinal).ToArray(),
                candidate.InteractionBehaviorRefs,
                roles.Select(EcommerceDataRequirement).Distinct(StringComparer.Ordinal).ToArray(),
                match.BehaviorOwnedByRuntime ? "runtime" : "presentation",
                confidence,
                candidate.EvidenceIds,
                match.ComponentId == preferredCatalogId ? "preferred-id-match" : "catalog-role-match",
                candidate.Alternatives,
                humanReviewRequired,
                sourcePageId,
                sourceSectionId,
                ecommerceRegionId,
                pageArchetype,
                targetGeneratedPath,
                GeneratedZoneForPath(targetGeneratedPath),
                "Storefront Presentation owns route declarations; generated visuals register view slots only",
                validation.ReasonCodes.Concat(source.ReasonCodes).Distinct(StringComparer.Ordinal).ToArray(),
                humanReviewRequired ? "NeedsReview" : "Approved"));
        }

        var document = new PresentationMappingsDocument("1.0", "presentation-mappings", "presentation-mappings", DateTimeOffset.UtcNow, components.ProjectId, mappings);
        var unsupportedDocument = new UnsupportedPatternsDocument("1.0", "unsupported-patterns", "unsupported-patterns", DateTimeOffset.UtcNow, components.ProjectId, unsupported);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/mapping/presentation-mappings.draft.json"), "presentation-mappings", document, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/mapping/unsupported-patterns.json"), "unsupported-patterns", unsupportedDocument, cancellationToken);
        return document;
    }

    private static async Task<IReadOnlyList<EcommerceRegionContext>> ReadRegionsAsync(string root, FileSystemVisualArtifactStore store, CancellationToken cancellationToken)
    {
        var pagesRoot = Path.Combine(root, "analysis", "pages");
        if (!Directory.Exists(pagesRoot)) return [];
        var regions = new List<EcommerceRegionContext>();
        foreach (var path in Directory.EnumerateFiles(pagesRoot, "ecommerce-regions.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            var document = await store.ReadJsonAsync<EcommerceRegionsDocument>(ArtifactPath.Create(relative), "ecommerce-regions", cancellationToken);
            regions.AddRange(document.Regions.Select(region => new EcommerceRegionContext(document.PageId, region)));
        }

        return regions;
    }

    private static async Task<IReadOnlyList<PageSectionContext>> ReadSectionsAsync(string root, FileSystemVisualArtifactStore store, CancellationToken cancellationToken)
    {
        var pagesRoot = Path.Combine(root, "analysis", "pages");
        if (!Directory.Exists(pagesRoot)) return [];
        var sections = new List<PageSectionContext>();
        foreach (var path in Directory.EnumerateFiles(pagesRoot, "sections.draft.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            var document = await store.ReadJsonAsync<SectionsDraftDocument>(ArtifactPath.Create(relative), "sections", cancellationToken);
            sections.AddRange(document.Sections.Select(section => new PageSectionContext(document.PageId, section)));
        }

        return sections;
    }

    private static async Task<IReadOnlyDictionary<string, string>> ReadPageArchetypesAsync(string root, FileSystemVisualArtifactStore store, CancellationToken cancellationToken)
    {
        var pagesRoot = Path.Combine(root, "analysis", "pages");
        if (!Directory.Exists(pagesRoot)) return new Dictionary<string, string>(StringComparer.Ordinal);
        var archetypes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(pagesRoot, "page-archetype.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            var document = await store.ReadJsonAsync<PageArchetypeDocument>(ArtifactPath.Create(relative), "page-archetype", cancellationToken);
            archetypes[document.PageId] = document.PrimaryArchetype;
        }

        return archetypes;
    }

    private static string PreferredCatalogId(string family, string pageArchetype) =>
        family switch
        {
            "product card" => pageArchetype == "home" ? "home.sections" : "catalog.product-card",
            "product image" => pageArchetype == "home" ? "home.sections" : "catalog.product-card",
            "price display" => pageArchetype == "home" ? "home.sections" : "catalog.product-card",
            "product gallery" => "product.gallery",
            "purchase action visual" => "product.purchase",
            "hero" => pageArchetype == "home" ? "home.sections" : "missing.hero",
            "announcement bar" => "home.sections",
            "header" => "layout.header",
            "navigation" => "layout.main-navigation",
            "cart trigger" => "layout.cart-badge",
            "account trigger" => "layout.account-menu",
            "footer" => "layout.footer",
            "account shell" => "account.shell",
            _ => "missing." + family.Replace(' ', '-')
        };

    private static string EcommerceDataRequirement(string role) =>
        role.Contains("cart", StringComparison.OrdinalIgnoreCase) ? "cart" :
        role.Contains("checkout", StringComparison.OrdinalIgnoreCase) ? "checkout" :
        role.Contains("product", StringComparison.OrdinalIgnoreCase) || role.Contains("price", StringComparison.OrdinalIgnoreCase) ? "product" :
        role.Contains("navigation", StringComparison.OrdinalIgnoreCase) || role.Contains("header", StringComparison.OrdinalIgnoreCase) ? "shell" :
        "catalog";

    private static bool IsPageArchetypeCompatible(PresentationCatalogEntry entry, string pageArchetype) =>
        entry.SupportedPageArchetypes.Count == 0 ||
        entry.SupportedPageArchetypes.Contains(pageArchetype, StringComparer.Ordinal);

    private static bool IsRoleCompatible(PresentationCatalogEntry entry, IReadOnlyList<string> roles, string preferredCatalogId) =>
        entry.ComponentId == preferredCatalogId ||
        entry.SupportedRegionRoles.Count == 0 ||
        entry.SupportedRegionRoles.Any(role => roles.Contains(role, StringComparer.Ordinal));

    private static bool IsVisualMappingTarget(PresentationCatalogEntry entry) =>
        entry.Category is "starter visual slot" or "visual generation target";

    private static MappingValidationResult ValidateMapping(
        VisualComponentCandidate candidate,
        PresentationCatalogEntry match,
        IReadOnlyList<string> roles,
        bool ambiguous)
    {
        var reasonCodes = new List<string> { "page-archetype-compatible", "ecommerce-role-compatible" };
        var targetGeneratedPath = match.AllowedFilePatterns.FirstOrDefault() ?? string.Empty;
        if (match.IntentCategory is "runtime-owned behavior" && match.VisualOverrideAllowed is false)
        {
            return MappingValidationResult.Block("runtime-behavior-assigned-to-visual-code", "Runtime-owned behavior cannot be assigned to generated visual code.");
        }

        if ((match.Category is "starter visual slot" or "visual generation target") && string.IsNullOrWhiteSpace(targetGeneratedPath))
        {
            return MappingValidationResult.Block("missing-target-generated-path", "Visual mapping target does not declare an allowed generated path.");
        }

        if (!string.IsNullOrWhiteSpace(targetGeneratedPath) && match.ProtectedFilePatterns.Any(pattern => targetGeneratedPath.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return MappingValidationResult.Block("protected-path-target", $"Mapping targets protected path '{targetGeneratedPath}'.");
        }

        if (!string.IsNullOrWhiteSpace(targetGeneratedPath) && GeneratedZoneForPath(targetGeneratedPath) == "unknown")
        {
            return MappingValidationResult.Block("unknown-generated-zone", $"Mapping target '{targetGeneratedPath}' is outside known generated zones.");
        }

        var slotNames = candidate.Slots.Select(slot => slot.SlotName).ToHashSet(StringComparer.Ordinal);
        if (match.RequiredChildren.Any(required => !slotNames.Contains(required)))
        {
            return MappingValidationResult.Block("missing-required-child-slot", "Candidate does not contain every required child slot for the Presentation target.");
        }

        if (roles.Any(role => role.Contains("drawer", StringComparison.OrdinalIgnoreCase) || role.Contains("overlay", StringComparison.OrdinalIgnoreCase)) &&
            !match.InteractionCapabilities.Any())
        {
            return MappingValidationResult.Block("unsupported-critical-interaction", "Candidate requires interaction capability not present in catalog.");
        }

        if (candidate.InteractionBehaviorRefs.Any(IsUnsafeBrowserAction))
        {
            return MappingValidationResult.Block("unsafe-browser-action", "Candidate interaction attempts to call Commerce Node Storefront APIs directly.");
        }

        var actionRefs = candidate.InteractionBehaviorRefs.Where(reference => !string.IsNullOrWhiteSpace(reference)).ToArray();
        if (actionRefs.Length > 0 && match.InteractionCapabilities.Count > 0 &&
            actionRefs.Any(reference => !match.InteractionCapabilities.Contains(reference, StringComparer.Ordinal)))
        {
            reasonCodes.Add("action-descriptor-needs-review");
            return new MappingValidationResult(null, "action-descriptor-review", true, reasonCodes);
        }

        if (ambiguous)
        {
            reasonCodes.Add("ambiguous-catalog-role-match");
            return new MappingValidationResult(null, "ambiguous-role-match", true, reasonCodes);
        }

        if (candidate.Confidence < 0.60m)
        {
            reasonCodes.Add("low-confidence");
            return new MappingValidationResult(null, "low-confidence", true, reasonCodes);
        }

        reasonCodes.Add(match.AllowedFilePatterns.Count > 0 ? "target-generated-path-allowed" : "behavior-binding-no-generated-path");
        return new MappingValidationResult(null, "valid", false, reasonCodes);
    }

    private static bool IsUnsafeBrowserAction(string reference) =>
        reference.Contains("/api/storefront/", StringComparison.OrdinalIgnoreCase) ||
        reference.Contains("api/storefront/stores/", StringComparison.OrdinalIgnoreCase) ||
        reference.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        reference.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string GeneratedZoneForPath(string path) =>
        path.StartsWith("Pages/", StringComparison.OrdinalIgnoreCase) ? "pages" :
        path.StartsWith("Components/Layout/", StringComparison.OrdinalIgnoreCase) ? "layout-components" :
        path.StartsWith("Components/Catalog/", StringComparison.OrdinalIgnoreCase) ? "catalog-components" :
        path.StartsWith("Components/", StringComparison.OrdinalIgnoreCase) ? "components" :
        string.IsNullOrWhiteSpace(path) ? "none" :
        "unknown";

    private static SourceSelection SelectSource(
        VisualComponentCandidate candidate,
        IReadOnlyList<EcommerceRegionContext> regions,
        IReadOnlyList<PageSectionContext> sections)
    {
        var bestRegion = regions
            .Select(region => new
            {
                Context = region,
                Score = ScoreRegion(candidate, region, sections)
            })
            .OrderByDescending(region => region.Score)
            .ThenBy(region => region.Context.PageId, StringComparer.Ordinal)
            .ThenBy(region => region.Context.Region.RegionId, StringComparer.Ordinal)
            .FirstOrDefault();

        var bestSection = sections
            .Select(section => new
            {
                Context = section,
                Score = ScoreSection(candidate, section)
            })
            .OrderByDescending(section => section.Score)
            .ThenBy(section => section.Context.PageId, StringComparer.Ordinal)
            .ThenBy(section => section.Context.Section.Order)
            .ThenBy(section => section.Context.Section.SectionId, StringComparer.Ordinal)
            .FirstOrDefault();

        if (bestRegion is not null && bestRegion.Score >= (bestSection?.Score ?? 0))
        {
            var sectionId = BestRegionSectionId(candidate, bestRegion.Context, sections);
            var reasonCodes = new List<string> { "source-region-binding" };
            if (bestRegion.Context.Region.EvidenceIds.Any(candidate.EvidenceIds.Contains))
            {
                reasonCodes.Add("source-evidence-overlap");
            }

            if (!string.IsNullOrWhiteSpace(sectionId) && sectionId != "unknown")
            {
                reasonCodes.Add("source-section-binding");
            }

            return new SourceSelection(bestRegion.Context.PageId, sectionId, bestRegion.Context.Region, reasonCodes);
        }

        if (bestSection is not null)
        {
            return new SourceSelection(bestSection.Context.PageId, bestSection.Context.Section.SectionId, null, ["source-evidence-overlap", "source-section-binding"]);
        }

        return new SourceSelection(null, null, null, ["source-unknown"]);
    }

    private static int ScoreRegion(VisualComponentCandidate candidate, EcommerceRegionContext region, IReadOnlyList<PageSectionContext> sections)
    {
        var score = 0;
        if (region.Region.EvidenceIds.Any(candidate.EvidenceIds.Contains))
        {
            score += 100;
        }

        if (region.Region.SourceComponentFamilyIds.Contains(candidate.FamilyId, StringComparer.Ordinal))
        {
            score += 40;
        }

        if (region.Region.SourceSectionIds.Any(sectionId => sections.Any(section => section.PageId == region.PageId && section.Section.SectionId == sectionId && section.Section.EvidenceIds.Any(candidate.EvidenceIds.Contains))))
        {
            score += 30;
        }

        if (RoleLooksCompatible(candidate.Family, region.Region.Role))
        {
            score += 20;
        }

        return score;
    }

    private static int ScoreSection(VisualComponentCandidate candidate, PageSectionContext section)
    {
        var score = section.Section.EvidenceIds.Any(candidate.EvidenceIds.Contains) ? 90 : 0;
        if (RoleLooksCompatible(candidate.Family, section.Section.SectionType))
        {
            score += 20;
        }

        return score;
    }

    private static string BestRegionSectionId(VisualComponentCandidate candidate, EcommerceRegionContext region, IReadOnlyList<PageSectionContext> sections)
    {
        var matched = region.Region.SourceSectionIds
            .Select(sectionId => sections.FirstOrDefault(section => section.PageId == region.PageId && section.Section.SectionId == sectionId))
            .Where(section => section is not null)
            .OrderByDescending(section => section!.Section.EvidenceIds.Any(candidate.EvidenceIds.Contains))
            .ThenBy(section => section!.Section.Order)
            .ThenBy(section => section!.Section.SectionId, StringComparer.Ordinal)
            .FirstOrDefault();
        return matched?.Section.SectionId ?? region.Region.SourceSectionIds.FirstOrDefault() ?? "unknown";
    }

    private static bool RoleLooksCompatible(string family, string role)
    {
        var normalizedFamily = family.Replace("-", " ", StringComparison.OrdinalIgnoreCase);
        return role.Contains(normalizedFamily, StringComparison.OrdinalIgnoreCase) ||
            (family.Contains("product", StringComparison.OrdinalIgnoreCase) && role.Contains("product", StringComparison.OrdinalIgnoreCase)) ||
            (family.Contains("cart", StringComparison.OrdinalIgnoreCase) && role.Contains("cart", StringComparison.OrdinalIgnoreCase)) ||
            (family.Contains("account", StringComparison.OrdinalIgnoreCase) && role.Contains("account", StringComparison.OrdinalIgnoreCase)) ||
            (family.Contains("hero", StringComparison.OrdinalIgnoreCase) && role.Contains("hero", StringComparison.OrdinalIgnoreCase)) ||
            (family.Contains("footer", StringComparison.OrdinalIgnoreCase) && role.Contains("footer", StringComparison.OrdinalIgnoreCase)) ||
            (family.Contains("navigation", StringComparison.OrdinalIgnoreCase) && role.Contains("navigation", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record EcommerceRegionContext(string PageId, EcommerceRegion Region);

    private sealed record PageSectionContext(string PageId, SectionDraft Section);

    private sealed record SourceSelection(
        string? PageId,
        string? SectionId,
        EcommerceRegion? Region,
        IReadOnlyList<string> ReasonCodes);

    private sealed record MappingValidationResult(
        string? BlockingReason,
        string Group,
        bool HumanReviewRequired,
        IReadOnlyList<string> ReasonCodes)
    {
        public static MappingValidationResult Block(string group, string reason) => new(reason, group, true, [group]);
    }
}
