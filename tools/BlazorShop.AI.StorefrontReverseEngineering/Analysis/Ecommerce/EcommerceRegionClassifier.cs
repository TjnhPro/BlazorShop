using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;

public sealed class EcommerceRegionClassifier
{
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public EcommerceRegionClassifier(string repoRoot)
    {
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<IReadOnlyList<EcommerceRegionsDocument>> ClassifyAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var components = await store.ReadJsonAsync<ComponentCandidatesDocument>(ArtifactPath.Create("analysis/components/component-candidates.json"), "component-candidates", cancellationToken);
        _ = await store.ReadJsonAsync<SemanticTokenDocument>(ArtifactPath.Create("analysis/tokens/semantic-tokens.draft.json"), "semantic-tokens", cancellationToken);
        var pages = Directory.EnumerateDirectories(Path.Combine(root, "analysis", "pages"))
            .Select(Path.GetFileName)
            .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
        var documents = new List<EcommerceRegionsDocument>();
        foreach (var pageId in pages)
        {
            var archetype = await store.ReadJsonAsync<PageArchetypeDocument>(ArtifactPath.Create($"analysis/pages/{pageId}/page-archetype.json"), "page-archetype", cancellationToken);
            var sections = await store.ReadJsonAsync<SectionsDraftDocument>(ArtifactPath.Create($"analysis/pages/{pageId}/sections.draft.json"), "sections", cancellationToken);
            var interaction = await store.ReadJsonAsync<InteractionModelDocument>(ArtifactPath.Create($"analysis/pages/{pageId}/interaction-model.json"), "interaction-model", cancellationToken);
            var document = Build(components, archetype, sections, interaction);
            await store.WriteJsonAsync(ArtifactPath.Create($"analysis/pages/{pageId}/ecommerce-regions.json"), "ecommerce-regions", document, cancellationToken);
            documents.Add(document);
        }

        return documents;
    }

    private static EcommerceRegionsDocument Build(
        ComponentCandidatesDocument components,
        PageArchetypeDocument archetype,
        SectionsDraftDocument sections,
        InteractionModelDocument interaction)
    {
        var regions = new List<EcommerceRegion>();
        foreach (var section in sections.Sections)
        {
            var matchingComponents = components.Candidates
                .Where(component => component.EvidenceIds.Any(section.EvidenceIds.Contains))
                .ToArray();
            var role = RoleFor(section, matchingComponents, archetype);
            AddRegion(regions, role, section, matchingComponents, interaction);
            foreach (var componentRole in matchingComponents.Select(RoleForComponent).Where(componentRole => componentRole is not null).Cast<string>().Distinct(StringComparer.Ordinal))
            {
                if (componentRole != role)
                {
                    AddRegion(regions, componentRole, section, matchingComponents.Where(component => RoleForComponent(component) == componentRole).ToArray(), interaction);
                }
            }
        }

        if (regions.Count == 0)
        {
            regions.Add(new EcommerceRegion("region-01", "unknown role", "system state", "none", false, true, true, [], [], [], []));
        }

        return new EcommerceRegionsDocument(
            "1.0",
            "ecommerce-regions",
            $"ecommerce-regions-{archetype.ProjectId}-{archetype.PageId}",
            DateTimeOffset.UtcNow,
            archetype.ProjectId,
            archetype.PageId,
            regions);
    }

    private static void AddRegion(
        List<EcommerceRegion> regions,
        string role,
        SectionDraft section,
        IReadOnlyList<VisualComponentCandidate> matchingComponents,
        InteractionModelDocument interaction)
    {
        regions.Add(new EcommerceRegion(
            $"region-{regions.Count + 1:00}",
            role,
            DataDependencyFor(role),
            BehaviorFor(role, interaction),
            SeoRelevant: role is "product title" or "price" or "product media" or "description/metadata/trust/reviews" or "product listing",
            PresentationOnly: role is "store header" or "primary/category navigation" or "search access" or "account access" or "cart access" or "add-to-cart/buy-now visual" or "payment visual placeholder" or "unknown role",
            Unsupported: role == "unknown role",
            [section.SectionId],
            matchingComponents.Select(component => component.FamilyId).ToArray(),
            section.EvidenceIds,
            AlternativesFor(role)));
    }

