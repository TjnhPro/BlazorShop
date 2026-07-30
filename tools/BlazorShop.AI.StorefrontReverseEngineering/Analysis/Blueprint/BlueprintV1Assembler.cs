using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;

public sealed class BlueprintV1Assembler
{
    private const string PageCompositionsArtifactPath = "analysis/resolved/page-compositions.reviewed.json";
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public BlueprintV1Assembler(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<(VisualBlueprintV1 Draft, VisualBlueprintV1 Reviewed, GenerationReadinessReport Readiness)> AssembleAsync(
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var root = resolver.ResolveRoot(projectRoot);
        var store = new FileSystemVisualArtifactStore(root, resolver, validator);
        var project = await store.ReadJsonAsync<VisualProject>(ArtifactPath.Create("project.json"), "visual-project", cancellationToken);
        var reviewed = await new ReviewDecisionApplier(repoRoot)
            .ApplyAsync(root, cancellationToken);
        var pageCompositions = BuildReviewedPageCompositions(project, root);
        var draft = Build(project, root, pageCompositions, reviewedPath: "review/review-queue.json", reviewed: false);
        var reviewedBlueprint = Build(project, root, pageCompositions, reviewedPath: "review/reviewed-items.json", reviewed: true);
        var readiness = Validate(project.ProjectId, root, reviewed, pageCompositions);
        await store.WriteJsonAsync(ArtifactPath.Create(PageCompositionsArtifactPath), "reviewed-page-compositions", pageCompositions, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/visual-blueprint.v1.draft.json"), "visual-blueprint-v1", draft, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("analysis/visual-blueprint.v1.reviewed.json"), "visual-blueprint-v1", reviewedBlueprint, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create("reports/generation-readiness.json"), "generation-readiness", readiness, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "reports", "generation-readiness.md"), WriteMarkdown(readiness), cancellationToken);
        return (draft, reviewedBlueprint, readiness);
    }

    private static VisualBlueprintV1 Build(
        VisualProject project,
        string root,
        ReviewedPageCompositionsDocument pageCompositions,
        string reviewedPath,
        bool reviewed)
    {
        var pageRoot = Path.Combine(root, "analysis", "pages");
        IReadOnlyList<string> Files(string pattern) =>
            Directory.Exists(pageRoot)
                ? Directory.EnumerateFiles(pageRoot, pattern, SearchOption.AllDirectories).Select(path => Rel(root, path)).Order(StringComparer.Ordinal).ToArray()
                : [];
        var metadata = new Dictionary<string, string>
        {
            ["name"] = project.Name,
            ["referenceUrl"] = project.ReferenceUrl,
            ["siteId"] = pageCompositions.Site.SiteId,
            ["sitePageCount"] = pageCompositions.Pages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["storeArchetypeSummary"] = pageCompositions.Site.StoreArchetypeSummary
        };

        return new VisualBlueprintV1(
            "1.0",
            "visual-blueprint-v1",
            reviewed ? $"visual-blueprint-v1-reviewed-{project.ProjectId}" : $"visual-blueprint-v1-draft-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            metadata,
            ["analysis/evidence-snapshot.json", PageCompositionsArtifactPath, "presentation-catalog/presentation-component-catalog.json"],
            pageCompositions.Pages.Select(page => page.PageId).ToArray(),
            Files("page-archetype.json"),
            "analysis/tokens/semantic-tokens.draft.json",
            Files("sections.draft.json"),
            Files("responsive-behavior.json"),
            Files("interaction-model.json"),
            "analysis/components/component-candidates.json",
            "analysis/components/component-instances.json",
            Files("ecommerce-regions.json"),
            "analysis/mapping/presentation-mappings.draft.json",
            "analysis/mapping/unsupported-patterns.json",
            "analysis/originality-audit.json",
            "analysis/confidence/confidence-report.json",
            reviewedPath,
            [
                "Use analysis/resolved/page-compositions.reviewed.json as the site-level generation plan.",
                "Do not reuse reference assets by default.",
                "Do not generate unsupported runtime behavior."
            ]);
    }

    private GenerationReadinessReport Validate(
        string projectId,
        string root,
        ReviewedItems reviewed,
        ReviewedPageCompositionsDocument pageCompositions)
    {
        var findings = new List<GenerationReadinessFinding>();
        foreach (var path in RequiredArtifacts())
        {
            if (!File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))))
            {
                findings.Add(new GenerationReadinessFinding("missing-required-artifact", "blocking", $"Required artifact is missing: {path}", path));
            }
        }

