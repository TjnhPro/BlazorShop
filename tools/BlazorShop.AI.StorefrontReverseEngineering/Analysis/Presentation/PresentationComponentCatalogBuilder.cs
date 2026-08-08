using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Presentation;

public sealed partial class PresentationComponentCatalogBuilder
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public PresentationComponentCatalogBuilder(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(this.repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<PresentationComponentCatalog> BuildAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var sources = RequiredSources();
        var findings = sources
            .Where(source => !File.Exists(source) && !Directory.Exists(source))
            .Select(source => new PresentationCatalogValidationFinding("missing-catalog-source", "blocking", $"Catalog source is missing: {Relative(source)}"))
            .ToList();
        var pattern = await new StorefrontPatternContractBuilder(repoRoot)
            .BuildAsync(root, cancellationToken);
        var entries = new List<PresentationCatalogEntry>();
        entries.AddRange(ReadFoundationSlots(sources[0], sources[1]));
        entries.AddRange(ReadStarterSlotsAndActions(pattern, sources[2]));
        entries.AddRange(ReadComponentContracts(sources[3], behaviorOwnedByRuntime: false));
        entries.AddRange(ReadComponentContracts(sources[4], behaviorOwnedByRuntime: true));
        ValidateRequiredFoundationSlots(entries, findings);
        ValidateStarterContract(entries, pattern, findings);
        ValidateBehaviorOwnership(entries, findings);
        ValidateSemanticCategories(entries, findings);

        var catalog = new PresentationComponentCatalog(
            "1.0",
            "presentation-component-catalog",
            "presentation-component-catalog",
            DateTimeOffset.UtcNow,
            entries.DistinctBy(entry => entry.ComponentId).OrderBy(entry => entry.ComponentId, StringComparer.Ordinal).ToArray(),
            sources.Select(Relative).ToArray());
        var report = new PresentationCatalogValidationReport(
            "1.0",
            "presentation-catalog-validation-report",
            "presentation-catalog-validation-report",
            DateTimeOffset.UtcNow,
            findings.All(finding => finding.Severity != "blocking") && entries.Count > 0,
            findings);
        await store.WriteJsonAsync(ArtifactPath.Create("presentation-catalog/presentation-component-catalog.json"), "presentation-component-catalog", catalog, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("presentation-catalog/catalog-validation-report.json"), "presentation-catalog-validation-report", report, cancellationToken);
        return catalog;
    }

    private IReadOnlyList<string> RequiredSources() =>
    [
        Path.Combine(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Presentation", "Views", "Foundation", "StorefrontFoundationViewSet.cs"),
        Path.Combine(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Presentation", "Views", "Foundation", "StorefrontFoundationViewOptionsValidator.cs"),
        Path.Combine(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Starter", "starter-generation.contract.yaml"),
        Path.Combine(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Components", "Contracts"),
        Path.Combine(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Components", "Headless")
    ];

    private IEnumerable<PresentationCatalogEntry> ReadFoundationSlots(string viewSetPath, string validatorPath)
    {
        if (!File.Exists(viewSetPath)) yield break;
        var contexts = ReadExpectedContextTypes(validatorPath);
        foreach (Match match in RequiredSlotRegex().Matches(File.ReadAllText(viewSetPath)))
        {
            var slot = match.Groups[1].Value;
            var context = contexts.GetValueOrDefault(slot, slot == "VisualScripts" ? "visual-script-slot-context" : "foundation-view-context");
            yield return Entry(
                $"foundation.{ToKebab(slot)}",
                "foundation view slot",
                [],
                RolesForSlot(slot),
                [slot],
                [],
                ["visual-component-type"],
                ["host-specific"],
                [],
                context,
                presentation: true,
                runtime: false,
                sourceFiles: [Relative(viewSetPath), Relative(validatorPath)],
                intentCategory: "foundation view slot",
                capabilityOwnership: ["Presentation-owned routing/SEO/media behavior"],
                allowedFilePatterns: [],
                protectedFilePatterns: ["BlazorShop.Storefront.Presentation/**"],
                requiredEvidenceTypes: ["foundation slot", "component type", "context parameter"],
                fallbackBehavior: slot.EndsWith("State", StringComparison.Ordinal) ? "required system-state fallback visual" : "required host visual registration");
        }
    }

    private IEnumerable<PresentationCatalogEntry> ReadStarterSlotsAndActions(StorefrontPatternContract pattern, string path)
    {
        if (!File.Exists(path)) yield break;
        foreach (var slot in pattern.Slots)
        {
            var runtimeOwned = slot.Category == "runtime-owned behavior";
            var visualOnly = slot.Category is "starter visual slot" or "visual generation target";
            yield return Entry(
                slot.SlotId,
                slot.Category,
                PagesForSlot(slot.SlotId),
                RolesForSlot(slot.SlotId),
                [slot.SlotId],
                ["default"],
                ["css", "markup"],
                ["responsive-layout"],
                slot.Action is null ? [] : [slot.Action],
                "starter-generation-contract",
                presentation: true,
                runtime: runtimeOwned,
                sourceFiles: [Relative(path)],
                intentCategory: slot.Category,
                capabilityOwnership: runtimeOwned ? ["Runtime-owned Commerce Node transport behavior", "BFF-owned behavior"] : visualOnly ? ["visual-only"] : ["explicit extension"],
                allowedFilePatterns: slot.VisualGenerationTarget ? [slot.Path] : [],
                protectedFilePatterns: pattern.ProtectedFiles.Select(file => file.Path).ToArray(),
                requiredEvidenceTypes: slot.VisualGenerationTarget ? ["DOM", "computed styles", "bounding boxes"] : ["semantic descriptor"],
                fallbackBehavior: runtimeOwned ? "keep Presentation/Runtime behavior and only restyle visual shell" : "optional visual region may use Starter fallback when evidence is absent");
        }

        foreach (var action in pattern.Actions)
        {
            yield return Entry(
                $"action.{ToKebab(action.ActionId)}",
                "presentation action binding",
                [],
                RolesForSlot(action.ActionId),
                [],
                [],
                ["data-attribute"],
                [],
                action.Descriptor is null ? [] : [action.Descriptor],
                "bff-action-descriptor",
                presentation: true,
                runtime: true,
                sourceFiles: [Relative(path)],
                intentCategory: "presentation action binding",
                capabilityOwnership: ["browser-safe action", "BFF-owned behavior"],
                allowedFilePatterns: [],
                protectedFilePatterns: pattern.ProtectedFiles.Select(file => file.Path).ToArray(),
                requiredEvidenceTypes: ["action descriptor"],
                fallbackBehavior: "preserve descriptor and route through same-origin BFF");
        }
    }

    private IEnumerable<PresentationCatalogEntry> ReadComponentContracts(string directory, bool behaviorOwnedByRuntime)
    {
        if (!Directory.Exists(directory)) yield break;
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var id = Path.GetFileNameWithoutExtension(file);
            yield return Entry(
                $"contract.{ToKebab(id)}",
                behaviorOwnedByRuntime ? "headless behavior contract" : "component data contract",
                [],
                RolesForSlot(id),
                [],
                ["default"],
                ["labels", "model"],
                ["state-aware"],
                [],
                id,
                presentation: !behaviorOwnedByRuntime,
                runtime: behaviorOwnedByRuntime,
                sourceFiles: [Relative(file)],
                intentCategory: behaviorOwnedByRuntime ? "headless behavior contract" : "component data contract",
                capabilityOwnership: behaviorOwnedByRuntime ? ["runtime-owned behavior"] : ["browser-safe action"],
                allowedFilePatterns: [],
                protectedFilePatterns: [Relative(file)],
                requiredEvidenceTypes: ["contract shape"],
                fallbackBehavior: behaviorOwnedByRuntime ? "do not target for visual generation" : "consume through shared contract only");
        }
    }

    private static PresentationCatalogEntry Entry(
        string componentId,
        string category,
        IReadOnlyList<string> pageArchetypes,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> slots,
        IReadOnlyList<string> variants,
        IReadOnlyList<string> visualProperties,
        IReadOnlyList<string> responsive,
        IReadOnlyList<string> interactions,
        string dataContract,
        bool presentation,
        bool runtime,
        IReadOnlyList<string> sourceFiles,
        string intentCategory,
        IReadOnlyList<string> capabilityOwnership,
        IReadOnlyList<string> allowedFilePatterns,
        IReadOnlyList<string> protectedFilePatterns,
        IReadOnlyList<string> requiredEvidenceTypes,
        string fallbackBehavior)
    {
        var visualOverrideAllowed = !runtime || category is "presentation action binding";
        return new(componentId, category, pageArchetypes, roles, slots, variants, visualProperties, responsive, interactions, dataContract, presentation, runtime, VisualOverrideAllowed: visualOverrideAllowed, BehaviorOverrideAllowed: false, RequiredChildren: [], OptionalChildren: [], UnsupportedPatterns: runtime ? ["generated-functional-js"] : [], sourceFiles, ContractVersion: "2", intentCategory, capabilityOwnership, allowedFilePatterns, protectedFilePatterns, requiredEvidenceTypes, fallbackBehavior);
    }

    private static void ValidateRequiredFoundationSlots(
        IReadOnlyList<PresentationCatalogEntry> entries,
        List<PresentationCatalogValidationFinding> findings)
    {
        var required = new[]
        {
            "foundation.application-head", "foundation.visual-scripts", "foundation.main-layout", "foundation.consent-banner",
            "foundation.home-page", "foundation.category-page", "foundation.product-page", "foundation.search-page",
            "foundation.content-page", "foundation.cart-page",
            "foundation.checkout-page", "foundation.payment-result-page", "foundation.auth-page", "foundation.account-page",
            "foundation.maintenance-state", "foundation.not-found-state", "foundation.service-unavailable-state", "foundation.error-state"
        };
        foreach (var id in required.Where(id => entries.All(entry => entry.ComponentId != id)))
        {
            findings.Add(new PresentationCatalogValidationFinding("missing-foundation-slot", "blocking", $"Required foundation slot is missing from catalog: {id}"));
        }
    }

    private static void ValidateStarterContract(
        IReadOnlyList<PresentationCatalogEntry> entries,
        StorefrontPatternContract pattern,
        List<PresentationCatalogValidationFinding> findings)
    {
        var required = new[] { "catalog.product-card", "product.gallery", "product.purchase", "cart.page", "checkout.page" };
        foreach (var id in required.Where(id => entries.All(entry => entry.ComponentId != id)))
        {
            findings.Add(new PresentationCatalogValidationFinding("missing-starter-slot", "blocking", $"Required Starter slot mapping is missing from catalog: {id}"));
        }

        foreach (var slot in pattern.Slots.Where(slot => entries.All(entry => entry.ComponentId != slot.SlotId)))
        {
            findings.Add(new PresentationCatalogValidationFinding("unmapped-starter-slot", "blocking", $"Starter contract slot is not represented in catalog: {slot.SlotId}"));
        }
    }

    private static void ValidateBehaviorOwnership(
        IReadOnlyList<PresentationCatalogEntry> entries,
        List<PresentationCatalogValidationFinding> findings)
    {
        foreach (var entry in entries.Where(entry => entry.BehaviorOwnedByRuntime && entry.BehaviorOverrideAllowed))
        {
            findings.Add(new PresentationCatalogValidationFinding("invalid-behavior-ownership", "blocking", $"Runtime-owned behavior must not be overrideable: {entry.ComponentId}"));
        }

        foreach (var entry in entries.Where(entry => entry.CapabilityOwnership.Contains("visual-only", StringComparer.Ordinal) && entry.BehaviorOwnedByRuntime))
        {
            findings.Add(new PresentationCatalogValidationFinding("visual-only-claims-runtime", "blocking", $"Visual-only catalog entry must not claim runtime ownership: {entry.ComponentId}"));
        }

        foreach (var entry in entries.Where(entry => entry.BehaviorOwnedByRuntime && entry.VisualOverrideAllowed && entry.Category != "presentation action binding"))
        {
            findings.Add(new PresentationCatalogValidationFinding("runtime-owned-visual-target", "blocking", $"Runtime-owned behavior cannot be targeted for visual generation: {entry.ComponentId}"));
        }
    }

    private static void ValidateSemanticCategories(
        IReadOnlyList<PresentationCatalogEntry> entries,
        List<PresentationCatalogValidationFinding> findings)
    {
        var allowed = new[]
        {
            "visual generation target",
            "foundation view slot",
            "starter visual slot",
            "presentation action binding",
            "component data contract",
            "headless behavior contract",
            "runtime-owned behavior",
            "explicit extension"
        };

        foreach (var entry in entries.Where(entry => !allowed.Contains(entry.Category, StringComparer.Ordinal)))
        {
            findings.Add(new PresentationCatalogValidationFinding("unknown-catalog-category", "blocking", $"Catalog entry has unknown semantic category: {entry.ComponentId} -> {entry.Category}"));
        }
    }

    private static IReadOnlyList<string> PagesForSlot(string id) =>
        id.StartsWith("home.", StringComparison.Ordinal) ? ["home"] :
        id.StartsWith("catalog.", StringComparison.Ordinal) ? ["product-listing", "search-results"] :
        id.StartsWith("product.", StringComparison.Ordinal) ? ["product-detail"] :
        id.StartsWith("cart.", StringComparison.Ordinal) ? ["cart-shell"] :
        id.StartsWith("checkout.", StringComparison.Ordinal) ? ["checkout-shell"] : [];

    private static IReadOnlyList<string> RolesForSlot(string id)
    {
        var text = id.ToLowerInvariant();
        if (text.Contains("header", StringComparison.Ordinal)) return ["store header"];
        if (text.Contains("navigation", StringComparison.Ordinal)) return ["primary/category navigation"];
        if (text.Contains("product-card", StringComparison.Ordinal)) return ["product card collection"];
        if (text.Contains("gallery", StringComparison.Ordinal)) return ["product media"];
        if (text.Contains("purchase", StringComparison.Ordinal)) return ["add-to-cart/buy-now visual"];
        if (text.Contains("cart", StringComparison.Ordinal)) return ["cart line items visual", "cart summary"];
        if (text.Contains("checkout", StringComparison.Ordinal)) return ["checkout form region", "order summary visual"];
        if (text.Contains("account", StringComparison.Ordinal)) return ["account access"];
        if (text.Contains("error", StringComparison.Ordinal) || text.Contains("not-found", StringComparison.Ordinal) || text.Contains("maintenance", StringComparison.Ordinal)) return ["error", "not found", "service unavailable"];
        return [];
    }

    private string Relative(string path) => Path.GetRelativePath(repoRoot, path).Replace(Path.DirectorySeparatorChar, '/');

    private static string ToKebab(string value) =>
        Regex.Replace(value, "([a-z0-9])([A-Z])", "$1-$2").Replace('.', '-').ToLowerInvariant();

    private static IReadOnlyDictionary<string, string> ReadExpectedContextTypes(string validatorPath)
    {
        if (!File.Exists(validatorPath)) return new Dictionary<string, string>(StringComparer.Ordinal);
        return ContextTypeRegex().Matches(File.ReadAllText(validatorPath))
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value, StringComparer.Ordinal);
    }

    [GeneratedRegex(@"new\(nameof\(this\.([A-Za-z0-9]+)\)")]
    private static partial Regex RequiredSlotRegex();

    [GeneratedRegex(@"\[nameof\(StorefrontFoundationViewSet\.([A-Za-z0-9]+)\)\]\s*=\s*typeof\(([A-Za-z0-9]+)\)")]
    private static partial Regex ContextTypeRegex();
}
