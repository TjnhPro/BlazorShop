using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Components;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Ecommerce;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Pages;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Tokens;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class EcommerceRegionClassificationTests
{
    [Fact]
    public async Task Ecommerce_ProductGridRegionIsDetected()
    {
        var projectRoot = await CreateProjectAsync("product-listing", "product grid", [Candidate("family-product-card", "product card", ["grid-evidence"])]);

        var document = Assert.Single(await new EcommerceRegionClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None));

        Assert.Contains(document.Regions, region => region.Role == "product card collection" && region.DataDependency == "catalog");
    }

    [Fact]
    public async Task Ecommerce_PdpGalleryTitlePricePurchaseRegionsAreDetected()
    {
        var projectRoot = await CreateProjectAsync(
            "product-detail",
            "product gallery",
            [
                Candidate("family-gallery", "product gallery", ["region-evidence"]),
                Candidate("family-price", "price display", ["region-evidence"]),
                Candidate("family-action", "purchase action visual", ["region-evidence"])
            ]);

        var document = Assert.Single(await new EcommerceRegionClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None));

        Assert.Contains(document.Regions, region => region.Role == "product media");
        Assert.Contains(document.Regions, region => region.Role == "price");
        Assert.Contains(document.Regions, region => region.Role == "add-to-cart/buy-now visual" && region.BehaviorContractRequirement == "runtime-business-behavior-required");
    }

    [Fact]
    public async Task Ecommerce_CartShellVisualDoesNotExecuteCartBusinessLogic()
    {
        var projectRoot = await CreateProjectAsync("cart-shell", "unknown section", []);

        var document = Assert.Single(await new EcommerceRegionClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None));

        Assert.Contains(document.Regions, region => region.Role == "unknown role" && region.PresentationOnly && region.Unsupported);
    }

    [Fact]
    public async Task Ecommerce_UnknownRoleIsValid()
    {
        var projectRoot = await CreateProjectAsync("unknown", "unknown section", []);

        var document = Assert.Single(await new EcommerceRegionClassifier(GetRepoRoot()).ClassifyAsync(projectRoot, CancellationToken.None));

        var region = Assert.Single(document.Regions);
        Assert.Equal("unknown role", region.Role);
        Assert.True(region.Unsupported);
        Assert.NotEmpty(region.Alternatives);
    }

    private static VisualComponentCandidate Candidate(string familyId, string family, IReadOnlyList<string> evidenceIds) =>
        new(
            familyId,
            family,
            familyId + "-default",
            0.7m,
            ["instance-" + familyId],
            Slots: [],
            TokenReferences: [],
            LocalOverrideIds: [],
            ResponsiveBehaviorRefs: [],
            InteractionBehaviorRefs: [],
            Alternatives: [],
            HumanReviewRequired: false,
            evidenceIds);

    private static async Task<string> CreateProjectAsync(
        string archetype,
        string sectionType,
        IReadOnlyList<VisualComponentCandidate> candidates)
    {
        var repoRoot = GetRepoRoot();
        var root = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "ecommerce-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "pages", "home"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "components"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "tokens"));
        var evidenceIds = candidates.SelectMany(candidate => candidate.EvidenceIds).DefaultIfEmpty("region-evidence").ToArray();
        var page = new PageArchetypeDocument("1.0", "page-archetype", "page-archetype-ecommerce-home", DateTimeOffset.UtcNow, "ecommerce", "home", archetype, 0.7m, evidenceIds, ["test"], Alternatives: []);
        var sections = new SectionsDraftDocument(
            "1.0",
            "sections",
            "sections-ecommerce-home",
            DateTimeOffset.UtcNow,
            "ecommerce",
            "home",
            [
                new SectionDraft("section-01", sectionType, 1, 0.7m, new SectionBounds(0, 0, 100, 100), ParentSectionId: null, ChildSectionIds: [], CrossViewportIdentityKey: sectionType, evidenceIds, ReasonCodes: ["test"])
            ],
            Issues: []);
        var interaction = new InteractionModelDocument("1.0", "interaction-model", "interaction-model-ecommerce-home", DateTimeOffset.UtcNow, "ecommerce", "home", Interactions: [], Issues: []);
        var componentDocument = new ComponentCandidatesDocument("1.0", "component-candidates", "component-candidates-ecommerce", DateTimeOffset.UtcNow, "ecommerce", candidates, Issues: []);
        var semantic = new SemanticTokenDocument(
            "1.0",
            "semantic-tokens",
            "semantic-tokens-ecommerce",
            DateTimeOffset.UtcNow,
            "ecommerce",
            "analysis/tokens/raw-design-tokens.json",
            [new SemanticToken("text-body", "typography", ["16px"], ["raw-body"], evidenceIds, 0.6m, ["test"], false)],
            PageLocalOverrides: [],
            ComponentLocalOverrides: [],
            HumanReviewRequired: false,
            ReviewReasons: []);
        await WriteAsync(root, "analysis/pages/home/page-archetype.json", page);
        await WriteAsync(root, "analysis/pages/home/sections.draft.json", sections);
        await WriteAsync(root, "analysis/pages/home/interaction-model.json", interaction);
        await WriteAsync(root, "analysis/components/component-candidates.json", componentDocument);
        await WriteAsync(root, "analysis/tokens/semantic-tokens.draft.json", semantic);
        return root;
    }

    private static async Task WriteAsync<T>(string root, string path, T artifact)
    {
        await File.WriteAllTextAsync(
            Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)),
            JsonSerializer.Serialize(artifact, VisualJson.Options) + Environment.NewLine);
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
