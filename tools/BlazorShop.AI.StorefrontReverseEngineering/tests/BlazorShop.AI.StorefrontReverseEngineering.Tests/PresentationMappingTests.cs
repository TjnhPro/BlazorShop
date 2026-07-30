using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class PresentationMappingTests
{
    [Fact]
    public async Task PresentationMapping_ProductCardMapsToStarterSlot()
    {
        var projectRoot = await CreateProjectAsync(Candidate("family-product-card", "product card", ["ev-card"]), "product card collection");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);

        var mappings = await new PresentationMapper(GetRepoRoot()).MapAsync(projectRoot, CancellationToken.None);

        var mapping = Assert.Single(mappings.Mappings);
        Assert.Equal("catalog.product-card", mapping.PresentationComponentId);
        Assert.Equal("catalog.product-card", mapping.StarterSlotId);
        Assert.Equal("category", mapping.SourcePageId);
        Assert.Equal("section-01", mapping.SourceSectionId);
        Assert.Equal("region-01", mapping.EcommerceRegionId);
        Assert.Equal("product-listing", mapping.PageArchetype);
        Assert.Equal("Components/Catalog/ProductSummaryCard.razor", mapping.TargetGeneratedPath);
        Assert.Equal("catalog-components", mapping.GeneratedZone);
        Assert.Equal("Approved", mapping.ReviewState);
        Assert.Contains("product", mapping.DataRequirements);
        Assert.Contains("ev-card", mapping.EvidenceIds);
    }

    [Fact]
    public async Task PresentationMapping_UnknownCandidateBecomesUnsupported()
    {
        var projectRoot = await CreateProjectAsync(Candidate("family-custom", "custom immersive widget", ["ev-custom"]), "unknown role");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);

        await new PresentationMapper(GetRepoRoot()).MapAsync(projectRoot, CancellationToken.None);
        var unsupported = await ReadUnsupportedAsync(projectRoot);

        Assert.Empty((await ReadMappingsAsync(projectRoot)).Mappings);
        Assert.Contains(unsupported.Patterns, pattern => pattern.Group == "missing component" && pattern.HumanReviewRequired);
    }

    [Fact]
    public async Task PresentationMapping_RoleOnlyMatchRequiresCompatiblePageArchetype()
    {
        var compatibleRoot = await CreateProjectAsync(Candidate("family-product-media", "custom media", ["ev-media"]), "product media", pageId: "product", pageArchetype: "product-detail");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(compatibleRoot, CancellationToken.None);

        var mappings = await new PresentationMapper(GetRepoRoot()).MapAsync(compatibleRoot, CancellationToken.None);

        var mapping = Assert.Single(mappings.Mappings);
        Assert.Equal("product.gallery", mapping.PresentationComponentId);
        Assert.Equal("product.gallery", mapping.StarterSlotId);
        Assert.Equal("product-detail", mapping.PageArchetype);

        var incompatibleRoot = await CreateProjectAsync(Candidate("family-home-media", "custom media", ["ev-home-media"]), "product media", pageId: "home", pageArchetype: "home");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(incompatibleRoot, CancellationToken.None);

        await new PresentationMapper(GetRepoRoot()).MapAsync(incompatibleRoot, CancellationToken.None);

        Assert.Empty((await ReadMappingsAsync(incompatibleRoot)).Mappings);
        Assert.Contains((await ReadUnsupportedAsync(incompatibleRoot)).Patterns, pattern => pattern.Group == "missing component");
    }

    [Fact]
    public async Task PresentationMapping_AmbiguousRoleMappingRequiresReview()
    {
        var projectRoot = await CreateProjectAsync(Candidate("family-navigation", "custom navigation", ["ev-nav"]), "primary/category navigation", pageArchetype: "home");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);

        var mappings = await new PresentationMapper(GetRepoRoot()).MapAsync(projectRoot, CancellationToken.None);

        var mapping = Assert.Single(mappings.Mappings);
        Assert.True(mapping.HumanReviewRequired);
        Assert.Equal("NeedsReview", mapping.ReviewState);
        Assert.Contains("ambiguous-catalog-role-match", mapping.ReasonCodes);
    }

    [Fact]
    public async Task PresentationMapping_ProtectedPathMappingFails()
    {
        var projectRoot = await CreateProjectAsync(Candidate("family-product-card", "product card", ["ev-card"]), "product card collection");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);
        await MutateCatalogEntryAsync(projectRoot, "catalog.product-card", entry => entry["allowedFilePatterns"] = new JsonArray("starter-generation.contract.yaml"));

        await new PresentationMapper(GetRepoRoot()).MapAsync(projectRoot, CancellationToken.None);

        Assert.Empty((await ReadMappingsAsync(projectRoot)).Mappings);
        Assert.Contains((await ReadUnsupportedAsync(projectRoot)).Patterns, pattern => pattern.Group == "protected-path-target");
    }

    [Fact]
    public async Task PresentationMapping_DirectStorefrontApiInteractionFails()
    {
        var candidate = Candidate("family-product-card", "product card", ["ev-card"], interactionRefs: ["/api/storefront/stores/demo/cart"]);
        var projectRoot = await CreateProjectAsync(candidate, "product card collection");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);

        await new PresentationMapper(GetRepoRoot()).MapAsync(projectRoot, CancellationToken.None);

        Assert.Empty((await ReadMappingsAsync(projectRoot)).Mappings);
        Assert.Contains((await ReadUnsupportedAsync(projectRoot)).Patterns, pattern => pattern.Group == "unsafe-browser-action");
    }

    [Fact]
    public async Task PresentationMapping_RuntimeOwnedBehaviorFailsForVisualMapping()
    {
        var projectRoot = await CreateProjectAsync(Candidate("family-account", "account shell", ["ev-account"]), "account access", pageId: "account", pageArchetype: "account-auth-shell");
        await new PresentationComponentCatalogBuilder(GetRepoRoot()).BuildAsync(projectRoot, CancellationToken.None);

        await new PresentationMapper(GetRepoRoot()).MapAsync(projectRoot, CancellationToken.None);

        Assert.Empty((await ReadMappingsAsync(projectRoot)).Mappings);
        Assert.Contains((await ReadUnsupportedAsync(projectRoot)).Patterns, pattern => pattern.Group == "runtime-behavior-assigned-to-visual-code");
    }

    [Fact]
    public void PresentationMapping_RejectedMappingIsExcludedFromAgentHandoff()
    {
        var approved = MappingWithReviewState("Approved");
        var rejected = MappingWithReviewState("Rejected");

        var handoff = PresentationMappingReviewFilter.ForAgentHandoff([rejected, approved]);

        Assert.Same(approved, Assert.Single(handoff));
    }

    private static PresentationMapping MappingWithReviewState(string reviewState) =>
        new(
            "candidate",
            "catalog.product-card",
            "catalog.product-card",
            "default",
            [],
            [],
            [],
            [],
            [],
            "presentation",
            0.75m,
            [],
            "test",
            [],
            reviewState != "Approved",
            "category",
            "section-01",
            "region-01",
            "product-listing",
            "Components/Catalog/ProductSummaryCard.razor",
            "catalog-components",
            "Storefront Presentation owns route declarations; generated visuals register view slots only",
            [],
            reviewState);

    private static VisualComponentCandidate Candidate(
        string familyId,
        string family,
        IReadOnlyList<string> evidenceIds,
        IReadOnlyList<string>? interactionRefs = null,
        decimal confidence = 0.72m) =>
        new(familyId, family, familyId + "-default", confidence, ["instance-" + familyId], [new ComponentSlot("root", "container", evidenceIds, 0.7m)], TokenReferences: [], LocalOverrideIds: [], ResponsiveBehaviorRefs: [], InteractionBehaviorRefs: interactionRefs ?? [], Alternatives: [], HumanReviewRequired: false, evidenceIds);

    private static async Task<string> CreateProjectAsync(
        VisualComponentCandidate candidate,
        string regionRole,
        string pageId = "category",
        string pageArchetype = "product-listing")
    {
        var repoRoot = GetRepoRoot();
        var root = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "mapping-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "components"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "tokens"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "pages", pageId));
        var candidates = new ComponentCandidatesDocument("1.0", "component-candidates", "component-candidates-mapping", DateTimeOffset.UtcNow, "mapping", [candidate], Issues: []);
        var semantic = new SemanticTokenDocument("1.0", "semantic-tokens", "semantic-tokens-mapping", DateTimeOffset.UtcNow, "mapping", "analysis/tokens/raw-design-tokens.json", [new SemanticToken("text-body", "typography", ["16px"], ["raw"], candidate.EvidenceIds, 0.6m, ["test"], false)], PageLocalOverrides: [], ComponentLocalOverrides: [], HumanReviewRequired: false, ReviewReasons: []);
        var regions = new EcommerceRegionsDocument("1.0", "ecommerce-regions", $"ecommerce-regions-mapping-{pageId}", DateTimeOffset.UtcNow, "mapping", pageId, [new EcommerceRegion("region-01", regionRole, "catalog", "presentation-only", false, true, regionRole == "unknown role", ["section-01"], [candidate.FamilyId], candidate.EvidenceIds, Alternatives: [])]);
        var archetype = new PageArchetypeDocument("1.0", "page-archetype", $"page-archetype-{pageId}", DateTimeOffset.UtcNow, "mapping", pageId, pageArchetype, 0.90m, candidate.EvidenceIds, ["test"], Alternatives: []);
        await WriteAsync(root, "analysis/components/component-candidates.json", candidates);
        await WriteAsync(root, "analysis/tokens/semantic-tokens.draft.json", semantic);
        await WriteAsync(root, $"analysis/pages/{pageId}/ecommerce-regions.json", regions);
        await WriteAsync(root, $"analysis/pages/{pageId}/page-archetype.json", archetype);
        return root;
    }

    private static async Task MutateCatalogEntryAsync(string projectRoot, string componentId, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, "presentation-catalog", "presentation-component-catalog.json");
        var node = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        var entry = node["components"]?.AsArray().OfType<JsonObject>().FirstOrDefault(candidate => candidate["componentId"]?.GetValue<string>() == componentId)
            ?? throw new InvalidOperationException($"Catalog entry '{componentId}' not found.");
        mutate(entry);
        await File.WriteAllTextAsync(path, node.ToJsonString(VisualJson.Options));
    }

    private static async Task<PresentationMappingsDocument> ReadMappingsAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "mapping", "presentation-mappings.draft.json"));
        return JsonSerializer.Deserialize<PresentationMappingsDocument>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Mappings artifact did not deserialize.");
    }

    private static async Task<UnsupportedPatternsDocument> ReadUnsupportedAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "mapping", "unsupported-patterns.json"));
        return JsonSerializer.Deserialize<UnsupportedPatternsDocument>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Unsupported artifact did not deserialize.");
    }

    private static async Task WriteAsync<T>(string root, string path, T artifact)
    {
        await File.WriteAllTextAsync(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)), JsonSerializer.Serialize(artifact, VisualJson.Options) + Environment.NewLine);
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
