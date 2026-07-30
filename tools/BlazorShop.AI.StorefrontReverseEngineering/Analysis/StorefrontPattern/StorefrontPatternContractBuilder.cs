using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.StorefrontPattern;

public sealed class StorefrontPatternContractBuilder
{
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public StorefrontPatternContractBuilder(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(this.repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public Task<StorefrontPatternContract> BuildAsync(string projectRoot, CancellationToken cancellationToken)
    {
        var contractPath = Path.Combine(repoRoot, "BlazorShop.PresentationV2", "BlazorShop.Storefront.Starter", "starter-generation.contract.yaml");
        return BuildAsync(projectRoot, contractPath, cancellationToken);
    }

    public async Task<StorefrontPatternContract> BuildAsync(
        string projectRoot,
        string contractPath,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var parsed = StarterContractYaml.Parse(await File.ReadAllLinesAsync(contractPath, cancellationToken));
        var zones = BuildZones(parsed);
        var actions = BuildActions(parsed);
        var slots = BuildSlots(parsed, zones);
        var routes = BuildRoutes(parsed);
        var pages = BuildPageContracts(slots, routes);
        var boundaries = BuildBoundaries(parsed, actions);
        Validate(parsed, zones, slots, routes, pages, actions);

        var metadata = new StorefrontPatternMetadata(
            parsed.Scalar("contractVersion"),
            parsed.Scalar("starterVersion"),
            parsed.Scalar("targetFramework"),
            parsed.SectionValue("generatedProject", "namingConvention"),
            parsed.SectionValue("generatedProject", "outputRoot"),
            parsed.ObjectList("packageDependencies").ToDictionary(
                item => item.GetValueOrDefault("id", ""),
                item => item.GetValueOrDefault("versionProperty", ""),
                StringComparer.Ordinal),
            "generated files may be updated only inside generated zones; protected zones are read-only");

        var pattern = new StorefrontPatternContract(
            "1.0",
            "storefront-pattern",
            "storefront-pattern",
            DateTimeOffset.UtcNow,
            metadata,
            zones,
            boundaries,
            pages,
            slots,
            routes,
            actions,
            zones.ProtectedZones.Select(zone => new StorefrontProtectedFileContract(zone, "protected by Starter contract")).ToArray(),
            zones.GeneratedZones.Select(zone => new StorefrontGeneratedFileContract(zone.TrimEnd('/', '\\') + "/**", zone, ["create", "update", "restyle", "reposition"])).ToArray(),
            parsed.UnknownTopLevelScalars);

        await store.WriteJsonAsync(ArtifactPath.Create("analysis/storefront-pattern/storefront-pattern.json"), "storefront-pattern", pattern, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/storefront-pattern/page-contracts.json"), "page-contracts", new StorefrontPageContractsDocument("1.0", "page-contracts", "page-contracts", DateTimeOffset.UtcNow, pages), cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/storefront-pattern/behavior-boundaries.json"), "behavior-boundaries", new StorefrontBehaviorBoundariesDocument("1.0", "behavior-boundaries", "behavior-boundaries", DateTimeOffset.UtcNow, boundaries, actions), cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/storefront-pattern/generation-zones.json"), "generation-zones", zones, cancellationToken);
        return pattern;
    }

    private static StorefrontGenerationZones BuildZones(StarterContractYaml parsed) =>
        new(
            "1.0",
            "generation-zones",
            "generation-zones",
            DateTimeOffset.UtcNow,
            parsed.StringList("managedZones"),
            parsed.StringList("allowedGeneratedZones"),
            parsed.StringList("protectedZones"),
            parsed.StringList("assetZones"),
            parsed.Scalar("analysisArtifactZone"));

    private static IReadOnlyList<StorefrontActionContract> BuildActions(StarterContractYaml parsed) =>
        parsed.ObjectList("actionDescriptors")
            .Select(item => new StorefrontActionContract(
                item.GetValueOrDefault("id", ""),
                item.GetValueOrDefault("owner", ""),
                item.GetValueOrDefault("descriptor"),
                item.GetValueOrDefault("route"),
                item.GetValueOrDefault("routeSource"),
                true))
            .ToArray();

    private static IReadOnlyList<StorefrontSlotContract> BuildSlots(StarterContractYaml parsed, StorefrontGenerationZones zones) =>
        parsed.ObjectList("slots")
            .Select(item =>
            {
                var path = item.GetValueOrDefault("path", "");
                var generatedZone = ResolveZone(path, zones);
                return new StorefrontSlotContract(
                    item.GetValueOrDefault("id", ""),
                    item.GetValueOrDefault("owner", ""),
                    path,
                    item.GetValueOrDefault("action"),
                    CategoryForSlot(item.GetValueOrDefault("id", ""), item.GetValueOrDefault("owner", "")),
                    generatedZone,
                    string.Equals(item.GetValueOrDefault("owner", ""), "generated", StringComparison.Ordinal));
            })
            .ToArray();

    private static IReadOnlyList<StorefrontRouteContract> BuildRoutes(StarterContractYaml parsed) =>
        parsed.ObjectList("routes")
            .Select(item => new StorefrontRouteContract(
                item.GetValueOrDefault("route", ""),
                item.GetValueOrDefault("path", ""),
                item.GetValueOrDefault("renderOwner", ""),
                item.GetValueOrDefault("hydrationMode", ""),
                PageIdForRoute(item.GetValueOrDefault("route", ""), item.GetValueOrDefault("path", ""))))
            .ToArray();

    private static IReadOnlyList<StorefrontBehaviorBoundary> BuildBoundaries(
        StarterContractYaml parsed,
        IReadOnlyList<StorefrontActionContract> actions) =>
        [
            new StorefrontBehaviorBoundary(
                "browser-action-policy",
                parsed.SectionValue("browserActionPolicy", "owner"),
                "same-origin-bff-only",
                actions.Select(action => action.Descriptor).Where(descriptor => !string.IsNullOrWhiteSpace(descriptor)).Select(descriptor => descriptor!).ToArray(),
                parsed.StringList("requiredBffActions"),
                [
                    "generated-functional-js",
                    "direct-commerce-node-browser-call",
                    "route-bff-seo-media-reimplementation",
                    "runtime-transport-from-visual-code"
                ])
        ];

    private static IReadOnlyList<StorefrontPageContract> BuildPageContracts(
        IReadOnlyList<StorefrontSlotContract> slots,
        IReadOnlyList<StorefrontRouteContract> routes)
    {
        var specs = new (string PageId, string Archetype, string[] SlotPrefixes, string[] Required, string[] Optional)[]
        {
            ("home", "home", ["layout.", "home."], ["hero or home content", "store header", "store footer"], ["promotion band", "newsletter"]),
            ("category-listing", "product-listing", ["layout.", "catalog."], ["product grid", "product card"], ["filters", "sorting", "pagination"]),
            ("search-results", "search-results", ["layout.", "catalog."], ["search results", "product card"], ["filters", "sorting"]),
            ("product-detail", "product-detail", ["layout.", "product."], ["product gallery", "purchase panel", "product information"], ["reviews", "related products"]),
            ("cart-shell", "cart-shell", ["layout.", "cart."], ["cart line items", "cart summary"], ["empty cart state"]),
            ("checkout-shell", "checkout-shell", ["layout.", "checkout."], ["checkout form", "order summary"], ["payment result"]),
            ("account-auth-shell", "account-auth-shell", ["layout.", "account."], ["authentication/account shell"], ["account menu"]),
            ("content-page", "content-page", ["layout.", "home."], ["content body"], ["breadcrumbs"]),
            ("maintenance", "maintenance", ["layout.", "system."], ["maintenance state"], []),
            ("not-found", "not-found", ["layout.", "system."], ["not found state"], []),
            ("service-unavailable", "service-unavailable", ["layout.", "system."], ["service unavailable state"], []),
            ("error-state", "error-state", ["layout.", "system."], ["error state"], [])
        };

        return specs.Select(spec =>
        {
            var pageRoutes = routes.Where(route => route.PageId == spec.PageId).Select(route => route.Route).Distinct(StringComparer.Ordinal).ToArray();
            var allowedSlots = slots
                .Where(slot => spec.SlotPrefixes.Any(prefix => slot.SlotId.StartsWith(prefix, StringComparison.Ordinal)))
                .Select(slot => slot.SlotId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return new StorefrontPageContract(
                spec.PageId,
                spec.Archetype,
                "Storefront Presentation owns route declarations; generated visuals register view slots only",
                pageRoutes,
                allowedSlots,
                spec.Required,
                spec.Optional,
                ["@page route declarations", "Commerce Node direct browser calls", "BFF/SEO/media route reimplementation"],
                slots.Where(slot => slot.Action is not null && allowedSlots.Contains(slot.SlotId, StringComparer.Ordinal)).Select(slot => slot.Action!).Distinct(StringComparer.Ordinal).ToArray(),
                allowedSlots.Select(slotId => slots.First(slot => slot.SlotId == slotId).Path).Distinct(StringComparer.Ordinal).ToArray(),
                ["desktop", "tablet", "mobile"]);
        }).ToArray();
    }

    private static void Validate(
        StarterContractYaml parsed,
        StorefrontGenerationZones zones,
        IReadOnlyList<StorefrontSlotContract> slots,
        IReadOnlyList<StorefrontRouteContract> routes,
        IReadOnlyList<StorefrontPageContract> pages,
        IReadOnlyList<StorefrontActionContract> actions)
    {
        var findings = new List<string>();
        Require(parsed.Scalar("contractVersion"), "missing contract version", findings);
        Require(parsed.Scalar("starterVersion"), "missing starter template version", findings);
        Require(parsed.SectionValue("generatedProject", "namingConvention"), "missing generated project naming convention", findings);

        AddDuplicateFindings(slots.Select(slot => slot.SlotId), "duplicate slot ID", findings);
        AddDuplicateFindings(pages.Select(page => page.PageId), "duplicate page ID", findings);

        var slotIds = slots.Select(slot => slot.SlotId).ToHashSet(StringComparer.Ordinal);
        foreach (var requiredSlotId in RequiredSlotIds())
        {
            if (!slotIds.Contains(requiredSlotId))
            {
                findings.Add($"missing required slot ID: {requiredSlotId}");
            }
        }

        foreach (var slot in slots.Where(slot => slot.VisualGenerationTarget))
        {
            if (string.IsNullOrWhiteSpace(slot.GeneratedZone) || !zones.GeneratedZones.Contains(slot.GeneratedZone, StringComparer.Ordinal))
            {
                findings.Add($"unknown generated zone for slot '{slot.SlotId}': {slot.Path}");
            }

            if (zones.ProtectedZones.Any(zone => IsUnderZone(slot.Path, zone)))
            {
                findings.Add($"protected path collision for slot '{slot.SlotId}': {slot.Path}");
            }
        }

        foreach (var action in actions)
        {
            var route = action.Route ?? "";
            if (route.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                route.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                route.Contains("/api/storefront/", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add($"unsafe browser action route for '{action.ActionId}': {route}");
            }
        }

        foreach (var page in pages.Where(page => string.IsNullOrWhiteSpace(page.StablePageArchetype) || page.TargetGeneratedPathRules.Count == 0))
        {
            findings.Add($"missing required page contract fields for page '{page.PageId}'");
        }

        if (routes.Count == 0)
        {
            findings.Add("missing route contracts");
        }

        if (findings.Count > 0)
        {
            throw new InvalidOperationException("[SRE-STOREFRONT-PATTERN-001] Storefront pattern contract is invalid. Problem: Starter/Presentation handoff contract has blocking validation findings. Cause: Phase 3C requires typed zones, slots, routes, and same-origin action boundaries. Fix: update starter-generation.contract.yaml or the typed parser. Findings: " + string.Join(" | ", findings));
        }
    }

    private static void Require(string value, string message, List<string> findings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            findings.Add(message);
        }
    }

    private static void AddDuplicateFindings(IEnumerable<string> values, string label, List<string> findings)
    {
        foreach (var duplicate in values.Where(value => !string.IsNullOrWhiteSpace(value)).GroupBy(value => value, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            findings.Add($"{label}: {duplicate.Key}");
        }
    }

    private static IReadOnlyList<string> RequiredSlotIds() =>
    [
        "layout.header",
        "layout.footer",
        "layout.main-navigation",
        "layout.mobile-navigation",
        "layout.cart-badge",
        "layout.account-menu",
        "home.sections",
        "catalog.product-card",
        "catalog.filters",
        "catalog.sorting",
        "catalog.pagination",
        "product.gallery",
        "product.information",
        "product.purchase",
        "cart.page",
        "checkout.page",
        "account.shell",
        "system.error"
    ];

    private static string ResolveZone(string path, StorefrontGenerationZones zones) =>
        zones.GeneratedZones.Concat(zones.ManagedZones).Concat(zones.AssetZones)
            .OrderByDescending(zone => zone.Length)
            .FirstOrDefault(zone => IsUnderZone(path, zone)) ?? "";

    private static bool IsUnderZone(string path, string zone) =>
        path.Equals(zone, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(zone.TrimEnd('/', '\\') + "/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(zone.TrimEnd('/', '\\') + "\\", StringComparison.OrdinalIgnoreCase);

    private static string CategoryForSlot(string id, string owner)
    {
        if (id.StartsWith("layout.", StringComparison.Ordinal)) return "starter visual slot";
        if (id.StartsWith("catalog.", StringComparison.Ordinal) || id.StartsWith("product.", StringComparison.Ordinal)) return "visual generation target";
        if (id.StartsWith("cart.", StringComparison.Ordinal) || id.StartsWith("checkout.", StringComparison.Ordinal) || id.StartsWith("account.", StringComparison.Ordinal)) return "runtime-owned behavior";
        if (id.StartsWith("system.", StringComparison.Ordinal)) return "starter visual slot";
        return string.Equals(owner, "generated", StringComparison.Ordinal) ? "starter visual slot" : "explicit extension";
    }

    private static string PageIdForRoute(string route, string path) =>
        route == "/" ? "home" :
        route.StartsWith("/category", StringComparison.Ordinal) ? "category-listing" :
        route.StartsWith("/search", StringComparison.Ordinal) ? "search-results" :
        route.StartsWith("/product", StringComparison.Ordinal) ? "product-detail" :
        route.Contains("cart", StringComparison.OrdinalIgnoreCase) ? "cart-shell" :
        route.StartsWith("/checkout", StringComparison.Ordinal) || route.StartsWith("/payment", StringComparison.Ordinal) ? "checkout-shell" :
        path.Contains("/Auth/", StringComparison.OrdinalIgnoreCase) || path.Contains("/Account/", StringComparison.OrdinalIgnoreCase) ? "account-auth-shell" :
        route.StartsWith("/pages", StringComparison.Ordinal) ? "content-page" :
        route.StartsWith("/maintenance", StringComparison.Ordinal) ? "maintenance" :
        route.Contains("service", StringComparison.OrdinalIgnoreCase) ? "service-unavailable" :
        route.Contains("error", StringComparison.OrdinalIgnoreCase) ? "error-state" :
        route.Contains("{*Path", StringComparison.OrdinalIgnoreCase) ? "not-found" :
        "content-page";

    private sealed class StarterContractYaml
    {
        private readonly Dictionary<string, string> scalars;
        private readonly Dictionary<string, Dictionary<string, string>> sectionValues;
        private readonly Dictionary<string, List<string>> stringLists;
        private readonly Dictionary<string, List<Dictionary<string, string>>> objectLists;

        private StarterContractYaml(
            Dictionary<string, string> scalars,
            Dictionary<string, Dictionary<string, string>> sectionValues,
            Dictionary<string, List<string>> stringLists,
            Dictionary<string, List<Dictionary<string, string>>> objectLists)
        {
            this.scalars = scalars;
            this.sectionValues = sectionValues;
            this.stringLists = stringLists;
            this.objectLists = objectLists;
        }

        public IReadOnlyDictionary<string, string> UnknownTopLevelScalars =>
            scalars
                .Where(pair => pair.Key is not ("contractVersion" or "starterVersion" or "targetFramework" or "analysisArtifactZone" or "featureManifest"))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        public static StarterContractYaml Parse(IReadOnlyList<string> lines)
        {
            var scalars = new Dictionary<string, string>(StringComparer.Ordinal);
            var sectionValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            var stringLists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var objectLists = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.Ordinal);
            string? currentSection = null;
            Dictionary<string, string>? currentObject = null;

            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine) || rawLine.TrimStart().StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var indent = rawLine.TakeWhile(char.IsWhiteSpace).Count();
                var line = rawLine.Trim();
                if (indent == 0)
                {
                    currentObject = null;
                    if (line.EndsWith(":", StringComparison.Ordinal))
                    {
                        currentSection = line[..^1];
                        continue;
                    }

                    var pair = SplitPair(line);
                    if (pair is not null)
                    {
                        scalars[pair.Value.Key] = pair.Value.Value;
                    }
                    continue;
                }

                if (currentSection is null)
                {
                    continue;
                }

                if (indent == 2 && line.StartsWith("- ", StringComparison.Ordinal))
                {
                    var item = line[2..].Trim();
                    var pair = SplitPair(item);
                    if (pair is null)
                    {
                        GetStringList(stringLists, currentSection).Add(Unquote(item));
                        currentObject = null;
                        continue;
                    }

                    currentObject = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [pair.Value.Key] = pair.Value.Value
                    };
                    GetObjectList(objectLists, currentSection).Add(currentObject);
                    continue;
                }

                var childPair = SplitPair(line);
                if (childPair is null)
                {
                    continue;
                }

                if (currentObject is not null && indent >= 4)
                {
                    currentObject[childPair.Value.Key] = childPair.Value.Value;
                    continue;
                }

                if (indent == 2)
                {
                    if (!sectionValues.TryGetValue(currentSection, out var section))
                    {
                        section = new Dictionary<string, string>(StringComparer.Ordinal);
                        sectionValues[currentSection] = section;
                    }

                    section[childPair.Value.Key] = childPair.Value.Value;
                }
            }

            return new StarterContractYaml(scalars, sectionValues, stringLists, objectLists);
        }

        public string Scalar(string key) => scalars.GetValueOrDefault(key, "");

        public string SectionValue(string section, string key) =>
            sectionValues.TryGetValue(section, out var values) ? values.GetValueOrDefault(key, "") : "";

        public IReadOnlyList<string> StringList(string section) =>
            stringLists.TryGetValue(section, out var values) ? values : [];

        public IReadOnlyList<Dictionary<string, string>> ObjectList(string section) =>
            objectLists.TryGetValue(section, out var values) ? values : [];

        private static List<string> GetStringList(Dictionary<string, List<string>> lists, string section)
        {
            if (!lists.TryGetValue(section, out var list))
            {
                list = [];
                lists[section] = list;
            }

            return list;
        }

        private static List<Dictionary<string, string>> GetObjectList(Dictionary<string, List<Dictionary<string, string>>> lists, string section)
        {
            if (!lists.TryGetValue(section, out var list))
            {
                list = [];
                lists[section] = list;
            }

            return list;
        }

        private static KeyValuePair<string, string>? SplitPair(string line)
        {
            var index = line.IndexOf(':', StringComparison.Ordinal);
            if (index < 0)
            {
                return null;
            }

            var key = line[..index].Trim();
            var value = Unquote(line[(index + 1)..].Trim());
            return string.IsNullOrWhiteSpace(key) ? null : new KeyValuePair<string, string>(key, value);
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                return value[1..^1];
            }

            return value;
        }
    }
}
