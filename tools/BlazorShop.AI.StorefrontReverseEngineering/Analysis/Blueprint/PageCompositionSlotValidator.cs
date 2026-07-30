using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;

public sealed class PageCompositionSlotValidator
{
    public PageCompositionSlotValidator(string repoRoot)
    {
        _ = Path.GetFullPath(repoRoot);
    }

    public IReadOnlyList<GenerationReadinessFinding> Validate(string projectRoot)
    {
        var root = Path.GetFullPath(projectRoot);
        var findings = new List<GenerationReadinessFinding>();
        var contracts = Read<StorefrontPageContractsDocument>(root, "analysis/storefront-pattern/page-contracts.json", findings);
        var compositions = Read<ReviewedPageCompositionsDocument>(root, "analysis/resolved/page-compositions.reviewed.json", findings);
        var mappings = Read<PresentationMappingsDocument>(root, "analysis/resolved/presentation-mappings.reviewed.json", findings);
        var catalog = Read<PresentationComponentCatalog>(root, "presentation-catalog/presentation-component-catalog.json", findings);

        if (contracts is null || compositions is null || mappings is null || catalog is null)
        {
            return findings;
        }

        var catalogByComponent = catalog.Components.ToDictionary(component => component.ComponentId, StringComparer.Ordinal);
        var catalogBySlot = catalog.Components
            .SelectMany(component => component.Slots.Select(slot => (Slot: slot, Component: component)))
            .GroupBy(pair => pair.Slot, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Component).ToArray(), StringComparer.Ordinal);
        var knownSlots = catalogBySlot.Keys.ToHashSet(StringComparer.Ordinal);
        var mappingsById = mappings.Mappings.ToDictionary(mapping => mapping.SourceCandidateId, StringComparer.Ordinal);

        foreach (var composition in compositions.Compositions)
        {
            var contract = MatchContract(contracts.Pages, composition.PageId, composition.PageArchetype);
            if (contract is null)
            {
                findings.Add(new GenerationReadinessFinding(
                    "unknown-page-archetype",
                    "blocking",
                    $"Page '{composition.PageId}' has no exact slot contract for archetype '{composition.PageArchetype}'.",
                    "analysis/storefront-pattern/page-contracts.json"));
                continue;
            }

            var observed = CollectObservedSlots(composition, mappings.Mappings, catalogBySlot, mappingsById);
            foreach (var missing in contract.RequiredSlotIds.Where(slot => !observed.Counts.ContainsKey(slot)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "missing-required-slot",
                    "blocking",
                    $"Page '{composition.PageId}' is missing required slot '{missing}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var slot in observed.Counts.Keys.Where(slot => !knownSlots.Contains(slot)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "unknown-slot",
                    "blocking",
                    $"Page '{composition.PageId}' references unknown slot '{slot}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var pair in observed.Counts.Where(pair => pair.Value > 1 && !contract.RepeatableSlotIds.Contains(pair.Key, StringComparer.Ordinal)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "duplicate-non-repeatable-slot",
                    "blocking",
                    $"Page '{composition.PageId}' repeats non-repeatable slot '{pair.Key}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            var allowed = contract.RequiredSlotIds
                .Concat(contract.OptionalSlotIds)
                .Concat(contract.RepeatableSlotIds)
                .Concat(contract.AllowedAdditionalSlotIds)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var slot in observed.Counts.Keys.Where(slot => knownSlots.Contains(slot) && !allowed.Contains(slot)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "unapproved-extra-section",
                    "blocking",
                    $"Page '{composition.PageId}' contains unapproved slot '{slot}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var node in Flatten(composition.SectionTree))
            {
                ValidateNodeTargets(composition, node, mappingsById, catalogByComponent, catalogBySlot, findings);
                ValidateBehaviorOwnership(composition, node, mappingsById, findings);
            }
        }

        return findings;
    }

    private static ObservedSlots CollectObservedSlots(
        PageComposition composition,
        IReadOnlyList<PresentationMapping> mappings,
        IReadOnlyDictionary<string, PresentationCatalogEntry[]> catalogBySlot,
        IReadOnlyDictionary<string, PresentationMapping> mappingsById)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(composition.TargetViewSlot))
        {
            AddPresence(counts, composition.TargetViewSlot);
        }

        var pageMappings = mappings
            .Where(mapping => string.Equals(mapping.SourcePageId, composition.PageId, StringComparison.Ordinal))
            .ToArray();
        foreach (var mapping in pageMappings)
        {
            AddPresence(counts, mapping.StarterSlotId);
        }

        var hasHomeContent = false;
        foreach (var node in Flatten(composition.SectionTree))
        {
            if (!string.IsNullOrWhiteSpace(node.ComponentMappingRef) && mappingsById.TryGetValue(node.ComponentMappingRef, out var mapping))
            {
                AddPresence(counts, mapping.StarterSlotId);
            }

            foreach (var slot in SlotsForTargetPath(node.TargetFilePath, catalogBySlot))
            {
                AddPresence(counts, slot);
            }

            var inferred = InferSlot(composition.PageArchetype, node.Role);
            if (inferred == "home.sections")
            {
                hasHomeContent = true;
            }
            else
            {
                if (inferred == "catalog.product-card")
                {
                    Add(counts, inferred);
                }
                else
                {
                    AddPresence(counts, inferred);
                }
            }
        }

        if (hasHomeContent)
        {
            AddPresence(counts, "home.sections");
        }

        return new ObservedSlots(counts);
    }

