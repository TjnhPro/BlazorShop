using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
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
        var slotResolver = new SectionSlotResolver(mappings.Mappings, catalog.Components);

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

            var sectionNodeIds = Flatten(composition.SectionTree).Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);
            var allowed = contract.RequiredSlotIds
                .Concat(contract.OptionalSlotIds)
                .Concat(contract.RepeatableSlotIds)
                .Concat(contract.AllowedAdditionalSlotIds)
                .ToHashSet(StringComparer.Ordinal);
            AddReviewedMappingSourceFindings(composition, mappings.Mappings, sectionNodeIds, findings);
            var observed = CollectObservedSlots(composition, contract, mappings.Mappings, slotResolver, sectionNodeIds);
            foreach (var missing in contract.RequiredSlotIds.Where(slot => !observed.Sources.ContainsKey(slot)))
            {
                var suggested = observed.Suggestions.Any(suggestion => string.Equals(suggestion.SlotId, missing, StringComparison.Ordinal));
                findings.Add(new GenerationReadinessFinding(
                    suggested ? "required-slot-unmapped" : "missing-required-slot",
                    "blocking",
                    suggested
                        ? $"Page '{composition.PageId}' has only unreviewed role suggestions for required slot '{missing}'."
                        : $"Page '{composition.PageId}' is missing required slot '{missing}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var suggestion in observed.Suggestions.Where(suggestion => !string.IsNullOrWhiteSpace(suggestion.SlotId) && !observed.Sources.ContainsKey(suggestion.SlotId)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "section-slot-suggestion-unreviewed",
                    "warning",
                    $"Page '{composition.PageId}' section '{suggestion.SectionNodeId}' role suggests slot '{suggestion.SlotId}' but no reviewed mapping exists.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var slot in observed.Sources.Keys.Where(slot => !knownSlots.Contains(slot)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "unknown-slot",
                    "blocking",
                    $"Page '{composition.PageId}' references unknown slot '{slot}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var pair in observed.Sources.Where(pair => pair.Value.Where(source => source.SourceKind != "page-target").Select(SourceIdentity).Distinct(StringComparer.Ordinal).Count() > 1 && !contract.RepeatableSlotIds.Contains(pair.Key, StringComparer.Ordinal)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "duplicate-non-repeatable-slot",
                    "blocking",
                    $"Page '{composition.PageId}' repeats non-repeatable slot '{pair.Key}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var slot in observed.Sources.Keys.Where(slot => knownSlots.Contains(slot) && !allowed.Contains(slot)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "unapproved-extra-section",
                    "blocking",
                    $"Page '{composition.PageId}' contains unapproved slot '{slot}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

            foreach (var node in Flatten(composition.SectionTree))
            {
                ValidateExtraSection(composition, node, observed, allowed, findings);
                ValidateNodeTargets(composition, node, mappingsById, catalogByComponent, catalogBySlot, findings);
                ValidateBehaviorOwnership(composition, node, mappingsById, findings);
            }
        }

        return findings;
    }

    private static ObservedSlots CollectObservedSlots(
        PageComposition composition,
        StorefrontPageContract contract,
        IReadOnlyList<PresentationMapping> mappings,
        SectionSlotResolver slotResolver,
        IReadOnlySet<string> sectionNodeIds)
    {
        var sources = new Dictionary<string, HashSet<SlotObservationSource>>(StringComparer.Ordinal);
        var suggestions = new List<SlotObservationSource>();
        if (!string.IsNullOrWhiteSpace(composition.TargetViewSlot) && SectionSlotResolver.ContractAllowsSlot(contract, composition.TargetViewSlot))
        {
            AddObservation(
                sources,
                new SlotObservationSource(
                    "page-target",
                    "page:" + composition.PageId,
                    composition.PageId,
                    null,
                    null,
                    composition.TargetViewSlot,
                        null));
        }

        foreach (var mapping in mappings.Where(mapping => string.Equals(mapping.SourcePageId, composition.PageId, StringComparison.Ordinal)))
        {
            if (!sectionNodeIds.Contains(mapping.SourceSectionId))
            {
                continue;
            }

            AddObservation(
                sources,
                new SlotObservationSource(
                    SectionSlotResolver.ReviewedPresentationMappingSource,
                    mapping.SourceCandidateId,
                    composition.PageId,
                    mapping.SourceSectionId,
                    mapping.SourceCandidateId,
                    mapping.StarterSlotId,
                    mapping.TargetGeneratedPath));
        }

        foreach (var node in Flatten(composition.SectionTree))
        {
            var resolution = slotResolver.Resolve(composition, node, contract);
            if (resolution.HasAuthoritativeSlot &&
                (resolution.SlotSource != SectionSlotResolver.ReviewedPresentationMappingSource ||
                    (string.Equals(resolution.SourcePageId, composition.PageId, StringComparison.Ordinal) &&
                        sectionNodeIds.Contains(resolution.SourceSectionId))))
            {
                if (!IsPageTargetVisualExtension(composition, resolution))
                {
                    AddObservation(
                        sources,
                        new SlotObservationSource(
                            resolution.SlotSource,
                            resolution.MappingId ?? resolution.SourceSectionId + ":" + resolution.StarterSlotId,
                            resolution.SourcePageId,
                            resolution.SourceSectionId,
                            resolution.MappingId,
                            resolution.StarterSlotId,
                            resolution.TargetPath));
                }
            }

            if (!string.IsNullOrWhiteSpace(resolution.SuggestedSlotId))
            {
                suggestions.Add(new SlotObservationSource(
                    "role-suggestion",
                    node.NodeId + ":" + resolution.SuggestedSlotId,
                    composition.PageId,
                    node.NodeId,
                    node.ComponentMappingRef,
                    resolution.SuggestedSlotId,
                    node.TargetFilePath));
            }
        }

        return new ObservedSlots(sources, suggestions);
    }

    private static void AddReviewedMappingSourceFindings(
        PageComposition composition,
        IEnumerable<PresentationMapping> mappings,
        IReadOnlySet<string> sectionNodeIds,
        List<GenerationReadinessFinding> findings)
    {
        foreach (var mapping in mappings.Where(mapping => string.Equals(mapping.SourcePageId, composition.PageId, StringComparison.Ordinal)))
        {
            if (sectionNodeIds.Contains(mapping.SourceSectionId))
            {
                continue;
            }

            findings.Add(new GenerationReadinessFinding(
                "reviewed-slot-mapping-orphan",
                "blocking",
                $"Page '{composition.PageId}' reviewed mapping '{mapping.SourceCandidateId}' points at missing section '{mapping.SourceSectionId}'.",
                "analysis/resolved/presentation-mappings.reviewed.json"));
        }
    }

    private static void ValidateExtraSection(
        PageComposition composition,
        PageCompositionNode node,
        ObservedSlots observed,
        IReadOnlySet<string> allowed,
        List<GenerationReadinessFinding> findings)
    {
        var nodeSources = observed.Sources
            .Where(pair => allowed.Contains(pair.Key))
            .SelectMany(pair => pair.Value)
            .Where(source => string.Equals(source.SectionNodeId, node.NodeId, StringComparison.Ordinal))
            .ToArray();
        if (nodeSources.Length > 0)
        {
            return;
        }

        if (IsValidPageTargetVisualExtension(composition, node, allowed))
        {
            return;
        }

        findings.Add(new GenerationReadinessFinding(
            "unapproved-extra-section",
            "blocking",
            $"Page '{composition.PageId}' section '{node.NodeId}' has no reviewed slot mapping or approved visual extension.",
            "analysis/resolved/page-compositions.reviewed.json"));
    }

    private static void ValidateNodeTargets(
        PageComposition composition,
        PageCompositionNode node,
        IReadOnlyDictionary<string, PresentationMapping> mappingsById,
        IReadOnlyDictionary<string, PresentationCatalogEntry> catalogByComponent,
        IReadOnlyDictionary<string, PresentationCatalogEntry[]> catalogBySlot,
        List<GenerationReadinessFinding> findings)
    {
        PresentationMapping? mapping = null;
        if (!string.IsNullOrWhiteSpace(node.ComponentMappingRef) && !mappingsById.TryGetValue(node.ComponentMappingRef, out mapping))
        {
            findings.Add(new GenerationReadinessFinding(
                "invalid-section-slot-mapping",
                "blocking",
                $"Page '{composition.PageId}' section '{node.NodeId}' references missing mapping '{node.ComponentMappingRef}'.",
                "analysis/resolved/page-compositions.reviewed.json"));
            return;
        }

        if (mapping is not null)
        {
            if (!string.Equals(mapping.SourcePageId, composition.PageId, StringComparison.Ordinal) ||
                !string.Equals(mapping.SourceSectionId, node.NodeId, StringComparison.Ordinal))
            {
                findings.Add(new GenerationReadinessFinding(
                    "reviewed-slot-mapping-orphan",
                    "blocking",
                    $"Page '{composition.PageId}' section '{node.NodeId}' references reviewed mapping '{mapping.SourceCandidateId}' for page '{mapping.SourcePageId}' section '{mapping.SourceSectionId}'.",
                    "analysis/resolved/presentation-mappings.reviewed.json"));
                return;
            }

            if (string.IsNullOrWhiteSpace(mapping.StarterSlotId) ||
                string.IsNullOrWhiteSpace(mapping.PresentationComponentId) ||
                !catalogByComponent.TryGetValue(mapping.PresentationComponentId, out var mappedComponent) ||
                !mappedComponent.Slots.Contains(mapping.StarterSlotId, StringComparer.Ordinal) ||
                !catalogBySlot.ContainsKey(mapping.StarterSlotId))
            {
                findings.Add(new GenerationReadinessFinding(
                    "invalid-section-slot-mapping",
                    "blocking",
                    $"Page '{composition.PageId}' section '{node.NodeId}' has invalid reviewed mapping '{node.ComponentMappingRef}'.",
                    "analysis/resolved/presentation-mappings.reviewed.json"));
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(node.TargetFilePath))
        {
            if (mapping is not null)
            {
                findings.Add(new GenerationReadinessFinding(
                    "slot-target-path-mismatch",
                    "blocking",
                    $"Page '{composition.PageId}' section '{node.NodeId}' has no target path for reviewed slot '{mapping.StarterSlotId}'.",
                    "analysis/resolved/page-compositions.reviewed.json"));
            }

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
        if (mapping is not null)
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

    private static bool IsPageTargetVisualExtension(PageComposition composition, SectionSlotResolution resolution) =>
        string.Equals(resolution.SlotSource, SectionSlotResolver.ApprovedVisualExtensionSource, StringComparison.Ordinal) &&
        !string.IsNullOrWhiteSpace(composition.TargetViewSlot) &&
        string.Equals(resolution.StarterSlotId, composition.TargetViewSlot, StringComparison.Ordinal);

    private static bool IsValidPageTargetVisualExtension(
        PageComposition composition,
        PageCompositionNode node,
        IReadOnlySet<string> allowed) =>
        !string.IsNullOrWhiteSpace(composition.TargetViewSlot) &&
        allowed.Contains(composition.TargetViewSlot) &&
        string.Equals(node.TargetGeneratedZone, composition.TargetViewSlot, StringComparison.Ordinal) &&
        !string.IsNullOrWhiteSpace(node.ApprovedVisualExtensionId) &&
        !string.IsNullOrWhiteSpace(node.ApprovedVisualExtensionReason) &&
        node.ProtectedBehaviorMarkers.Count == 0 &&
        node.AllowedOperations.All(IsVisualOnlyOperation);

    private static bool IsVisualOnlyOperation(string operation) =>
        string.IsNullOrWhiteSpace(operation) ||
        operation.Contains("visual", StringComparison.OrdinalIgnoreCase) ||
        operation.Contains("responsive", StringComparison.OrdinalIgnoreCase);

    private static string SourceIdentity(SlotObservationSource source) =>
        !string.IsNullOrWhiteSpace(source.SectionNodeId) ? source.SectionNodeId! :
        source.SourceKind == SectionSlotResolver.ReviewedPresentationMappingSource && !string.IsNullOrWhiteSpace(source.SlotId) ? "page-level:" + source.SlotId :
        source.SourceId;

    private static void AddObservation(Dictionary<string, HashSet<SlotObservationSource>> sources, SlotObservationSource source)
    {
        if (string.IsNullOrWhiteSpace(source.SlotId))
        {
            return;
        }

        if (!sources.TryGetValue(source.SlotId, out var values))
        {
            values = [];
            sources[source.SlotId] = values;
        }

        values.Add(source);
    }

    private static void Add(List<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value);
        }
    }

    private sealed record ObservedSlots(
        Dictionary<string, HashSet<SlotObservationSource>> Sources,
        IReadOnlyList<SlotObservationSource> Suggestions);

    public sealed record SlotObservationSource(
        string SourceKind,
        string SourceId,
        string PageId,
        string? SectionNodeId,
        string? MappingId,
        string? SlotId,
        string? TargetPath);
}
