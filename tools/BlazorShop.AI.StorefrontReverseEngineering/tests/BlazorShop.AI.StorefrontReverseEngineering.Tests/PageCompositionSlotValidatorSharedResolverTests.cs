using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class PageCompositionSlotValidatorSharedResolverTests
{
    [Fact]
    public async Task RoleSuggestionDoesNotSatisfyRequiredSlotWithoutAuthoritativeResolution()
    {
        var projectRoot = Path.Combine(
            Phase3DNegativeReviewMutationTests.GetRepoRoot(),
            "obj",
            "storefront-reverse-engineering",
            "projects",
            "phase3e-validator-slot-" + Guid.NewGuid().ToString("N"));
        var node = Node("section-04", "purchase actions", mappingId: null, targetPath: null);
        await WriteJsonAsync(projectRoot, "analysis/storefront-pattern/page-contracts.json", new StorefrontPageContractsDocument(
            "1.0",
            "page-contracts",
            "page-contracts",
            DateTimeOffset.UtcNow,
            [Contract("product", "product-detail", ["product.purchase"])]));
        await WriteJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", new ReviewedPageCompositionsDocument(
            "1.0",
            "reviewed-page-compositions",
            "page-compositions",
            DateTimeOffset.UtcNow,
            "phase3e-validator",
            new ReviewedPageCompositionProvenance("analysis/resolved/review-resolution-manifest.json", "hash", new Dictionary<string, string>(StringComparer.Ordinal), [], new Dictionary<string, string>(StringComparer.Ordinal)),
            new SiteBlueprint("site", [], "store", new Dictionary<string, string>(StringComparer.Ordinal), [], [], ["product"], []),
            [new PageBlueprint("product", "product-detail", "https://example.test/product", [], [], [], [], [node], new Dictionary<string, string>(StringComparer.Ordinal), null, null, [])],
            [new PageComposition("product", "product-detail", null, [node], [], [], [], [], [])]));
        await WriteJsonAsync(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json", new PresentationMappingsDocument(
            "1.0",
            "presentation-mappings",
            "presentation-mappings",
            DateTimeOffset.UtcNow,
            "phase3e-validator",
            []));
        await WriteJsonAsync(projectRoot, "presentation-catalog/presentation-component-catalog.json", new PresentationComponentCatalog(
            "1.0",
            "presentation-component-catalog",
            "presentation-component-catalog",
            DateTimeOffset.UtcNow,
            [Catalog("product.purchase", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor")],
            []));

        var findings = new PageCompositionSlotValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "required-slot-unmapped" && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Code == "section-slot-suggestion-unreviewed" && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OrphanReviewedMappingDoesNotSatisfyRequiredSlot()
    {
        var projectRoot = CreateProjectRoot();
        await WriteMinimalSlotArtifactsAsync(
            projectRoot,
            [Node("unrelated-section", "editorial", mappingId: null, targetPath: null)],
            [Mapping("orphan-purchase", "product", "missing-section", "product.purchase", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor")]);

        var findings = new PageCompositionSlotValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "reviewed-slot-mapping-orphan" && finding.Message.Contains("missing-section", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Code == "missing-required-slot" && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NodeReferencedReviewedMappingMustBelongToThatNode()
    {
        var projectRoot = CreateProjectRoot();
        await WriteMinimalSlotArtifactsAsync(
            projectRoot,
            [Node("actual-section", "purchase actions", mappingId: "wrong-section-purchase", targetPath: "Components/Catalog/PurchasePanelPlaceholder.razor")],
            [Mapping("wrong-section-purchase", "product", "different-section", "product.purchase", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor")]);

        var findings = new PageCompositionSlotValidator(Phase3DNegativeReviewMutationTests.GetRepoRoot()).Validate(projectRoot);

        Assert.Contains(findings, finding => finding.Code == "reviewed-slot-mapping-orphan" && finding.Message.Contains("different-section", StringComparison.Ordinal));
        Assert.Contains(findings, finding => finding.Code == "required-slot-unmapped" && finding.Message.Contains("product.purchase", StringComparison.Ordinal));
    }

    private static string CreateProjectRoot()
    {
        var projectRoot = Path.Combine(
            Phase3DNegativeReviewMutationTests.GetRepoRoot(),
            "obj",
            "storefront-reverse-engineering",
            "projects",
            "phase3e-validator-slot-" + Guid.NewGuid().ToString("N"));
        Phase3TempPathRegistry.Register(projectRoot);
        return projectRoot;
    }

    private static async Task WriteMinimalSlotArtifactsAsync(
        string projectRoot,
        IReadOnlyList<PageCompositionNode> nodes,
        IReadOnlyList<PresentationMapping> mappings)
    {
        await WriteJsonAsync(projectRoot, "analysis/storefront-pattern/page-contracts.json", new StorefrontPageContractsDocument(
            "1.0",
            "page-contracts",
            "page-contracts",
            DateTimeOffset.UtcNow,
            [Contract("product", "product-detail", ["product.purchase"])]));
        await WriteJsonAsync(projectRoot, "analysis/resolved/page-compositions.reviewed.json", new ReviewedPageCompositionsDocument(
            "1.0",
            "reviewed-page-compositions",
            "page-compositions",
            DateTimeOffset.UtcNow,
            "phase3e-validator",
            new ReviewedPageCompositionProvenance("analysis/resolved/review-resolution-manifest.json", "hash", new Dictionary<string, string>(StringComparer.Ordinal), [], new Dictionary<string, string>(StringComparer.Ordinal)),
            new SiteBlueprint("site", [], "store", new Dictionary<string, string>(StringComparer.Ordinal), [], [], ["product"], []),
            [new PageBlueprint("product", "product-detail", "https://example.test/product", [], [], [], [], nodes, new Dictionary<string, string>(StringComparer.Ordinal), null, null, [])],
            [new PageComposition("product", "product-detail", null, nodes, [], [], [], [], [])]));
        await WriteJsonAsync(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json", new PresentationMappingsDocument(
            "1.0",
            "presentation-mappings",
            "presentation-mappings",
            DateTimeOffset.UtcNow,
            "phase3e-validator",
            mappings));
        await WriteJsonAsync(projectRoot, "presentation-catalog/presentation-component-catalog.json", new PresentationComponentCatalog(
            "1.0",
            "presentation-component-catalog",
            "presentation-component-catalog",
            DateTimeOffset.UtcNow,
            [Catalog("product.purchase", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor")],
            []));
    }

    private static async Task WriteJsonAsync(string projectRoot, string relativePath, object value)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, VisualJson.Options) + Environment.NewLine);
    }

    private static PageCompositionNode Node(string nodeId, string role, string? mappingId, string? targetPath) =>
        new(
            nodeId,
            role,
            mappingId,
            [],
            [],
            nodeId + "-fingerprint",
            null,
            null,
            [],
            new Dictionary<string, string>(StringComparer.Ordinal),
            [],
            mappingId,
            targetPath,
            null,
            [],
            [],
            [],
            [],
            null,
            null,
            null,
            [],
            [],
            []);

    private static PresentationMapping Mapping(
        string mappingId,
        string pageId,
        string sectionId,
        string componentId,
        string slotId,
        string targetPath) =>
        new(
            mappingId,
            componentId,
            slotId,
            "default",
            [],
            [],
            [],
            [],
            [],
            "presentation",
            0.95m,
            ["ev-desktop-1440-001"],
            "test reviewed mapping",
            [],
            false,
            pageId,
            sectionId,
            sectionId,
            "product-detail",
            targetPath,
            "catalog-components",
            "presentation",
            ["test"],
            "Approved");

    private static StorefrontPageContract Contract(string pageId, string pageArchetype, IReadOnlyList<string> requiredSlots) =>
        new(
            pageId,
            pageArchetype,
            "presentation",
            [],
            requiredSlots,
            [],
            [],
            requiredSlots,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private static PresentationCatalogEntry Catalog(string componentId, string slotId, string targetPath) =>
        new(
            componentId,
            "visual generation target",
            [],
            [],
            [slotId],
            ["default"],
            [],
            [],
            [],
            "presentation",
            true,
            false,
            true,
            false,
            [],
            [],
            [],
            [],
            "1.0",
            "visual",
            ["presentation"],
            [targetPath],
            [],
            [],
            "none");
}
