using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;

public sealed record HandoffConsumerDryRunPage(
    string PageId,
    IReadOnlyList<string> RequiredSlots,
    IReadOnlyList<string> AllowedTargetFiles,
    IReadOnlyList<string> ProtectedFiles,
    IReadOnlyList<string> EvidenceFilePaths,
    IReadOnlyList<string> UnresolvedIssues);

public sealed record HandoffConsumerDryRunPackage(
    string ProjectId,
    SiteBlueprint Site,
    IReadOnlyList<HandoffConsumerDryRunPage> Pages,
    JsonNode? DesignTokens,
    JsonNode? VisualStyle,
    HandoffResponsiveBehaviorDocument ResponsiveBehavior,
    HandoffInteractionModelsDocument InteractionModels,
    IReadOnlyList<string> AllowedTargetFiles,
    IReadOnlyList<string> ProtectedFiles,
    IReadOnlyList<string> EvidenceFilePaths,
    IReadOnlyList<string> UnresolvedRegions,
    AgentHandoffReadinessReport ReadinessReport);

public sealed class HandoffConsumerDryRunLoader
{
    private readonly PortableHandoffValidator validator;

    public HandoffConsumerDryRunLoader()
        : this(new PortableHandoffValidator())
    {
    }

    internal HandoffConsumerDryRunLoader(PortableHandoffValidator validator)
    {
        this.validator = validator;
    }

    public async Task<HandoffConsumerDryRunPackage> LoadAsync(string handoffRoot, string schemaRoot, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(handoffRoot, schemaRoot, cancellationToken);
        if (validation.Findings.Any(finding => finding.Severity == "blocking") || !validation.ReadinessPassed)
        {
            var first = validation.Findings.FirstOrDefault(finding => finding.Severity == "blocking");
            throw new InvalidOperationException(FormatFailure(first, validation.ReadinessPassed));
        }

        var root = Path.GetFullPath(handoffRoot);
        var pageCompositions = Read<HandoffPageCompositions>(root, "analysis/agent-handoff/page-compositions.json")
            ?? throw new InvalidOperationException("Portable handoff page compositions are missing.");
        var pattern = Read<StorefrontPatternContract>(root, "analysis/agent-handoff/storefront-pattern.json")
            ?? throw new InvalidOperationException("Portable storefront pattern is missing.");
        var mappings = Read<PresentationMappingsDocument>(root, "analysis/agent-handoff/presentation-mappings.json")
            ?? throw new InvalidOperationException("Portable presentation mappings are missing.");
        var catalog = Read<HandoffPresentationCatalog>(root, "analysis/agent-handoff/presentation-catalog.json")
            ?? throw new InvalidOperationException("Portable presentation catalog is missing.");
        var allowedFiles = Read<AgentHandoffFileManifest>(root, "analysis/agent-handoff/allowed-files.json")
            ?? throw new InvalidOperationException("Portable allowed files manifest is missing.");
        var protectedFiles = Read<AgentHandoffFileManifest>(root, "analysis/agent-handoff/protected-files.json")
            ?? throw new InvalidOperationException("Portable protected files manifest is missing.");
        var responsive = Read<HandoffResponsiveBehaviorDocument>(root, "analysis/agent-handoff/responsive-behavior.json")
            ?? throw new InvalidOperationException("Portable responsive behavior document is missing.");
        var interaction = Read<HandoffInteractionModelsDocument>(root, "analysis/agent-handoff/interaction-models.json")
            ?? throw new InvalidOperationException("Portable interaction models document is missing.");
        var unresolved = Read<AgentHandoffUnresolvedRegions>(root, "analysis/agent-handoff/unresolved-regions.json")
            ?? throw new InvalidOperationException("Portable unresolved regions document is missing.");
        var evidence = Read<AgentHandoffEvidenceManifest>(root, "analysis/agent-handoff/evidence-manifest.json")
            ?? throw new InvalidOperationException("Portable evidence manifest is missing.");
        var readiness = Read<AgentHandoffReadinessReport>(root, "analysis/agent-handoff/handoff-readiness.json")
            ?? throw new InvalidOperationException("Portable handoff readiness report is missing.");
        var designTokens = ReadJsonNode(root, "analysis/agent-handoff/design-tokens.json");
        var visualStyle = ReadJsonNode(root, "analysis/agent-handoff/visual-style.json");
        var slotResolver = new SectionSlotResolver(mappings.Mappings, catalog.Components);
        var sharedLayoutMappingSlots = SharedLayoutMappingSlots(mappings.Mappings, catalog.Components);

        var pages = pageCompositions.Compositions
            .OrderBy(composition => composition.PageId, StringComparer.Ordinal)
            .Select(composition =>
            {
                var contract = MatchContract(pattern.PageContracts, composition.PageId, composition.PageArchetype)
                    ?? throw new InvalidOperationException($"No storefront contract exists for page '{composition.PageId}'.");
                var observedSections = composition.SectionTree.SelectMany(Flatten).ToArray();
                var authoritativeSlots = observedSections
                    .Select(node => slotResolver.Resolve(composition, node, contract))
                    .Where(resolution => resolution.HasAuthoritativeSlot)
                    .Select(resolution => resolution.StarterSlotId!)
                    .Concat(sharedLayoutMappingSlots)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                var missingRequiredSlots = contract.RequiredSlotIds.Where(slot => !authoritativeSlots.Contains(slot, StringComparer.Ordinal)).ToArray();
                if (missingRequiredSlots.Length > 0)
                {
                    throw new InvalidOperationException($"Portable handoff loader is missing required slots on page '{composition.PageId}': {string.Join(", ", missingRequiredSlots)}.");
                }

                return new HandoffConsumerDryRunPage(
                    composition.PageId,
                    contract.RequiredSlotIds.Order(StringComparer.Ordinal).ToArray(),
                    allowedFiles.Paths.Order(StringComparer.Ordinal).ToArray(),
                    protectedFiles.Paths.Order(StringComparer.Ordinal).ToArray(),
                    EvidencePaths(evidence, composition.PageId),
                    composition.UnresolvedIssues.Order(StringComparer.Ordinal).ToArray());
            })
            .ToArray();

        var allowedTargetFiles = allowedFiles.Paths.Order(StringComparer.Ordinal).ToArray();
        var protectedTargetFiles = protectedFiles.Paths.Order(StringComparer.Ordinal).ToArray();
        var evidenceFilePaths = EvidencePaths(evidence);
        var unresolvedRegions = unresolved.BlockingRegions.Concat(unresolved.WarningRegions).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        return new HandoffConsumerDryRunPackage(
            pageCompositions.ProjectId,
            pageCompositions.Site,
            pages,
            designTokens,
            visualStyle,
            responsive,
            interaction,
            allowedTargetFiles,
            protectedTargetFiles,
            evidenceFilePaths,
            unresolvedRegions,
            readiness);
    }

