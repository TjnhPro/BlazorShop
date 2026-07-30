using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
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
        var mappings = new List<PresentationMapping>();
        var unsupported = new List<UnsupportedPattern>();
        foreach (var candidate in components.Candidates)
        {
            var roles = ecommerce.Where(region => region.SourceComponentFamilyIds.Contains(candidate.FamilyId)).Select(region => region.Role).Distinct(StringComparer.Ordinal).ToArray();
            var match = catalog.Components.FirstOrDefault(entry =>
                entry.ComponentId == PreferredCatalogId(candidate.Family) ||
                entry.SupportedRegionRoles.Any(roles.Contains));
            if (match is null)
            {
                unsupported.Add(new UnsupportedPattern(candidate.FamilyId, "missing component", $"No catalog component supports candidate family '{candidate.Family}'.", candidate.EvidenceIds, true));
                continue;
            }

            if (roles.Any(role => role.Contains("drawer", StringComparison.OrdinalIgnoreCase) || role.Contains("overlay", StringComparison.OrdinalIgnoreCase)) &&
                !match.InteractionCapabilities.Any())
            {
                unsupported.Add(new UnsupportedPattern(candidate.FamilyId, "unsupported overlay/drawer/gallery/product option/content/shell behavior", "Candidate requires interaction capability not present in catalog.", candidate.EvidenceIds, true));
                continue;
            }

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
                Math.Min(candidate.Confidence, 0.82m),
                candidate.EvidenceIds,
                "catalog-role-or-id-match",
                candidate.Alternatives,
                candidate.HumanReviewRequired || unsupported.Any(pattern => pattern.SourceCandidateId == candidate.FamilyId)));
        }

        var document = new PresentationMappingsDocument("1.0", "presentation-mappings", "presentation-mappings", DateTimeOffset.UtcNow, components.ProjectId, mappings);
        var unsupportedDocument = new UnsupportedPatternsDocument("1.0", "unsupported-patterns", "unsupported-patterns", DateTimeOffset.UtcNow, components.ProjectId, unsupported);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/mapping/presentation-mappings.draft.json"), "presentation-mappings", document, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/mapping/unsupported-patterns.json"), "unsupported-patterns", unsupportedDocument, cancellationToken);
        return document;
    }

    private static async Task<IReadOnlyList<EcommerceRegion>> ReadRegionsAsync(string root, FileSystemVisualArtifactStore store, CancellationToken cancellationToken)
    {
        var pagesRoot = Path.Combine(root, "analysis", "pages");
        if (!Directory.Exists(pagesRoot)) return [];
        var regions = new List<EcommerceRegion>();
        foreach (var path in Directory.EnumerateFiles(pagesRoot, "ecommerce-regions.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            var document = await store.ReadJsonAsync<EcommerceRegionsDocument>(ArtifactPath.Create(relative), "ecommerce-regions", cancellationToken);
            regions.AddRange(document.Regions);
        }

        return regions;
    }

    private static string PreferredCatalogId(string family) =>
        family switch
        {
            "product card" => "catalog.product-card",
            "product gallery" => "product.gallery",
            "purchase action visual" => "product.purchase",
            "header" => "layout.header",
            "navigation" => "layout.main-navigation",
            "footer" => "layout.footer",
            _ => "missing." + family.Replace(' ', '-')
        };

    private static string EcommerceDataRequirement(string role) =>
        role.Contains("cart", StringComparison.OrdinalIgnoreCase) ? "cart" :
        role.Contains("checkout", StringComparison.OrdinalIgnoreCase) ? "checkout" :
        role.Contains("product", StringComparison.OrdinalIgnoreCase) || role.Contains("price", StringComparison.OrdinalIgnoreCase) ? "product" :
        role.Contains("navigation", StringComparison.OrdinalIgnoreCase) || role.Contains("header", StringComparison.OrdinalIgnoreCase) ? "shell" :
        "catalog";
}