        if (reviewed.BlocksReadiness)
        {
            findings.Add(new GenerationReadinessFinding("missing-review-decisions", "blocking", "Review queue contains rejected or deferred blocking items.", "review/reviewed-items.json"));
        }

        var unsupportedPath = Path.Combine(root, "analysis", "mapping", "unsupported-patterns.json");
        if (File.Exists(unsupportedPath) && File.ReadAllText(unsupportedPath).Contains("\"humanReviewRequired\": true", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new GenerationReadinessFinding("missing-mapping-for-critical-region", "blocking", "Unsupported critical pattern requires review before generation.", "analysis/mapping/unsupported-patterns.json"));
        }

        var pagesRoot = Path.Combine(root, "analysis", "pages");
        if (Directory.Exists(pagesRoot) &&
            Directory.EnumerateFiles(pagesRoot, "sections.draft.json", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Any(text => text.Contains("invalid-peer-overlap", StringComparison.Ordinal)))
        {
            findings.Add(new GenerationReadinessFinding("invalid-section-segmentation", "blocking", "Section segmentation has blocking overlap findings."));
        }

        foreach (var page in pageCompositions.Pages)
        {
            if (page.CaptureArtifactPaths.Count == 0 || page.ViewportCoverage.Count == 0)
            {
                findings.Add(new GenerationReadinessFinding(
                    "missing-page-evidence",
                    "blocking",
                    $"Page '{page.PageId}' does not have complete capture evidence.",
                    PageCompositionsArtifactPath));
            }

            if (page.UnsupportedOrBlockedRegions.Any(region => region.StartsWith("archetype-drift:", StringComparison.Ordinal)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "page-archetype-drift",
                    "warning",
                    $"Page '{page.PageId}' URL and detected visual role need review.",
                    PageCompositionsArtifactPath));
            }
        }

        foreach (var issue in pageCompositions.Site.UnresolvedSiteLevelIssues)
        {
            findings.Add(new GenerationReadinessFinding("site-composition-review", "warning", issue, PageCompositionsArtifactPath));
        }