    private static string RoleFor(SectionDraft section, IReadOnlyList<VisualComponentCandidate> components, PageArchetypeDocument archetype)
    {
        var type = section.SectionType;
        if (type == "header") return "store header";
        if (type == "navigation" || type == "category navigation") return "primary/category navigation";
        if (components.Any(component => component.Family == "search trigger")) return "search access";
        if (components.Any(component => component.Family == "account trigger")) return "account access";
        if (components.Any(component => component.Family == "cart trigger")) return "cart access";
        if (type == "product grid" || components.Any(component => component.Family is "product grid" or "product card")) return archetype.PrimaryArchetype == "product-detail" ? "related/cross-sell/upsell" : "product card collection";
        if (type == "product gallery" || components.Any(component => component.Family == "product gallery")) return "product media";
        if (type == "product information") return "product title";
        if (type == "purchase actions" || components.Any(component => component.Family == "purchase action visual")) return "add-to-cart/buy-now visual";
        if (components.Any(component => component.Family == "price display")) return "price";
        if (components.Any(component => component.Family == "variant selector visual")) return "variant options";
        if (components.Any(component => component.Family == "quantity selector visual")) return "quantity";
        if (type is "reviews/testimonials" or "trust/benefit strip") return "description/metadata/trust/reviews";
        if (components.Any(component => component.Family == "filter trigger/panel")) return "filter";
        if (components.Any(component => component.Family == "sort selector")) return "sort";
        if (components.Any(component => component.Family == "pagination")) return "pagination/load-more";
        if (components.Any(component => component.Family == "cart line visual")) return "cart line items visual";
        if (components.Any(component => component.Family == "order summary visual")) return "order summary visual";
        if (type == "unknown section") return "unknown role";
        return archetype.PrimaryArchetype switch
        {
            "cart-shell" => "cart summary",
            "checkout-shell" => "checkout form region",
            "search-results" => "empty/search result region",
            _ => "unknown role"
        };
    }

    private static string? RoleForComponent(VisualComponentCandidate component) =>
        component.Family switch
        {
            "product gallery" => "product media",
            "price display" => "price",
            "purchase action visual" => "add-to-cart/buy-now visual",
            "variant selector visual" => "variant options",
            "quantity selector visual" => "quantity",
            "filter trigger/panel" => "filter",
            "sort selector" => "sort",
            "pagination" => "pagination/load-more",
            "cart line visual" => "cart line items visual",
            "order summary visual" => "order summary visual",
            _ => null
        };

    private static string DataDependencyFor(string role) =>
        role switch
        {
            "account access" => "account",
            "store header" or "primary/category navigation" or "search access" or "cart access" => "shell",
            "product listing" or "product card collection" or "filter" or "sort" or "pagination/load-more" or "empty/search result region" => "catalog",
            "product media" or "product title" or "price" or "variant options" or "quantity" or "add-to-cart/buy-now visual" or "description/metadata/trust/reviews" or "related/cross-sell/upsell" => "product",
            "cart line items visual" or "cart summary" or "promo-code visual" or "checkout CTA visual" => "cart",
            "checkout form region" or "order summary visual" or "payment visual placeholder" => "checkout",
            _ => "system state"
        };

    private static string BehaviorFor(string role, InteractionModelDocument interaction) =>
        role is "add-to-cart/buy-now visual" or "quantity" or "variant options" || interaction.Interactions.Any(item => item.Classification == "business behavior required")
            ? "runtime-business-behavior-required"
            : "presentation-only";

    private static IReadOnlyList<string> AlternativesFor(string role) =>
        role == "unknown role" ? ["presentation-only region", "unsupported role"] : [];
}