    private static void ValidateNodeTargets(
        PageComposition composition,
        PageCompositionNode node,
        IReadOnlyDictionary<string, PresentationMapping> mappingsById,
        IReadOnlyDictionary<string, PresentationCatalogEntry> catalogByComponent,
        IReadOnlyDictionary<string, PresentationCatalogEntry[]> catalogBySlot,
        List<GenerationReadinessFinding> findings)
    {
        if (string.IsNullOrWhiteSpace(node.TargetFilePath))
        {
            return;
        }

        if (ProtectedPathMarkers().Any(marker => node.TargetFilePath.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new GenerationReadinessFinding(
                "protected-path-target",
                "blocking",
                $"Page '{composition.PageId}' section '{node.NodeId}' targets protected path '{node.TargetFilePath}'.",
                "analysis/resolved/page-compositions.reviewed.json"));
            return;
        }

        var mappedSlotIds = new List<string>();
        if (!string.IsNullOrWhiteSpace(node.ComponentMappingRef) && mappingsById.TryGetValue(node.ComponentMappingRef, out var mapping))
        {
            Add(mappedSlotIds, mapping.StarterSlotId);
            if (catalogByComponent.TryGetValue(mapping.PresentationComponentId, out var component) &&
                component.ProtectedFilePatterns.Any(pattern => node.TargetFilePath.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "protected-path-target",
                    "blocking",
                    $"Page '{composition.PageId}' section '{node.NodeId}' targets protected pattern '{node.TargetFilePath}'.",
                    "analysis/resolved/presentation-mappings.reviewed.json"));
                return;
            }
        }

        mappedSlotIds.AddRange(SlotsForTargetPath(node.TargetFilePath, catalogBySlot));
        if (mappedSlotIds.Count > 0 &&
            mappedSlotIds.All(slot => catalogBySlot.TryGetValue(slot, out var components) &&
                components.All(component => component.AllowedFilePatterns.Count > 0 && !component.AllowedFilePatterns.Contains(node.TargetFilePath, StringComparer.Ordinal))))
        {
            findings.Add(new GenerationReadinessFinding(
                "slot-target-path-mismatch",
                "blocking",
                $"Page '{composition.PageId}' section '{node.NodeId}' target path does not match its approved slot target.",
                "analysis/resolved/page-compositions.reviewed.json"));
        }
    }

    private static void ValidateBehaviorOwnership(
        PageComposition composition,
        PageCompositionNode node,
        IReadOnlyDictionary<string, PresentationMapping> mappingsById,
        List<GenerationReadinessFinding> findings)
    {
        var behaviorTerms = new[] { "bff", "seo", "media", "cart", "checkout", "account", "auth", "payment", "functional-js", "commerce-node" };
        var ownsBehavior = node.AllowedOperations.Any(operation => behaviorTerms.Any(term => operation.Contains(term, StringComparison.OrdinalIgnoreCase)));
        if (!ownsBehavior &&
            (!string.IsNullOrWhiteSpace(node.ComponentMappingRef) &&
             mappingsById.TryGetValue(node.ComponentMappingRef, out var mapping) &&
             !string.Equals(mapping.BehaviorOwnership, "presentation", StringComparison.OrdinalIgnoreCase)))
        {
            ownsBehavior = true;
        }

        if (ownsBehavior)
        {
            findings.Add(new GenerationReadinessFinding(
                "slot-behavior-ownership-conflict",
                "blocking",
                $"Page '{composition.PageId}' section '{node.NodeId}' attempts to own protected storefront behavior.",
                "analysis/resolved/page-compositions.reviewed.json"));
        }
    }

    private T? Read<T>(string root, string relativePath, List<GenerationReadinessFinding> findings)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            findings.Add(new GenerationReadinessFinding("missing-required-artifact", "blocking", $"Required artifact is missing: {relativePath}", relativePath));
            return default;
        }

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), VisualJson.Options);
    }

    private static StorefrontPageContract? MatchContract(IReadOnlyList<StorefrontPageContract> contracts, string pageId, string archetype) =>
        contracts.FirstOrDefault(contract => string.Equals(contract.PageId, pageId, StringComparison.OrdinalIgnoreCase))
        ?? contracts.FirstOrDefault(contract => string.Equals(contract.StablePageArchetype, archetype, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<PageCompositionNode> Flatten(IEnumerable<PageCompositionNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children))
            {
                yield return child;
            }
        }
    }

    private static string? InferSlot(string pageArchetype, string role)
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

    private static IEnumerable<string> SlotsForTargetPath(string? targetPath, IReadOnlyDictionary<string, PresentationCatalogEntry[]> catalogBySlot)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            yield break;
        }

        foreach (var pair in catalogBySlot)
        {
            if (pair.Value.Any(component => component.AllowedFilePatterns.Contains(targetPath, StringComparer.Ordinal)))
            {
                yield return pair.Key;
            }
        }
    }

    private static IReadOnlyList<string> ProtectedPathMarkers() =>
        ["starter-generation.contract.yaml", "StorefrontPackageVersions.props", "BlazorShop.Storefront.Presentation"];

    private static void Add(Dictionary<string, int> counts, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        counts[value] = counts.GetValueOrDefault(value) + 1;
    }

    private static void AddPresence(Dictionary<string, int> counts, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || counts.ContainsKey(value))
        {
            return;
        }

        counts[value] = 1;
    }

    private static void Add(List<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value);
        }
    }

    private sealed record ObservedSlots(Dictionary<string, int> Counts);
}