        return new GenerationReadinessReport("1.0", "generation-readiness", $"generation-readiness-{projectId}", DateTimeOffset.UtcNow, projectId, findings.All(finding => finding.Severity != "blocking"), findings);
    }

    private static IReadOnlyList<string> RequiredArtifacts() =>
    [
        PageCompositionsArtifactPath,
        "analysis/tokens/semantic-tokens.draft.json",
        "analysis/components/component-candidates.json",
        "analysis/mapping/presentation-mappings.draft.json",
        "analysis/confidence/confidence-report.json",
        "presentation-catalog/presentation-component-catalog.json"
    ];

    private static string WriteMarkdown(GenerationReadinessReport report) =>
        "# Generation Readiness" + Environment.NewLine + Environment.NewLine +
        $"Passed: `{report.Passed}`" + Environment.NewLine + Environment.NewLine +
        string.Join(Environment.NewLine, report.Findings.Select(finding => $"- `{finding.Code}` ({finding.Severity}): {finding.Message}")) + Environment.NewLine;

    private static ReviewedPageCompositionsDocument BuildReviewedPageCompositions(VisualProject project, string root)
    {
        var capturedPages = ReadCapturedPages(root);
        if (capturedPages.Count == 0)
        {
            capturedPages =
            [
                new CapturedPageInfo("home", project.ReferenceUrl, [], [], [])
            ];
        }

        var pageContracts = ReadPageContracts(root);
        var presentationMappings = ReadPresentationMappings(root);
        var unsupportedPatterns = ReadUnsupportedPatterns(root);
        var sharedTokens = ReadSharedTokens(root);
        var responsiveRules = ReadResponsiveRules(root);
        var siteIssues = new List<string>();
        var pageBlueprints = new List<PageBlueprint>();
        var layoutSignatures = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var page in capturedPages.OrderBy(candidate => candidate.PageId, StringComparer.Ordinal))
        {
            var archetype = ReadPageArchetype(root, page.PageId);
            var sections = ReadPageSections(root, page.PageId);
            var roles = sections.Select(section => section.Role).Where(role => !string.IsNullOrWhiteSpace(role)).ToArray();
            var layoutSignature = string.Join("|", roles.Where(IsSharedLayoutRole).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase));
            layoutSignatures[page.PageId] = layoutSignature;
            var targetContract = MatchPageContract(pageContracts, page.PageId, archetype);
            var pageIssues = new List<string>();
            var drift = DetectArchetypeDrift(page.SourceUrl, archetype);
            if (drift is not null)
            {
                pageIssues.Add(drift);
            }

            pageIssues.AddRange(unsupportedPatterns
                .Where(pattern => pattern.EvidenceIds.Count == 0 || pattern.EvidenceIds.Intersect(page.EvidenceIds, StringComparer.Ordinal).Any())
                .Select(pattern => $"unsupported:{pattern.Id}"));

            pageBlueprints.Add(new PageBlueprint(
                page.PageId,
                archetype,
                page.SourceUrl,
                page.ArtifactPaths,
                page.ViewportIds,
                ReadEcommerceRegionIds(root, page.PageId),
                presentationMappings
                    .Where(mapping => mapping.EvidenceIds.Count == 0 || mapping.EvidenceIds.Intersect(page.EvidenceIds, StringComparer.Ordinal).Any())
                    .Select(mapping => mapping.Id)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                sections.Select(section => new PageCompositionNode(section.Id, section.Role, null, section.EvidenceIds, [])).ToArray(),
                ReadPageTokenOverrides(root, page.PageId, sharedTokens),
                targetContract?.SlotId,
                targetContract?.GeneratedPath,
                pageIssues.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));
        }

        var nonEmptySignatures = layoutSignatures.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)).Select(pair => pair.Value).Distinct(StringComparer.Ordinal).ToArray();
        if (nonEmptySignatures.Length > 1)
        {
            siteIssues.Add("inconsistent shared header/navigation/footer patterns across captured pages.");
        }

        var archetypes = pageBlueprints.Select(page => page.Archetype).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var sharedLayoutSystem = layoutSignatures
            .SelectMany(pair => pair.Value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var site = new SiteBlueprint(
            project.ProjectId,
            pageBlueprints.Select(page => page.SourceUrl).Where(url => !string.IsNullOrWhiteSpace(url)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            archetypes.Length == 0 ? "unknown storefront" : string.Join(", ", archetypes),
            sharedTokens,
            sharedLayoutSystem,
            responsiveRules,
            pageBlueprints.Select(page => page.PageId).ToArray(),
            siteIssues);

        return new ReviewedPageCompositionsDocument(
            "1.0",
            "reviewed-page-compositions",
            $"reviewed-page-compositions-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            site,
            pageBlueprints);
    }

    private static IReadOnlyList<CapturedPageInfo> ReadCapturedPages(string root)
    {
        var path = Path.Combine(root, "analysis", "evidence-snapshot.json");
        var json = TryReadNode(path);
        var pages = json?["pages"]?.AsArray();
        if (pages is null)
        {
            return [];
        }

        var results = new List<CapturedPageInfo>();
        foreach (var page in pages.OfType<JsonObject>())
        {
            var pageId = StringValue(page, "pageId") ?? StringValue(page, "id") ?? StringValue(page, "role") ?? $"page-{results.Count + 1}";
            var sourceUrl = StringValue(page, "sourceUrl") ?? StringValue(page, "url") ?? string.Empty;
            var artifacts = new List<string>();
            artifacts.AddRange(StringArray(page, "sourceArtifactPaths"));
            artifacts.AddRange(StringArray(page, "captureArtifactPaths"));
            var evidenceIds = new List<string>();
            evidenceIds.AddRange(StringArray(page, "sourceEvidenceIds"));
            evidenceIds.AddRange(StringArray(page, "evidenceIds"));
            var viewportIds = new List<string>();

            foreach (var viewport in page["viewports"]?.AsArray().OfType<JsonObject>() ?? [])
            {
                var viewportId = StringValue(viewport, "viewportId") ?? StringValue(viewport, "id") ?? StringValue(viewport, "name");
                if (!string.IsNullOrWhiteSpace(viewportId))
                {
                    viewportIds.Add(viewportId);
                }

                artifacts.AddRange(StringArray(viewport, "sourceArtifactPaths"));
                artifacts.AddRange(StringArray(viewport, "captureArtifactPaths"));
                evidenceIds.AddRange(StringArray(viewport, "sourceEvidenceIds"));
                evidenceIds.AddRange(StringArray(viewport, "evidenceIds"));
            }

            results.Add(new CapturedPageInfo(
                pageId,
                sourceUrl,
                artifacts.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                viewportIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                evidenceIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));
        }

        return results;
    }

    private static string ReadPageArchetype(string root, string pageId)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "pages", pageId, "page-archetype.json"));
        return StringValue(node, "primaryArchetype")
            ?? StringValue(node, "archetype")
            ?? "unknown";
    }

    private static IReadOnlyList<PageSectionInfo> ReadPageSections(string root, string pageId)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "pages", pageId, "sections.draft.json"));
        var sections = node?["sections"]?.AsArray();
        if (sections is null)
        {
            return [];
        }

        return sections.OfType<JsonObject>()
            .Select((section, index) => new PageSectionInfo(
                StringValue(section, "sectionId") ?? StringValue(section, "id") ?? $"section-{index + 1}",
                StringValue(section, "sectionType") ?? StringValue(section, "role") ?? "unknown section",
                StringArray(section, "evidenceIds")))
            .ToArray();
    }

    private static IReadOnlyList<string> ReadEcommerceRegionIds(string root, string pageId)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "pages", pageId, "ecommerce-regions.json"));
        return node?["regions"]?.AsArray().OfType<JsonObject>()
            .Select((region, index) => StringValue(region, "regionId") ?? StringValue(region, "id") ?? $"region-{index + 1}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray()
            ?? [];
    }

    private static IReadOnlyDictionary<string, string> ReadSharedTokens(string root)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "tokens", "semantic-tokens.draft.json"));
        var tokens = node?["tokens"]?.AsArray();
        if (tokens is null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return tokens.OfType<JsonObject>()
            .Select(token => new
            {
                Role = StringValue(token, "role") ?? StringValue(token, "tokenId"),
                Value = StringValue(token, "value")
                    ?? StringValue(token, "normalizedValue")
                    ?? string.Join(", ", StringArray(token, "values"))
                    ?? StringValue(token, "rawValue")
            })
            .Where(token => !string.IsNullOrWhiteSpace(token.Role) && !string.IsNullOrWhiteSpace(token.Value))
            .GroupBy(token => token.Role!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => string.Join(", ", group.Select(token => token.Value!).Distinct(StringComparer.Ordinal)), StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, string> ReadPageTokenOverrides(string root, string pageId, IReadOnlyDictionary<string, string> sharedTokens)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "pages", pageId, "semantic-token-overrides.json"));
        var tokens = node?["tokens"]?.AsArray();
        if (tokens is null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return tokens.OfType<JsonObject>()
            .Select(token => new
            {
                Role = StringValue(token, "role"),
                Value = StringValue(token, "value") ?? StringValue(token, "normalizedValue")
            })
            .Where(token => !string.IsNullOrWhiteSpace(token.Role) && !string.IsNullOrWhiteSpace(token.Value))
            .Where(token => !sharedTokens.TryGetValue(token.Role!, out var shared) || !string.Equals(shared, token.Value, StringComparison.Ordinal))
            .GroupBy(token => token.Role!, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value!, StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> ReadResponsiveRules(string root)
    {
        var pagesRoot = Path.Combine(root, "analysis", "pages");
        return Directory.Exists(pagesRoot)
            ? Directory.EnumerateFiles(pagesRoot, "responsive-behavior.json", SearchOption.AllDirectories)
                .Select(path => Rel(root, path))
                .Order(StringComparer.Ordinal)
                .ToArray()
            : [];
    }

    private static IReadOnlyList<PageContractInfo> ReadPageContracts(string root)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "storefront-pattern", "page-contracts.json"));
        return node?["pages"]?.AsArray().OfType<JsonObject>()
            .Select(page => new PageContractInfo(
                StringValue(page, "pageId") ?? string.Empty,
                StringValue(page, "stablePageArchetype") ?? StringValue(page, "archetype") ?? string.Empty,
                StringArray(page, "routes").Count > 0 ? StringArray(page, "routes") : StringArray(page, "routeTemplates"),
                (StringArray(page, "targetViewSlots").Count > 0 ? StringArray(page, "targetViewSlots") : StringArray(page, "allowedVisualSlots"))
                    .FirstOrDefault(slot => !slot.StartsWith("layout.", StringComparison.OrdinalIgnoreCase)),
                StringArray(page, "targetGeneratedPathRules")
                    .LastOrDefault(path => !path.Contains("/Layout/", StringComparison.OrdinalIgnoreCase) && !path.Contains("\\Layout\\", StringComparison.OrdinalIgnoreCase))
                    ?? StringArray(page, "targetGeneratedPathRules").FirstOrDefault()))
            .Where(page => !string.IsNullOrWhiteSpace(page.PageId))
            .ToArray()
            ?? [];
    }

    private static PageContractInfo? MatchPageContract(IReadOnlyList<PageContractInfo> contracts, string pageId, string archetype)
    {
        var normalizedPage = pageId.Replace("-", string.Empty).Replace("_", string.Empty);
        return contracts.FirstOrDefault(contract => string.Equals(contract.PageId, pageId, StringComparison.OrdinalIgnoreCase))
            ?? contracts.FirstOrDefault(contract => !string.IsNullOrWhiteSpace(contract.Archetype) && string.Equals(contract.Archetype, archetype, StringComparison.OrdinalIgnoreCase))
            ?? contracts.FirstOrDefault(contract => contract.PageId.Replace(".", string.Empty).Contains(normalizedPage, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<MappingInfo> ReadPresentationMappings(string root)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "mapping", "presentation-mappings.draft.json"));
        return node?["mappings"]?.AsArray().OfType<JsonObject>()
            .Select((mapping, index) => new MappingInfo(
                StringValue(mapping, "mappingId") ?? StringValue(mapping, "sourceCandidateId") ?? $"mapping-{index + 1}",
                StringArray(mapping, "evidenceIds")))
            .ToArray()
            ?? [];
    }

    private static IReadOnlyList<MappingInfo> ReadUnsupportedPatterns(string root)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "mapping", "unsupported-patterns.json"));
        return node?["patterns"]?.AsArray().OfType<JsonObject>()
            .Select((pattern, index) => new MappingInfo(
                StringValue(pattern, "patternId") ?? StringValue(pattern, "sourceCandidateId") ?? $"unsupported-{index + 1}",
                StringArray(pattern, "evidenceIds")))
            .ToArray()
            ?? [];
    }

    private static string? DetectArchetypeDrift(string sourceUrl, string archetype)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var path = uri.AbsolutePath.Trim('/').ToLowerInvariant();
        if (path.Length == 0)
        {
            return archetype.Contains("home", StringComparison.OrdinalIgnoreCase) ? null : "archetype-drift:root-url-not-home";
        }

        if ((path.Contains("product", StringComparison.Ordinal) || path.Contains("pdp", StringComparison.Ordinal)) &&
            !archetype.Contains("product", StringComparison.OrdinalIgnoreCase))
        {
            return "archetype-drift:product-url-not-product-detail";
        }

        if ((path.Contains("category", StringComparison.Ordinal) || path.Contains("collection", StringComparison.Ordinal) || path.Contains("listing", StringComparison.Ordinal)) &&
            !archetype.Contains("category", StringComparison.OrdinalIgnoreCase) &&
            !archetype.Contains("listing", StringComparison.OrdinalIgnoreCase))
        {
            return "archetype-drift:listing-url-not-listing";
        }

        return null;
    }

    private static bool IsSharedLayoutRole(string role) =>
        role.Contains("header", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("navigation", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("nav", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("footer", StringComparison.OrdinalIgnoreCase);

    private static JsonObject? TryReadNode(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
    }

    private static string? StringValue(JsonNode? node, string propertyName)
    {
        if (node is not JsonObject obj || !obj.TryGetPropertyValue(propertyName, out var value) || value is null)
        {
            return null;
        }

        return value.GetValueKind() == System.Text.Json.JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
    }

    private static IReadOnlyList<string> StringArray(JsonNode? node, string propertyName)
    {
        if (node is not JsonObject obj || !obj.TryGetPropertyValue(propertyName, out var value) || value is null)
        {
            return [];
        }

        if (value is JsonArray array)
        {
            return array.Select(item => item?.GetValueKind() == System.Text.Json.JsonValueKind.String ? item.GetValue<string>() : item?.ToJsonString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .ToArray();
        }

        var scalar = value.GetValueKind() == System.Text.Json.JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
        return string.IsNullOrWhiteSpace(scalar) ? [] : [scalar];
    }

    private static string Rel(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private sealed record CapturedPageInfo(
        string PageId,
        string SourceUrl,
        IReadOnlyList<string> ArtifactPaths,
        IReadOnlyList<string> ViewportIds,
        IReadOnlyList<string> EvidenceIds);

    private sealed record PageSectionInfo(
        string Id,
        string Role,
        IReadOnlyList<string> EvidenceIds);

    private sealed record PageContractInfo(
        string PageId,
        string Archetype,
        IReadOnlyList<string> Routes,
        string? SlotId,
        string? GeneratedPath);

    private sealed record MappingInfo(
        string Id,
        IReadOnlyList<string> EvidenceIds);
}
