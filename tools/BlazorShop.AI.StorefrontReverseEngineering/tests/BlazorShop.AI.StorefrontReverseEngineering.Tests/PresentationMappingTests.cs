using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
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

    private static VisualComponentCandidate Candidate(string familyId, string family, IReadOnlyList<string> evidenceIds) =>
        new(familyId, family, familyId + "-default", 0.72m, ["instance-" + familyId], [new ComponentSlot("root", "container", evidenceIds, 0.7m)], TokenReferences: [], LocalOverrideIds: [], ResponsiveBehaviorRefs: [], InteractionBehaviorRefs: [], Alternatives: [], HumanReviewRequired: false, evidenceIds);

    private static async Task<string> CreateProjectAsync(VisualComponentCandidate candidate, string regionRole)
    {
        var repoRoot = GetRepoRoot();
        var root = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "mapping-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "components"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "tokens"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "pages", "home"));
        var candidates = new ComponentCandidatesDocument("1.0", "component-candidates", "component-candidates-mapping", DateTimeOffset.UtcNow, "mapping", [candidate], Issues: []);
        var semantic = new SemanticTokenDocument("1.0", "semantic-tokens", "semantic-tokens-mapping", DateTimeOffset.UtcNow, "mapping", "analysis/tokens/raw-design-tokens.json", [new SemanticToken("text-body", "typography", ["16px"], ["raw"], candidate.EvidenceIds, 0.6m, ["test"], false)], PageLocalOverrides: [], ComponentLocalOverrides: [], HumanReviewRequired: false, ReviewReasons: []);
        var regions = new EcommerceRegionsDocument("1.0", "ecommerce-regions", "ecommerce-regions-mapping-home", DateTimeOffset.UtcNow, "mapping", "home", [new EcommerceRegion("region-01", regionRole, "catalog", "presentation-only", false, true, regionRole == "unknown role", ["section-01"], [candidate.FamilyId], candidate.EvidenceIds, Alternatives: [])]);
        await WriteAsync(root, "analysis/components/component-candidates.json", candidates);
        await WriteAsync(root, "analysis/tokens/semantic-tokens.draft.json", semantic);
        await WriteAsync(root, "analysis/pages/home/ecommerce-regions.json", regions);
        return root;
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
