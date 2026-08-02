using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class SectionSlotResolverTests
{
    [Fact]
    public void ProductPurchaseSectionUsesReviewedMappingSlot()
    {
        var node = Node("section-04", "purchase actions", mappingId: "product-purchase", targetPath: "Components/Catalog/PurchasePanelPlaceholder.razor");
        var composition = Composition("product", "product-detail", node);
        var resolver = new SectionSlotResolver(
            [Mapping("product-purchase", "product", "section-04", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor")],
            [Catalog("product.purchase", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor")]);

        var resolution = resolver.Resolve(composition, node, Contract("product", "product-detail", ["product.purchase"]));

        Assert.True(resolution.HasAuthoritativeSlot);
        Assert.Equal("product.purchase", resolution.StarterSlotId);
        Assert.Equal(SectionSlotResolver.ReviewedPresentationMappingSource, resolution.SlotSource);
        Assert.Equal("product-purchase", resolution.MappingId);
        Assert.Equal("product", resolution.SourcePageId);
        Assert.Equal("section-04", resolution.SourceSectionId);
        Assert.Equal("Components/Catalog/PurchasePanelPlaceholder.razor", resolution.TargetPath);
        Assert.Equal("product.purchase", resolution.SuggestedSlotId);
        Assert.Null(resolution.Problem);
    }

    [Fact]
    public void AmbiguousRoleTextIsDiagnosticsOnly()
    {
        var node = Node("section-04", "purchase actions", mappingId: null, targetPath: null);
        var composition = Composition("product", "product-detail", node);
        var resolver = new SectionSlotResolver([], []);

        var resolution = resolver.Resolve(composition, node, Contract("product", "product-detail", ["product.purchase"]));

        Assert.False(resolution.HasAuthoritativeSlot);
        Assert.Null(resolution.StarterSlotId);
        Assert.Equal(SectionSlotResolver.UnresolvedSource, resolution.SlotSource);
        Assert.Null(resolution.MappingId);
        Assert.Equal("product.purchase", resolution.SuggestedSlotId);
        Assert.Equal("section-slot-unresolved", resolution.Problem?.Code);
    }

    [Fact]
    public void ExactCatalogTargetIsAuthoritativeWithoutRoleInference()
    {
        var node = Node("section-02", "editorial block", mappingId: null, targetPath: "Components/Catalog/ProductSummaryCard.razor");
        var composition = Composition("category", "product-listing", node);
        var resolver = new SectionSlotResolver(
            [],
            [Catalog("catalog.product-card", "catalog.product-card", "Components/Catalog/ProductSummaryCard.razor")]);

        var resolution = resolver.Resolve(composition, node, Contract("category", "product-listing", ["catalog.product-card"]));

        Assert.True(resolution.HasAuthoritativeSlot);
        Assert.Equal("catalog.product-card", resolution.StarterSlotId);
        Assert.Equal(SectionSlotResolver.ExactStorefrontContractSource, resolution.SlotSource);
        Assert.Null(resolution.MappingId);
        Assert.Null(resolution.Problem);
    }

    [Fact]
    public void ApprovedVisualExtensionRecordsReviewedExtensionSource()
    {
        var node = Node(
            "section-related",
            "related products",
            mappingId: null,
            targetPath: "Components/Catalog/RelatedProducts.razor",
            targetZone: "product.related-products",
            approvedVisualExtensionId: "visual-extension-related-products",
            approvedVisualExtensionReason: "Approved visual-only merchandising extension.");
        var composition = Composition("product", "product-detail", node);
        var resolver = new SectionSlotResolver([], []);

        var resolution = resolver.Resolve(composition, node, Contract("product", "product-detail", ["product.purchase"], ["product.related-products"]));

        Assert.True(resolution.HasAuthoritativeSlot);
        Assert.Equal("product.related-products", resolution.StarterSlotId);
        Assert.Equal(SectionSlotResolver.ApprovedVisualExtensionSource, resolution.SlotSource);
        Assert.Null(resolution.MappingId);
        Assert.Equal("product.related-products", resolution.SuggestedSlotId);
        Assert.Null(resolution.Problem);
    }

    private static PageComposition Composition(string pageId, string pageArchetype, PageCompositionNode node) =>
        new(
            pageId,
            pageArchetype,
            null,
            [node],
            [],
            [],
            [],
            [],
            []);

    private static PageCompositionNode Node(
        string nodeId,
        string role,
        string? mappingId,
        string? targetPath,
        string targetZone = "catalog-components",
        string? approvedVisualExtensionId = null,
        string? approvedVisualExtensionReason = null) =>
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
            targetZone,
            [],
            [],
            [],
            [],
            approvedVisualExtensionId,
            approvedVisualExtensionReason,
            null,
            [],
            [],
            []);

    private static PresentationMapping Mapping(string mappingId, string pageId, string sectionId, string slotId, string targetPath) =>
        new(
            mappingId,
            slotId,
            slotId,
            "default",
            [],
            [],
            [],
            [],
            [],
            "presentation",
            0.95m,
            [],
            "test mapping",
            [],
            false,
            pageId,
            sectionId,
            sectionId,
            pageId,
            targetPath,
            "catalog-components",
            "presentation",
            [],
            "Approved");

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

    private static StorefrontPageContract Contract(
        string pageId,
        string pageArchetype,
        IReadOnlyList<string> requiredSlots,
        IReadOnlyList<string>? additionalSlots = null) =>
        new(
            pageId,
            pageArchetype,
            "presentation",
            [],
            requiredSlots.Concat(additionalSlots ?? []).ToArray(),
            [],
            [],
            requiredSlots,
            [],
            [],
            additionalSlots ?? [],
            [],
            [],
            [],
            [],
            []);
}
