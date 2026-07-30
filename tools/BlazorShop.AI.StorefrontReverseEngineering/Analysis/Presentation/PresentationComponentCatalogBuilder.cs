using System.Text.RegularExpressions;
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
        var entries = new List<PresentationCatalogEntry>();
        entries.AddRange(ReadFoundationSlots(sources[0]));
        entries.AddRange(ReadStarterSlotsAndActions(sources[2]));
        entries.AddRange(ReadComponentContracts(sources[3], behaviorOwnedByRuntime: false));
        entries.AddRange(ReadComponentContracts(sources[4], behaviorOwnedByRuntime: true));
        ValidateRequiredFoundationSlots(entries, findings);
        ValidateStarterContract(entries, findings);
        ValidateBehaviorOwnership(entries, findings);

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

    private IEnumerable<PresentationCatalogEntry> ReadFoundationSlots(string path)
    {
        if (!File.Exists(path)) yield break;
        foreach (Match match in FoundationSlotRegex().Matches(File.ReadAllText(path)))
        {
            var slot = match.Groups[1].Value;
            yield return Entry($"foundation.{ToKebab(slot)}", "foundation-slot", [], RolesForSlot(slot), [slot], [], ["visual-component-type"], ["host-specific"], [], "foundation-view-context", true, false, [Relative(path)]);
        }
    }

    private IEnumerable<PresentationCatalogEntry> ReadStarterSlotsAndActions(string path)
    {
        if (!File.Exists(path)) yield break;
        string? currentSlot = null;
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("- id:", StringComparison.Ordinal))
            {
                currentSlot = line["- id:".Length..].Trim();
                yield return Entry(currentSlot, "starter-slot", PagesForSlot(currentSlot), RolesForSlot(currentSlot), [currentSlot], ["default"], ["css", "markup"], ["responsive-layout"], [], "starter-generation-contract", true, currentSlot.Contains("cart", StringComparison.Ordinal), [Relative(path)]);
            }
            else if (line.StartsWith("descriptor:", StringComparison.Ordinal) && currentSlot is not null)
            {
                yield return Entry($"action.{ToKebab(currentSlot)}", "action-descriptor", [], RolesForSlot(currentSlot), [], [], ["data-attribute"], [], [line["descriptor:".Length..].Trim()], "bff-action-descriptor", true, true, [Relative(path)]);
            }
        }
    }

    private IEnumerable<PresentationCatalogEntry> ReadComponentContracts(string directory, bool behaviorOwnedByRuntime)
    {
        if (!Directory.Exists(directory)) yield break;
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var id = Path.GetFileNameWithoutExtension(file);
            yield return Entry($"contract.{ToKebab(id)}", behaviorOwnedByRuntime ? "headless-behavior" : "component-contract", [], RolesForSlot(id), [], ["default"], ["labels", "model"], ["state-aware"], [], id, !behaviorOwnedByRuntime, behaviorOwnedByRuntime, [Relative(file)]);
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
        IReadOnlyList<string> sourceFiles) =>
        new(componentId, category, pageArchetypes, roles, slots, variants, visualProperties, responsive, interactions, dataContract, presentation, runtime, VisualOverrideAllowed: true, BehaviorOverrideAllowed: false, RequiredChildren: [], OptionalChildren: [], UnsupportedPatterns: runtime ? ["generated-functional-js"] : [], sourceFiles, ContractVersion: "1");

    private static void ValidateRequiredFoundationSlots(
        IReadOnlyList<PresentationCatalogEntry> entries,
        List<PresentationCatalogValidationFinding> findings)
    {
        var required = new[]
        {
            "foundation.application-head", "foundation.main-layout", "foundation.consent-banner", "foundation.home-page",
            "foundation.category-page", "foundation.product-page", "foundation.search-page", "foundation.cart-page",
            "foundation.checkout-page", "foundation.auth-page", "foundation.account-page", "foundation.not-found-state"
        };
        foreach (var id in required.Where(id => entries.All(entry => entry.ComponentId != id)))
        {
            findings.Add(new PresentationCatalogValidationFinding("missing-foundation-slot", "blocking", $"Required foundation slot is missing from catalog: {id}"));
        }
    }

    private static void ValidateStarterContract(
        IReadOnlyList<PresentationCatalogEntry> entries,
        List<PresentationCatalogValidationFinding> findings)
    {
        var required = new[] { "catalog.product-card", "product.gallery", "product.purchase", "cart.page", "checkout.page" };
        foreach (var id in required.Where(id => entries.All(entry => entry.ComponentId != id)))
        {
            findings.Add(new PresentationCatalogValidationFinding("missing-starter-slot", "blocking", $"Required Starter slot mapping is missing from catalog: {id}"));
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

    [GeneratedRegex(@"required\s+Type\s+([A-Za-z0-9]+)\s*\{")]
    private static partial Regex FoundationSlotRegex();
}