    private static IReadOnlyList<string> SharedLayoutMappingSlots(
        IReadOnlyList<PresentationMapping> mappings,
        IReadOnlyList<PresentationCatalogEntry> catalogEntries)
    {
        var catalogByComponent = catalogEntries.ToDictionary(component => component.ComponentId, StringComparer.Ordinal);
        var slots = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.StarterSlotId) ||
                !mapping.StarterSlotId.StartsWith("layout.", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(mapping.PresentationComponentId) ||
                !catalogByComponent.TryGetValue(mapping.PresentationComponentId, out var component) ||
                !component.Slots.Contains(mapping.StarterSlotId, StringComparer.Ordinal))
            {
                continue;
            }

            slots.Add(mapping.StarterSlotId);
        }

        return slots.ToArray();
    }

    private static IReadOnlyList<string> EvidencePaths(AgentHandoffEvidenceManifest evidence, string? pageId = null) =>
        evidence.Pages
            .Where(page => pageId is null || string.Equals(page.PageId, pageId, StringComparison.Ordinal))
            .SelectMany(page => page.Screenshots.Select(screenshot => screenshot.HandoffPath).Concat(page.Sections.Select(section => section.HandoffPath)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<PageCompositionNode> Flatten(PageCompositionNode node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static StorefrontPageContract? MatchContract(IReadOnlyList<StorefrontPageContract> contracts, string pageId, string pageArchetype) =>
        contracts.FirstOrDefault(contract =>
            string.Equals(contract.PageId, pageId, StringComparison.Ordinal) ||
            string.Equals(contract.StablePageArchetype, pageArchetype, StringComparison.Ordinal));

    private static string FormatFailure(PortableHandoffValidationFinding? finding, bool readinessPassed) =>
        finding is null
            ? readinessPassed
                ? "Portable handoff loader refused an invalid package state."
                : "Portable handoff loader refused a package with failed readiness."
            : string.Join(" | ",
                new[]
                {
                    finding.Code,
                    finding.Message,
                    finding.Problem is null ? null : "Problem: " + finding.Problem,
                    finding.Cause is null ? null : "Cause: " + finding.Cause,
                    finding.FixSuggestion is null ? null : "Fix: " + finding.FixSuggestion
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static T? Read<T>(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), VisualJson.Options)
            : default;
    }

    private static JsonNode? ReadJsonNode(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? JsonNode.Parse(File.ReadAllText(path))
            : null;
    }
}
