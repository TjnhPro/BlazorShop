using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record SectionSlotResolution(
    string SourcePageId,
    string SourceSectionId,
    string? StarterSlotId,
    string SlotSource,
    string? MappingId,
    string? TargetPath,
    string? SuggestedSlotId,
    SectionSlotResolutionProblem? Problem)
{
    public bool HasAuthoritativeSlot =>
        !string.IsNullOrWhiteSpace(StarterSlotId) &&
        SectionSlotResolver.IsAuthoritativeSource(SlotSource);
}

public sealed record SectionSlotResolutionProblem(
    string Code,
    string Problem,
    string Cause,
    string FixSuggestion);

public sealed class SectionSlotResolver
{
    public const string ReviewedPresentationMappingSource = "reviewed-presentation-mapping";
    public const string ExactStorefrontContractSource = "exact-storefront-contract";
    public const string ApprovedVisualExtensionSource = "approved-visual-extension";
    public const string UnresolvedSource = "unresolved";

    private readonly IReadOnlyDictionary<string, PresentationMapping> mappingsById;
    private readonly IReadOnlyDictionary<string, PresentationCatalogEntry> catalogByComponent;
    private readonly IReadOnlyDictionary<string, PresentationCatalogEntry[]> catalogBySlot;

    public SectionSlotResolver(
        IEnumerable<PresentationMapping> mappings,
        IEnumerable<PresentationCatalogEntry> catalogEntries)
    {
        mappingsById = mappings.ToDictionary(mapping => mapping.SourceCandidateId, StringComparer.Ordinal);
        var entries = catalogEntries.ToArray();
        catalogByComponent = entries.ToDictionary(component => component.ComponentId, StringComparer.Ordinal);
        catalogBySlot = entries
            .SelectMany(component => component.Slots.Select(slot => (Slot: slot, Component: component)))
            .GroupBy(pair => pair.Slot, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Component).ToArray(), StringComparer.Ordinal);
    }

    public SectionSlotResolution Resolve(
        PageComposition composition,
        PageCompositionNode node,
        StorefrontPageContract? contract)
    {
        var suggested = SuggestSlotFromRole(composition.PageArchetype, node.Role);
        var mappingId = FirstNonEmpty(node.ComponentMappingRef, node.PresentationMappingId);
        if (!string.IsNullOrWhiteSpace(mappingId))
        {
            return ResolveReviewedMapping(composition, node, mappingId, suggested);
        }

        if (!string.IsNullOrWhiteSpace(node.ApprovedVisualExtensionId))
        {
            var extensionSlot = ResolveApprovedVisualExtensionSlot(node, contract);
            if (!string.IsNullOrWhiteSpace(extensionSlot) &&
                !string.IsNullOrWhiteSpace(node.ApprovedVisualExtensionReason) &&
                node.ProtectedBehaviorMarkers.Count == 0)
            {
                return new SectionSlotResolution(
                    composition.PageId,
                    node.NodeId,
                    extensionSlot,
                    ApprovedVisualExtensionSource,
                    null,
                    node.TargetFilePath,
                    suggested,
                    null);
            }

            return Unresolved(
                composition,
                node,
                mappingId,
                node.TargetFilePath,
                suggested,
                "approved-visual-extension-slot-unreviewed",
                $"Section '{node.NodeId}' declares an approved visual extension without an explicit reviewed slot.",
                "Approved visual extensions must carry an approved ID, reason, visual-only boundary, and resolvable Storefront slot.",
                "Record an explicit approved visual extension slot or remove the extension marker.");
        }

        var exactSlot = ExactSlotForTargetPath(node.TargetFilePath);
        if (!string.IsNullOrWhiteSpace(exactSlot))
        {
            return new SectionSlotResolution(
                composition.PageId,
                node.NodeId,
                exactSlot,
                ExactStorefrontContractSource,
                null,
                node.TargetFilePath,
                suggested,
                null);
        }

        return Unresolved(
            composition,
            node,
            mappingId,
            node.TargetFilePath,
            suggested,
            "section-slot-unresolved",
            $"Section '{node.NodeId}' has no authoritative Storefront slot.",
            "No reviewed presentation mapping, exact Storefront catalog target, or approved visual extension resolved this section.",
            "Add or approve a presentation mapping, target an exact Storefront slot contract, or record an approved visual extension.");
    }

    public static bool IsAuthoritativeSource(string? slotSource) =>
        string.Equals(slotSource, ReviewedPresentationMappingSource, StringComparison.Ordinal) ||
        string.Equals(slotSource, ExactStorefrontContractSource, StringComparison.Ordinal) ||
        string.Equals(slotSource, ApprovedVisualExtensionSource, StringComparison.Ordinal);

    public static bool ContractAllowsSlot(StorefrontPageContract contract, string slot) =>
        contract.RequiredSlotIds.Contains(slot, StringComparer.Ordinal) ||
        contract.OptionalSlotIds.Contains(slot, StringComparer.Ordinal) ||
        contract.RepeatableSlotIds.Contains(slot, StringComparer.Ordinal) ||
        contract.AllowedAdditionalSlotIds.Contains(slot, StringComparer.Ordinal);

    public static string? SuggestSlotFromRole(string pageArchetype, string role)
    {
        if (role.Contains("header", StringComparison.OrdinalIgnoreCase)) return "layout.header";
        if (role.Contains("footer", StringComparison.OrdinalIgnoreCase)) return "layout.footer";
        if (role.Contains("navigation", StringComparison.OrdinalIgnoreCase) || role.Contains("nav", StringComparison.OrdinalIgnoreCase)) return "layout.main-navigation";
        if (role.Contains("product card", StringComparison.OrdinalIgnoreCase)) return "catalog.product-card";
        if (role.Contains("filter", StringComparison.OrdinalIgnoreCase)) return "catalog.filters";
        if (role.Contains("sort", StringComparison.OrdinalIgnoreCase)) return "catalog.sorting";
        if (role.Contains("pagination", StringComparison.OrdinalIgnoreCase)) return "catalog.pagination";
        if (role.Contains("gallery", StringComparison.OrdinalIgnoreCase) || role.Contains("media gallery", StringComparison.OrdinalIgnoreCase)) return "product.gallery";
        if (role.Contains("purchase", StringComparison.OrdinalIgnoreCase) || role.Contains("add-to-cart", StringComparison.OrdinalIgnoreCase)) return "product.purchase";
        if (role.Contains("information", StringComparison.OrdinalIgnoreCase) || role.Contains("description", StringComparison.OrdinalIgnoreCase)) return "product.information";
        if (role.Contains("review", StringComparison.OrdinalIgnoreCase)) return "product.reviews";
        if (role.Contains("related", StringComparison.OrdinalIgnoreCase)) return "product.related-products";
        if (role.Contains("cart", StringComparison.OrdinalIgnoreCase)) return "cart.page";
        if (role.Contains("checkout", StringComparison.OrdinalIgnoreCase)) return "checkout.page";
        if (role.Contains("account", StringComparison.OrdinalIgnoreCase) || role.Contains("auth", StringComparison.OrdinalIgnoreCase)) return "account.shell";
        if (role.Contains("error", StringComparison.OrdinalIgnoreCase) || role.Contains("not found", StringComparison.OrdinalIgnoreCase) || role.Contains("maintenance", StringComparison.OrdinalIgnoreCase)) return "system.error";
        return pageArchetype.Contains("home", StringComparison.OrdinalIgnoreCase) ? "home.sections" : null;
    }

    private SectionSlotResolution ResolveReviewedMapping(
        PageComposition composition,
        PageCompositionNode node,
        string mappingId,
        string? suggested)
    {
        if (!mappingsById.TryGetValue(mappingId, out var mapping))
        {
            return Unresolved(
                composition,
                node,
                mappingId,
                node.TargetFilePath,
                suggested,
                "section-slot-mapping-missing",
                $"Section '{node.NodeId}' references missing reviewed mapping '{mappingId}'.",
                "The page composition points at a mapping that is absent from the reviewed Presentation mappings.",
                "Restore the reviewed mapping or clear the section mapping reference.");
        }

        var targetPath = string.IsNullOrWhiteSpace(mapping.TargetGeneratedPath) ? node.TargetFilePath : mapping.TargetGeneratedPath;
        if (string.IsNullOrWhiteSpace(mapping.StarterSlotId) ||
            string.IsNullOrWhiteSpace(mapping.PresentationComponentId) ||
            !catalogByComponent.TryGetValue(mapping.PresentationComponentId, out var component) ||
            !component.Slots.Contains(mapping.StarterSlotId, StringComparer.Ordinal) ||
            !catalogBySlot.ContainsKey(mapping.StarterSlotId))
        {
            return Unresolved(
                composition,
                node,
                mapping.SourceCandidateId,
                targetPath,
                suggested,
                "section-slot-mapping-invalid",
                $"Reviewed mapping '{mapping.SourceCandidateId}' does not resolve an exact Storefront slot.",
                "The mapping is missing a starter slot or targets a component/catalog slot that does not expose that slot.",
                "Fix the reviewed Presentation mapping so it names a catalog-backed starter slot.");
        }

        return new SectionSlotResolution(
            mapping.SourcePageId,
            mapping.SourceSectionId,
            mapping.StarterSlotId,
            ReviewedPresentationMappingSource,
            mapping.SourceCandidateId,
            targetPath,
            suggested,
            null);
    }

    private string? ResolveApprovedVisualExtensionSlot(PageCompositionNode node, StorefrontPageContract? contract)
    {
        if (contract is not null && !string.IsNullOrWhiteSpace(node.TargetGeneratedZone))
        {
            if (ContractAllowsSlot(contract, node.TargetGeneratedZone))
            {
                return node.TargetGeneratedZone;
            }

            var fromZone = contract.AllowedAdditionalSlotIds
                .FirstOrDefault(slot => node.TargetGeneratedZone.Contains(slot.Split('.').Last(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(fromZone))
            {
                return fromZone;
            }
        }

        return ExactSlotForTargetPath(node.TargetFilePath);
    }

    private string? ExactSlotForTargetPath(string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return null;
        }

        var slots = catalogBySlot
            .Where(pair => pair.Value.Any(component => component.AllowedFilePatterns.Contains(targetPath, StringComparer.Ordinal)))
            .Select(pair => pair.Key)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return slots.Length == 1 ? slots[0] : null;
    }

    private static SectionSlotResolution Unresolved(
        PageComposition composition,
        PageCompositionNode node,
        string? mappingId,
        string? targetPath,
        string? suggested,
        string code,
        string problem,
        string cause,
        string fixSuggestion) =>
        new(
            composition.PageId,
            node.NodeId,
            null,
            UnresolvedSource,
            mappingId,
            targetPath,
            suggested,
            new SectionSlotResolutionProblem(code, problem, cause, fixSuggestion));

    private static string? FirstNonEmpty(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first) ? first :
        !string.IsNullOrWhiteSpace(second) ? second :
        null;
}
