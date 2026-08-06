using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Mapping;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Provenance;
using BlazorShop.AI.StorefrontReverseEngineering.Storage;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;

namespace BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;

public sealed class BlueprintV1Assembler
{
    private const string PageCompositionsArtifactPath = "analysis/resolved/page-compositions.reviewed.json";
    private const string DraftBlueprintPath = "analysis/visual-blueprint.v1.draft.json";
    private const string ReviewedBlueprintPath = "analysis/visual-blueprint.v1.reviewed.json";
    private const string ReviewResolutionManifestPath = "analysis/resolved/review-resolution-manifest.json";
    private readonly string repoRoot;
    private readonly ApprovedArtifactRootResolver resolver;
    private readonly IVisualSchemaValidator validator;

    public BlueprintV1Assembler(string repoRoot)
    {
        this.repoRoot = Path.GetFullPath(repoRoot);
        resolver = new ApprovedArtifactRootResolver(repoRoot);
        validator = new VisualSchemaValidator(new VisualSchemaRegistry());
    }

    public async Task<(VisualBlueprintV1 Draft, VisualBlueprintV1? Reviewed, GenerationReadinessReport Readiness)> AssembleAsync(
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
        await store.WriteJsonAsync(ArtifactPath.Create(PageCompositionsArtifactPath), "reviewed-page-compositions", pageCompositions, cancellationToken);
        await store.WriteJsonAsync(ArtifactPath.Create(DraftBlueprintPath), "visual-blueprint-v1", draft, cancellationToken);

        var reviewResolution = ReadReviewResolutionManifest(root);
        var preReviewedReadiness = Validate(project.ProjectId, root, reviewed, pageCompositions, reviewResolution, validateReviewedBlueprint: false);
        VisualBlueprintV1? reviewedBlueprint = null;
        if (preReviewedReadiness.Passed)
        {
            reviewedBlueprint = Build(project, root, pageCompositions, reviewedPath: ReviewResolutionManifestPath, reviewed: true, reviewResolution);
            await store.WriteJsonAsync(ArtifactPath.Create(ReviewedBlueprintPath), "visual-blueprint-v1", reviewedBlueprint, cancellationToken);
        }
        else
        {
            DeleteIfExists(Path.Combine(root, ReviewedBlueprintPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        var readiness = Validate(project.ProjectId, root, reviewed, pageCompositions, reviewResolution, validateReviewedBlueprint: true);
        await store.WriteJsonAsync(ArtifactPath.Create("reports/generation-readiness.json"), "generation-readiness", readiness, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "reports", "generation-readiness.md"), WriteMarkdown(readiness), cancellationToken);
        return (draft, reviewedBlueprint, readiness);
    }

    private static VisualBlueprintV1 Build(
        VisualProject project,
        string root,
        ReviewedPageCompositionsDocument pageCompositions,
        string reviewedPath,
        bool reviewed,
        ReviewResolutionManifest? reviewResolution = null)
    {
        var pageRoot = Path.Combine(root, "analysis", "pages");
        IReadOnlyList<string> Files(string pattern) =>
            Directory.Exists(pageRoot)
                ? Directory.EnumerateFiles(pageRoot, pattern, SearchOption.AllDirectories).Select(path => Rel(root, path)).Order(StringComparer.Ordinal).ToArray()
                : [];
        var metadata = BuildMetadata(project, root, pageCompositions, reviewResolution);
        var sourceProvenance = reviewed
            ? new[]
            {
                "analysis/evidence-snapshot.json",
                PageCompositionsArtifactPath,
                "analysis/storefront-pattern/storefront-pattern.json",
                "analysis/storefront-pattern/page-contracts.json",
                "presentation-catalog/presentation-component-catalog.json",
                ReviewResolutionManifestPath,
                "analysis/resolved/semantic-tokens.reviewed.json",
                "analysis/resolved/page-archetypes.reviewed.json",
                "analysis/resolved/page-sections.reviewed.json",
                "analysis/resolved/component-candidates.reviewed.json",
                "analysis/resolved/presentation-mappings.reviewed.json",
                "analysis/resolved/ecommerce-regions.reviewed.json",
                "analysis/resolved/unsupported-pattern-decisions.json",
                "analysis/resolved/originality-restrictions.reviewed.json"
            }
            : ["analysis/evidence-snapshot.json", PageCompositionsArtifactPath, "presentation-catalog/presentation-component-catalog.json"];

        return new VisualBlueprintV1(
            "1.0",
            "visual-blueprint-v1",
            reviewed ? $"visual-blueprint-v1-reviewed-{project.ProjectId}" : $"visual-blueprint-v1-draft-{project.ProjectId}",
            DateTimeOffset.UtcNow,
            project.ProjectId,
            metadata,
            sourceProvenance,
            pageCompositions.Pages.Select(page => page.PageId).ToArray(),
            reviewed ? ["analysis/resolved/page-archetypes.reviewed.json"] : Files("page-archetype.json"),
            reviewed ? "analysis/resolved/semantic-tokens.reviewed.json" : "analysis/tokens/semantic-tokens.draft.json",
            reviewed ? ["analysis/resolved/page-sections.reviewed.json"] : Files("sections.draft.json"),
            Files("responsive-behavior.json"),
            Files("interaction-model.json"),
            reviewed ? "analysis/resolved/component-candidates.reviewed.json" : "analysis/components/component-candidates.json",
            "analysis/components/component-instances.json",
            reviewed ? ["analysis/resolved/ecommerce-regions.reviewed.json"] : Files("ecommerce-regions.json"),
            reviewed ? "analysis/resolved/presentation-mappings.reviewed.json" : "analysis/mapping/presentation-mappings.draft.json",
            reviewed ? "analysis/resolved/unsupported-pattern-decisions.json" : "analysis/mapping/unsupported-patterns.json",
            reviewed ? "analysis/resolved/originality-restrictions.reviewed.json" : "analysis/originality-audit.json",
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
        ReviewedPageCompositionsDocument pageCompositions,
        ReviewResolutionManifest? reviewResolution,
        bool validateReviewedBlueprint)
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

        if (reviewResolution is null)
        {
            findings.Add(new GenerationReadinessFinding("reviewed-blueprint-not-resolved", "blocking", "Review resolution manifest is missing; reviewed blueprint cannot be assembled.", ReviewResolutionManifestPath));
        }
        else if (reviewResolution.BlockingUnresolvedCount > 0)
        {
            findings.Add(new GenerationReadinessFinding("reviewed-blueprint-not-resolved", "blocking", "Review resolution still contains blocking unresolved items.", ReviewResolutionManifestPath));
        }

        var unsupportedPath = Path.Combine(root, "analysis", "resolved", "unsupported-pattern-decisions.json");
        if (File.Exists(unsupportedPath) &&
            (File.ReadAllText(unsupportedPath).Contains("\"status\": \"Deferred\"", StringComparison.OrdinalIgnoreCase) ||
             File.ReadAllText(unsupportedPath).Contains("\"status\": \"Rejected\"", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new GenerationReadinessFinding("missing-mapping-for-critical-region", "blocking", "Unsupported critical pattern requires review before generation.", unsupportedPath));
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

            if (page.UnsupportedOrBlockedRegions.Any(region => region.StartsWith("unknown-page-archetype:", StringComparison.Ordinal)))
            {
                findings.Add(new GenerationReadinessFinding(
                    "unknown-page-archetype",
                    "blocking",
                    $"Page '{page.PageId}' declares an archetype that is not represented in the Storefront pattern contract.",
                    PageCompositionsArtifactPath));
            }
        }

        foreach (var composition in pageCompositions.Compositions)
        {
            foreach (var node in Flatten(composition.SectionTree))
            {
                if (node.UnresolvedIssues.Contains("missing-section-evidence", StringComparer.Ordinal))
                {
                    findings.Add(new GenerationReadinessFinding(
                        "missing-section-evidence",
                        "blocking",
                        $"Page '{composition.PageId}' section '{node.NodeId}' does not have source evidence.",
                        PageCompositionsArtifactPath));
                }

                if (node.UnresolvedIssues.Contains("protected-path-target", StringComparer.Ordinal))
                {
                    findings.Add(new GenerationReadinessFinding(
                        "protected-path-target",
                        "blocking",
                        $"Page '{composition.PageId}' section '{node.NodeId}' targets a protected path.",
                        PageCompositionsArtifactPath));
                }
            }
        }

        foreach (var issue in pageCompositions.Site.UnresolvedSiteLevelIssues)
        {
            var code = issue.Split(':', 2)[0];
            var isReviewedCompositionBlocker =
                code is "missing-reviewed-composition-input" or
                    "reviewed-composition-input-kind-mismatch" or
                    "reviewed-composition-project-id-mismatch" or
                    "reviewed-composition-hash-stale" or
                    "reviewed-composition-uses-draft-input";
            findings.Add(new GenerationReadinessFinding(
                isReviewedCompositionBlocker ? code : "site-composition-review",
                isReviewedCompositionBlocker ? "blocking" : "warning",
                issue,
                isReviewedCompositionBlocker ? "analysis/resolved/page-compositions.reviewed.json" : PageCompositionsArtifactPath));
        }

        findings.AddRange(new PageCompositionSlotValidator(repoRoot).Validate(root));

        if (validateReviewedBlueprint)
        {
            ValidateReviewedBlueprint(root, reviewResolution, findings);
        }

        return new GenerationReadinessReport("1.0", "generation-readiness", $"generation-readiness-{projectId}", DateTimeOffset.UtcNow, projectId, findings.All(finding => finding.Severity != "blocking"), findings);
    }

    private static IReadOnlyList<string> RequiredArtifacts() =>
    [
        PageCompositionsArtifactPath,
        "analysis/tokens/semantic-tokens.draft.json",
        "analysis/resolved/semantic-tokens.reviewed.json",
        "analysis/resolved/page-archetypes.reviewed.json",
        "analysis/resolved/page-sections.reviewed.json",
        "analysis/resolved/component-candidates.reviewed.json",
        "analysis/resolved/presentation-mappings.reviewed.json",
        "analysis/resolved/ecommerce-regions.reviewed.json",
        "analysis/resolved/unsupported-pattern-decisions.json",
        "analysis/resolved/originality-restrictions.reviewed.json",
        ReviewResolutionManifestPath,
        "analysis/components/component-candidates.json",
        "analysis/mapping/presentation-mappings.draft.json",
        "analysis/confidence/confidence-report.json",
        "presentation-catalog/presentation-component-catalog.json"
    ];

    private static Dictionary<string, string> BuildMetadata(
        VisualProject project,
        string root,
        ReviewedPageCompositionsDocument pageCompositions,
        ReviewResolutionManifest? reviewResolution)
    {
        var metadata = new Dictionary<string, string>
        {
            ["name"] = project.Name,
            ["referenceUrl"] = project.ReferenceUrl,
            ["siteId"] = pageCompositions.Site.SiteId,
            ["sitePageCount"] = pageCompositions.Pages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["storeArchetypeSummary"] = pageCompositions.Site.StoreArchetypeSummary,
            ["reviewBundleHash"] = reviewResolution?.DecisionBundleHash ?? string.Empty,
            ["storefrontPatternHash"] = FileHash(root, "analysis/storefront-pattern/storefront-pattern.json") ?? string.Empty,
            ["presentationCatalogHash"] = FileHash(root, "presentation-catalog/presentation-component-catalog.json") ?? string.Empty,
            ["pageContractHash"] = FileHash(root, "analysis/storefront-pattern/page-contracts.json") ?? string.Empty
        };
        return metadata;
    }

    private static void ValidateReviewedBlueprint(string root, ReviewResolutionManifest? reviewResolution, List<GenerationReadinessFinding> findings)
    {
        var reviewedPath = Path.Combine(root, ReviewedBlueprintPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(reviewedPath))
        {
            findings.Add(new GenerationReadinessFinding("reviewed-blueprint-not-resolved", "blocking", "Reviewed blueprint was not assembled because prerequisite blockers remain.", ReviewedBlueprintPath));
            return;
        }

        VisualBlueprintV1 blueprint;
        try
        {
            blueprint = JsonSerializer.Deserialize<VisualBlueprintV1>(File.ReadAllText(reviewedPath), VisualJson.Options)
                ?? throw new JsonException("empty blueprint");
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            findings.Add(new GenerationReadinessFinding("reviewed-blueprint-not-resolved", "blocking", $"Reviewed blueprint is invalid: {exception.Message}", ReviewedBlueprintPath));
            return;
        }

        if (AllBlueprintReferences(blueprint).Any(reference => reference.Contains(".draft.json", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(new GenerationReadinessFinding("reviewed-blueprint-references-draft", "blocking", "Reviewed blueprint references draft artifacts.", ReviewedBlueprintPath));
        }

        if (reviewResolution is null ||
            !blueprint.ProjectMetadata.TryGetValue("reviewBundleHash", out var reviewHash) ||
            !string.Equals(reviewHash, reviewResolution.DecisionBundleHash, StringComparison.Ordinal) ||
            !MatchesMetadataHash(root, blueprint, "storefrontPatternHash", "analysis/storefront-pattern/storefront-pattern.json") ||
            !MatchesMetadataHash(root, blueprint, "presentationCatalogHash", "presentation-catalog/presentation-component-catalog.json") ||
            !MatchesMetadataHash(root, blueprint, "pageContractHash", "analysis/storefront-pattern/page-contracts.json"))
        {
            findings.Add(new GenerationReadinessFinding("reviewed-blueprint-hash-stale", "blocking", "Reviewed blueprint hash metadata does not match current reviewed inputs.", ReviewedBlueprintPath));
        }
    }

    private static IEnumerable<string> AllBlueprintReferences(VisualBlueprintV1 blueprint)
    {
        foreach (var reference in blueprint.SourceProvenance) yield return reference;
        foreach (var reference in blueprint.PageArchetypes) yield return reference;
        yield return blueprint.Tokens;
        foreach (var reference in blueprint.Sections) yield return reference;
        foreach (var reference in blueprint.ResponsiveBehavior) yield return reference;
        foreach (var reference in blueprint.InteractionModels) yield return reference;
        yield return blueprint.ComponentDefinitions;
        yield return blueprint.ComponentInstances;
        foreach (var reference in blueprint.EcommerceRegions) yield return reference;
        yield return blueprint.PresentationMappings;
        yield return blueprint.UnsupportedPatterns;
        yield return blueprint.OriginalityRestrictions;
        yield return blueprint.Confidence;
        yield return blueprint.ReviewState;
    }

    private static bool MatchesMetadataHash(string root, VisualBlueprintV1 blueprint, string metadataKey, string relativePath) =>
        blueprint.ProjectMetadata.TryGetValue(metadataKey, out var value) &&
        !string.IsNullOrWhiteSpace(value) &&
        string.Equals(value, FileHash(root, relativePath), StringComparison.Ordinal);

    private static string? FileHash(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()
            : null;
    }

    private static ReviewResolutionManifest? ReadReviewResolutionManifest(string root)
    {
        var path = Path.Combine(root, ReviewResolutionManifestPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ReviewResolutionManifest>(File.ReadAllText(path), VisualJson.Options);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string WriteMarkdown(GenerationReadinessReport report) =>
        "# Generation Readiness" + Environment.NewLine + Environment.NewLine +
        $"Passed: `{report.Passed}`" + Environment.NewLine + Environment.NewLine +
        string.Join(Environment.NewLine, report.Findings.Select(finding => $"- `{finding.Code}` ({finding.Severity}): {finding.Message}")) + Environment.NewLine;

    private static ReviewedPageCompositionsDocument BuildReviewedPageCompositions(VisualProject project, string root)
    {
        var inputs = ReviewedCompositionInputReader.Read(root, project.ProjectId);
        return BuildPageCompositions(project, root, inputs);
    }

    private static ReviewedPageCompositionsDocument BuildDraftPageCompositions(VisualProject project, string root)
    {
        var inputs = DraftCompositionInputReader.Read(root);
        return BuildPageCompositions(project, root, inputs);
    }

    private static ReviewedPageCompositionsDocument BuildPageCompositions(VisualProject project, string root, CompositionInputs inputs)
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
        var presentationMappings = inputs.PresentationMappings;
        var unsupportedPatterns = inputs.UnsupportedPatterns;
        var sharedTokens = inputs.SharedTokens;
        var responsiveRules = ReadResponsiveRules(root);
        var siteIssues = new List<string>(inputs.BlockingIssues);
        var pageBlueprints = new List<PageBlueprint>();
        var compositions = new List<PageComposition>();
        var layoutSignatures = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var page in capturedPages.OrderBy(candidate => candidate.PageId, StringComparer.Ordinal))
        {
            var archetype = inputs.PageArchetypes.GetValueOrDefault(page.PageId) ?? "unknown";
            var sections = inputs.PageSections.GetValueOrDefault(page.PageId) ?? [];
            var roles = sections.Select(section => section.Role).Where(role => !string.IsNullOrWhiteSpace(role)).ToArray();
            var layoutSignature = string.Join("|", roles.Where(IsSharedLayoutRole).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase));
            layoutSignatures[page.PageId] = layoutSignature;
            var targetContract = MatchPageContract(pageContracts, page.PageId, archetype);
            var pageIssues = new List<string>();
            var drift = DetectArchetypeDrift(page.SourceUrl, archetype);
            if (targetContract is null)
            {
                pageIssues.Add($"unknown-page-archetype:{archetype}");
            }

            if (drift is not null)
            {
                pageIssues.Add(drift);
            }

            pageIssues.AddRange(unsupportedPatterns
                .Where(pattern => pattern.EvidenceIds.Count == 0 || pattern.EvidenceIds.Intersect(page.EvidenceIds, StringComparer.Ordinal).Any())
                .Select(pattern => $"unsupported:{pattern.Id}"));

            var sectionNodes = BuildSectionTree(
                page.PageId,
                sections,
                presentationMappings,
                inputs.EcommerceRegionsBySection.GetValueOrDefault(page.PageId) ?? new Dictionary<string, string>(StringComparer.Ordinal),
                sharedTokens,
                page.ArtifactPaths,
                page.ViewportIds,
                targetContract?.GeneratedPath);
            var repeatedGroups = BuildRepeatedGroups(sectionNodes);
            var pageResponsiveRules = ReadPageResponsiveRules(root, page.PageId);
            var unresolvedIssues = sectionNodes.SelectMany(node => node.UnresolvedIssues).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            pageBlueprints.Add(new PageBlueprint(
                page.PageId,
                archetype,
                page.SourceUrl,
                page.ArtifactPaths,
                page.ViewportIds,
                inputs.EcommerceRegionIds.GetValueOrDefault(page.PageId) ?? [],
                presentationMappings
                    .Where(mapping => mapping.EvidenceIds.Count == 0 || mapping.EvidenceIds.Intersect(page.EvidenceIds, StringComparer.Ordinal).Any())
                    .Select(mapping => mapping.Id)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                sectionNodes,
                inputs.PageTokenOverrides.GetValueOrDefault(page.PageId) ?? new Dictionary<string, string>(StringComparer.Ordinal),
                targetContract?.SlotId,
                targetContract?.GeneratedPath,
                pageIssues.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));

            compositions.Add(new PageComposition(
                page.PageId,
                archetype,
                targetContract?.SlotId,
                sectionNodes,
                sectionNodes.Select(node => node.Role).Where(IsSharedLayoutRole).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                repeatedGroups,
                pageResponsiveRules,
                page.ArtifactPaths.Concat(sectionNodes.SelectMany(node => node.ScreenshotReferences)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                unresolvedIssues));
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
            inputs.Provenance,
            site,
            pageBlueprints,
            compositions);
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
                StringArray(section, "evidenceIds"),
                StringValue(section, "parentSectionId"),
                StringArray(section, "childSectionIds"),
                StringValue(section, "crossViewportIdentityKey") ?? $"section-{index + 1}",
                ReadViewportBounds(section),
                StringArray(section, "reasonCodes")))
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

    private static IReadOnlyList<string> ReadPageResponsiveRules(string root, string pageId)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "pages", pageId, "responsive-behavior.json"));
        var flags = node?["sections"]?.AsArray().OfType<JsonObject>()
            .SelectMany(section => StringArray(section, "behaviorFlags"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return flags is { Length: > 0 } ? flags : [];
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
                StringArray(mapping, "evidenceIds"),
                StringValue(mapping, "targetGeneratedPath"),
                StringValue(mapping, "generatedZone"),
                StringValue(mapping, "sourcePageId"),
                StringValue(mapping, "sourceSectionId")))
            .ToArray()
            ?? [];
    }

    private static IReadOnlyList<MappingInfo> ReadUnsupportedPatterns(string root)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "mapping", "unsupported-patterns.json"));
        return node?["patterns"]?.AsArray().OfType<JsonObject>()
            .Select((pattern, index) => new MappingInfo(
                StringValue(pattern, "patternId") ?? StringValue(pattern, "sourceCandidateId") ?? $"unsupported-{index + 1}",
                StringArray(pattern, "evidenceIds"),
                null,
                null))
            .ToArray()
            ?? [];
    }

    private static IReadOnlyDictionary<string, string> ReadEcommerceRegionsBySection(string root, string pageId)
    {
        var node = TryReadNode(Path.Combine(root, "analysis", "pages", pageId, "ecommerce-regions.json"));
        var regions = node?["regions"]?.AsArray();
        if (regions is null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var region in regions.OfType<JsonObject>())
        {
            var role = StringValue(region, "role") ?? string.Empty;
            foreach (var sectionId in StringArray(region, "sourceSectionIds"))
            {
                result[sectionId] = role;
            }
        }

        return result;
    }

    private static IReadOnlyList<PageCompositionNode> BuildSectionTree(
        string pageId,
        IReadOnlyList<PageSectionInfo> sections,
        IReadOnlyList<MappingInfo> mappings,
        IReadOnlyDictionary<string, string> ecommerceRoles,
        IReadOnlyDictionary<string, string> sharedTokens,
        IReadOnlyList<string> captureArtifacts,
        IReadOnlyList<string> viewportIds,
        string? pageTargetPath)
    {
        var groupIds = sections
            .GroupBy(section => section.Role, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1 || IsRepeatedRole(group.Key))
            .ToDictionary(group => group.Key, group => "group-" + StableId(group.Key), StringComparer.OrdinalIgnoreCase);
        var nodes = sections.Select(section =>
        {
            var mapping = mappings.FirstOrDefault(candidate =>
                    string.Equals(candidate.SourcePageId, pageId, StringComparison.Ordinal) &&
                    string.Equals(candidate.SourceSectionId, section.Id, StringComparison.Ordinal))
                ?? mappings.FirstOrDefault(candidate =>
                    (string.IsNullOrWhiteSpace(candidate.SourcePageId) || string.Equals(candidate.SourcePageId, pageId, StringComparison.Ordinal)) &&
                    candidate.EvidenceIds.Intersect(section.EvidenceIds, StringComparer.Ordinal).Any());
            var targetFile = mapping?.TargetGeneratedPath ?? pageTargetPath;
            var unresolved = new List<string>();
            if (section.EvidenceIds.Count == 0)
            {
                unresolved.Add("missing-section-evidence");
            }

            if (targetFile?.Contains("starter-generation.contract.yaml", StringComparison.OrdinalIgnoreCase) == true ||
                targetFile?.Contains("StorefrontPackageVersions.props", StringComparison.OrdinalIgnoreCase) == true)
            {
                unresolved.Add("protected-path-target");
            }

            return new PageCompositionNode(
                section.Id,
                section.Role,
                mapping?.Id,
                section.EvidenceIds,
                [],
                StableId($"{section.Role}:{section.IdentityKey}:{string.Join(",", section.EvidenceIds)}"),
                ecommerceRoles.GetValueOrDefault(section.Id),
                section.ParentId,
                section.ChildIds,
                SectionBoundsForViewports(section, viewportIds),
                sharedTokens.Keys.Order(StringComparer.Ordinal).Take(8).ToArray(),
                mapping?.Id,
                targetFile,
                mapping?.GeneratedZone ?? GeneratedZoneForPath(targetFile ?? string.Empty),
                AllowedOperationsFor(section.Role),
                ProtectedMarkersFor(section.Role, mapping?.Id),
                captureArtifacts.Where(path => path.Contains("screenshot", StringComparison.OrdinalIgnoreCase) || path.Contains("manifest.json", StringComparison.OrdinalIgnoreCase)).Take(6).ToArray(),
                [],
                null,
                null,
                groupIds.GetValueOrDefault(section.Role),
                StateExpectationsFor(section.Role),
                section.ReasonCodes.Where(code => code.Contains("responsive", StringComparison.OrdinalIgnoreCase)).ToArray(),
                unresolved);
        }).ToArray();

        var byParent = nodes.GroupBy(node => node.ParentNodeId ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        PageCompositionNode Attach(PageCompositionNode node) =>
            node with { Children = byParent.GetValueOrDefault(node.NodeId, []).Select(Attach).ToArray() };
        return nodes.Where(node => string.IsNullOrWhiteSpace(node.ParentNodeId)).Select(Attach).ToArray();
    }

    private static IReadOnlyList<PageRepeatedGroup> BuildRepeatedGroups(IReadOnlyList<PageCompositionNode> nodes)
    {
        var flattened = Flatten(nodes).ToArray();
        return flattened
            .Where(node => !string.IsNullOrWhiteSpace(node.RepeatedGroupId))
            .GroupBy(node => node.RepeatedGroupId!, StringComparer.Ordinal)
            .Select(group => new PageRepeatedGroup(
                group.Key,
                group.First().Role,
                group.Select(node => node.NodeId).Order(StringComparer.Ordinal).ToArray(),
                group.First().TargetFilePath))
            .OrderBy(group => group.GroupId, StringComparer.Ordinal)
            .ToArray();
    }

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

    private static IReadOnlyDictionary<string, string> ReadViewportBounds(JsonObject section)
    {
        var viewportBounds = section["viewportBoundingBoxes"] as JsonObject;
        if (viewportBounds is not null)
        {
            var values = viewportBounds
                .Where(pair => pair.Value is JsonObject)
                .ToDictionary(pair => pair.Key, pair => FormatBounds(pair.Value!.AsObject()), StringComparer.Ordinal);
            if (values.Count > 0)
            {
                return values;
            }
        }

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["base"] = ReadBounds(section)
        };
    }

    private static IReadOnlyDictionary<string, string> SectionBoundsForViewports(PageSectionInfo section, IReadOnlyList<string> viewportIds)
    {
        var exact = section.ViewportBounds
            .Where(pair => !string.Equals(pair.Key, "base", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        if (exact.Count > 0)
        {
            return exact;
        }

        if (viewportIds.Count == 1 && section.ViewportBounds.TryGetValue("base", out var singleViewportBounds))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [viewportIds[0]] = singleViewportBounds
            };
        }

        return new Dictionary<string, string>(section.ViewportBounds, StringComparer.Ordinal);
    }

    private static string ReadBounds(JsonObject section)
    {
        var bounds = section["bounds"] as JsonObject;
        if (bounds is null)
        {
            return "x=0;y=0;width=0;height=0";
        }

        return FormatBounds(bounds);
    }

    private static string FormatBounds(JsonObject bounds)
    {
        return $"x={StringValue(bounds, "x") ?? "0"};y={StringValue(bounds, "y") ?? "0"};width={StringValue(bounds, "width") ?? "0"};height={StringValue(bounds, "height") ?? "0"}";
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

    private static bool IsRepeatedRole(string role) =>
        role.Contains("product card", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("grid", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("thumbnail", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("menu", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("footer column", StringComparison.OrdinalIgnoreCase) ||
        role.Contains("promotion", StringComparison.OrdinalIgnoreCase);

    private static string StableId(string value)
    {
        var normalized = new string(value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
        return string.Join("-", normalized.Split('-', StringSplitOptions.RemoveEmptyEntries)).Trim('-');
    }

    private static string GeneratedZoneForPath(string path) =>
        path.StartsWith("Pages/", StringComparison.OrdinalIgnoreCase) ? "pages" :
        path.StartsWith("Components/Layout/", StringComparison.OrdinalIgnoreCase) ? "layout-components" :
        path.StartsWith("Components/Catalog/", StringComparison.OrdinalIgnoreCase) ? "catalog-components" :
        path.StartsWith("Components/", StringComparison.OrdinalIgnoreCase) ? "components" :
        string.IsNullOrWhiteSpace(path) ? "none" :
        "unknown";

    private static IReadOnlyList<string> AllowedOperationsFor(string role) =>
        role.Contains("state", StringComparison.OrdinalIgnoreCase)
            ? ["visual-markup", "css", "empty-loading-error-state-copy"]
            : ["visual-markup", "css", "responsive-layout"];

    private static IReadOnlyList<string> ProtectedMarkersFor(string role, string? mappingId)
    {
        var markers = new List<string>();
        if (role.Contains("cart", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("checkout", StringComparison.OrdinalIgnoreCase) ||
            role.Contains("purchase", StringComparison.OrdinalIgnoreCase))
        {
            markers.Add("preserve-action-descriptor");
            markers.Add("no-direct-storefront-api");
        }

        if (!string.IsNullOrWhiteSpace(mappingId))
        {
            markers.Add("preserve-presentation-mapping");
        }

        return markers;
    }

    private static IReadOnlyList<string> StateExpectationsFor(string role)
    {
        var states = new List<string>();
        foreach (var state in new[] { "empty", "loading", "error", "disabled", "unavailable" })
        {
            if (role.Contains(state, StringComparison.OrdinalIgnoreCase))
            {
                states.Add(state);
            }
        }

        return states;
    }

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
        IReadOnlyList<string> EvidenceIds,
        string? ParentId,
        IReadOnlyList<string> ChildIds,
        string IdentityKey,
        IReadOnlyDictionary<string, string> ViewportBounds,
        IReadOnlyList<string> ReasonCodes);

    private sealed record PageContractInfo(
        string PageId,
        string Archetype,
        IReadOnlyList<string> Routes,
        string? SlotId,
        string? GeneratedPath);

    private sealed record MappingInfo(
        string Id,
        IReadOnlyList<string> EvidenceIds,
        string? TargetGeneratedPath,
        string? GeneratedZone,
        string? SourcePageId = null,
        string? SourceSectionId = null);

    private sealed record CompositionInputs(
        IReadOnlyDictionary<string, string> PageArchetypes,
        IReadOnlyDictionary<string, IReadOnlyList<PageSectionInfo>> PageSections,
        IReadOnlyDictionary<string, string> SharedTokens,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PageTokenOverrides,
        IReadOnlyList<MappingInfo> PresentationMappings,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> EcommerceRegionsBySection,
        IReadOnlyDictionary<string, IReadOnlyList<string>> EcommerceRegionIds,
        IReadOnlyList<MappingInfo> UnsupportedPatterns,
        ReviewedPageCompositionProvenance Provenance,
        IReadOnlyList<string> BlockingIssues);

    private static class DraftCompositionInputReader
    {
        public static CompositionInputs Read(string root)
        {
            var pageIds = Directory.Exists(Path.Combine(root, "analysis", "pages"))
                ? Directory.EnumerateDirectories(Path.Combine(root, "analysis", "pages"))
                    .Select(Path.GetFileName)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id!)
                    .Order(StringComparer.Ordinal)
                    .ToArray()
                : [];
            var sharedTokens = ReadSharedTokens(root);
            var provenance = new ReviewedPageCompositionProvenance(
                string.Empty,
                string.Empty,
                new Dictionary<string, string>(StringComparer.Ordinal),
                [],
                new Dictionary<string, string>(StringComparer.Ordinal));

            return new CompositionInputs(
                pageIds.ToDictionary(pageId => pageId, pageId => ReadPageArchetype(root, pageId), StringComparer.Ordinal),
                pageIds.ToDictionary(pageId => pageId, pageId => ReadPageSections(root, pageId), StringComparer.Ordinal),
                sharedTokens,
                pageIds.ToDictionary(pageId => pageId, pageId => ReadPageTokenOverrides(root, pageId, sharedTokens), StringComparer.Ordinal),
                ReadPresentationMappings(root),
                pageIds.ToDictionary(pageId => pageId, pageId => ReadEcommerceRegionsBySection(root, pageId), StringComparer.Ordinal),
                pageIds.ToDictionary(pageId => pageId, pageId => ReadEcommerceRegionIds(root, pageId), StringComparer.Ordinal),
                ReadUnsupportedPatterns(root),
                provenance,
                []);
        }
    }

    private static class ReviewedCompositionInputReader
    {
        private const string PageArchetypesPath = "analysis/resolved/page-archetypes.reviewed.json";
        private const string PageSectionsPath = "analysis/resolved/page-sections.reviewed.json";
        private const string SemanticTokensPath = "analysis/resolved/semantic-tokens.reviewed.json";
        private const string PresentationMappingsPath = "analysis/resolved/presentation-mappings.reviewed.json";
        private const string EcommerceRegionsPath = "analysis/resolved/ecommerce-regions.reviewed.json";
        private const string OriginalityRestrictionsPath = "analysis/resolved/originality-restrictions.reviewed.json";
        private const string UnsupportedDecisionsPath = "analysis/resolved/unsupported-pattern-decisions.json";

        private static readonly (string Path, string Kind)[] RequiredInputs =
        [
            (PageArchetypesPath, "reviewed-page-archetypes"),
            (PageSectionsPath, "reviewed-page-sections"),
            (SemanticTokensPath, "reviewed-semantic-tokens"),
            (PresentationMappingsPath, "reviewed-presentation-mappings"),
            (EcommerceRegionsPath, "reviewed-ecommerce-regions"),
            (OriginalityRestrictionsPath, "reviewed-originality-restrictions"),
            (ReviewResolutionManifestPath, "review-resolution-manifest")
        ];

        public static CompositionInputs Read(string root, string projectId)
        {
            var issues = new List<string>();
            var nodes = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            foreach (var input in RequiredInputs)
            {
                var node = ReadReviewedObject(root, input.Path, input.Kind, projectId, issues);
                if (node is not null)
                {
                    nodes[input.Path] = node;
                }
            }

            var manifest = nodes.TryGetValue(ReviewResolutionManifestPath, out var manifestNode)
                ? JsonSerializer.Deserialize<ReviewResolutionManifest>(manifestNode.ToJsonString(), VisualJson.Options)
                : null;
            var manifestArtifacts = manifest?.ResolvedArtifacts.ToHashSet(StringComparer.Ordinal) ?? [];
            foreach (var input in RequiredInputs.Where(input => input.Path != ReviewResolutionManifestPath))
            {
                if (nodes.ContainsKey(input.Path) && !manifestArtifacts.Contains(input.Path))
                {
                    issues.Add($"reviewed-composition-hash-stale:{input.Path}:not-listed-in-resolution-manifest");
                }
            }

            var hashes = nodes.Keys
                .Order(StringComparer.Ordinal)
                .ToDictionary(path => path, path => FileHash(root, path) ?? string.Empty, StringComparer.Ordinal);
            var kinds = nodes
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => StringValue(pair.Value, "artifactKind") ?? string.Empty, StringComparer.Ordinal);
            var provenance = new ReviewedPageCompositionProvenance(
                ReviewResolutionManifestPath,
                manifest?.DecisionBundleHash ?? string.Empty,
                hashes,
                nodes.Keys.Order(StringComparer.Ordinal).ToArray(),
                kinds);

            return new CompositionInputs(
                ReadReviewedPageArchetypes(nodes.GetValueOrDefault(PageArchetypesPath)),
                ReadReviewedPageSections(nodes.GetValueOrDefault(PageSectionsPath)),
                ReadReviewedSemanticTokens(nodes.GetValueOrDefault(SemanticTokensPath)),
                new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal),
                ReadReviewedPresentationMappings(nodes.GetValueOrDefault(PresentationMappingsPath)),
                ReadReviewedEcommerceRegionsBySection(nodes.GetValueOrDefault(EcommerceRegionsPath)),
                ReadReviewedEcommerceRegionIds(nodes.GetValueOrDefault(EcommerceRegionsPath)),
                ReadUnsupportedDecisions(root, projectId, issues),
                provenance,
                issues);
        }

        private static JsonObject? ReadReviewedObject(string root, string relativePath, string expectedKind, string projectId, List<string> issues)
        {
            if (relativePath.Contains(".draft.", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"reviewed-composition-uses-draft-input:{relativePath}");
                return null;
            }

            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                issues.Add($"missing-reviewed-composition-input:{relativePath}");
                return null;
            }

            var node = TryReadNode(path);
            if (node is null)
            {
                issues.Add($"missing-reviewed-composition-input:{relativePath}:invalid-json");
                return null;
            }

            var kind = StringValue(node, "artifactKind");
            if (!string.Equals(kind, expectedKind, StringComparison.Ordinal))
            {
                issues.Add($"reviewed-composition-input-kind-mismatch:{relativePath}:{kind ?? "missing"}");
            }

            var artifactProjectId = StringValue(node, "projectId");
            if (!string.IsNullOrWhiteSpace(artifactProjectId) &&
                !string.Equals(artifactProjectId, projectId, StringComparison.Ordinal))
            {
                issues.Add($"reviewed-composition-project-id-mismatch:{relativePath}:{artifactProjectId}");
            }

            return node;
        }

        private static IReadOnlyDictionary<string, string> ReadReviewedPageArchetypes(JsonObject? node) =>
            node?["pages"]?.AsArray().OfType<JsonObject>()
                .Select(page => new
                {
                    PageId = StringValue(page, "pageId"),
                    Archetype = StringValue(page, "primaryArchetype") ?? StringValue(page, "archetype")
                })
                .Where(page => !string.IsNullOrWhiteSpace(page.PageId) && !string.IsNullOrWhiteSpace(page.Archetype))
                .ToDictionary(page => page.PageId!, page => page.Archetype!, StringComparer.Ordinal)
            ?? new Dictionary<string, string>(StringComparer.Ordinal);

        private static IReadOnlyDictionary<string, IReadOnlyList<PageSectionInfo>> ReadReviewedPageSections(JsonObject? node) =>
            node?["pages"]?.AsArray().OfType<JsonObject>()
                .Select(page =>
                {
                    var pageId = StringValue(page, "pageId") ?? string.Empty;
                    var sections = page["sections"]?.AsArray().OfType<JsonObject>()
                        .Select((section, index) => new PageSectionInfo(
                            StringValue(section, "sectionId") ?? StringValue(section, "id") ?? $"section-{index + 1}",
                            StringValue(section, "sectionType") ?? StringValue(section, "role") ?? "unknown section",
                            StringArray(section, "evidenceIds"),
                            StringValue(section, "parentSectionId"),
                            StringArray(section, "childSectionIds"),
                            StringValue(section, "crossViewportIdentityKey") ?? $"section-{index + 1}",
                            ReadViewportBounds(section),
                            StringArray(section, "reasonCodes")))
                        .ToArray() ?? [];
                    return (pageId, sections);
                })
                .Where(page => !string.IsNullOrWhiteSpace(page.pageId))
                .ToDictionary(page => page.pageId, page => (IReadOnlyList<PageSectionInfo>)page.sections, StringComparer.Ordinal)
            ?? new Dictionary<string, IReadOnlyList<PageSectionInfo>>(StringComparer.Ordinal);

        private static IReadOnlyDictionary<string, string> ReadReviewedSemanticTokens(JsonObject? node)
        {
            var tokens = node?["tokens"]?.AsArray();
            if (tokens is null)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            return tokens.OfType<JsonObject>()
                .Select(token => new
                {
                    Role = StringValue(token, "role") ?? StringValue(token, "tokenId"),
                    Value = FirstNonEmpty(
                        StringValue(token, "value"),
                        StringValue(token, "normalizedValue"),
                        Join(StringArray(token, "normalizedValues")),
                        Join(StringArray(token, "values")),
                        StringValue(token, "rawValue"))
                })
                .Where(token => !string.IsNullOrWhiteSpace(token.Role) && !string.IsNullOrWhiteSpace(token.Value))
                .GroupBy(token => token.Role!, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => string.Join(", ", group.Select(token => token.Value!).Distinct(StringComparer.Ordinal)), StringComparer.Ordinal);
        }

        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        private static string? Join(IReadOnlyList<string> values) =>
            values.Count == 0 ? null : string.Join(", ", values);

        private static IReadOnlyList<MappingInfo> ReadReviewedPresentationMappings(JsonObject? node) =>
            node?["mappings"]?.AsArray().OfType<JsonObject>()
                .Select((mapping, index) => new MappingInfo(
                    StringValue(mapping, "sourceCandidateId") ?? StringValue(mapping, "mappingId") ?? $"mapping-{index + 1}",
                    StringArray(mapping, "evidenceIds"),
                    StringValue(mapping, "targetGeneratedPath"),
                    StringValue(mapping, "generatedZone"),
                    StringValue(mapping, "sourcePageId"),
                    StringValue(mapping, "sourceSectionId")))
                .ToArray()
            ?? [];

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ReadReviewedEcommerceRegionsBySection(JsonObject? node)
        {
            var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
            foreach (var page in node?["pages"]?.AsArray().OfType<JsonObject>() ?? [])
            {
                var pageId = StringValue(page, "pageId");
                if (string.IsNullOrWhiteSpace(pageId))
                {
                    continue;
                }

                var sections = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var region in page["regions"]?.AsArray().OfType<JsonObject>() ?? [])
                {
                    var role = StringValue(region, "role") ?? string.Empty;
                    foreach (var sectionId in StringArray(region, "sourceSectionIds"))
                    {
                        sections[sectionId] = role;
                    }
                }

                result[pageId] = sections;
            }

            return result;
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadReviewedEcommerceRegionIds(JsonObject? node) =>
            node?["pages"]?.AsArray().OfType<JsonObject>()
                .Select(page =>
                {
                    var pageId = StringValue(page, "pageId") ?? string.Empty;
                    var regions = page["regions"]?.AsArray().OfType<JsonObject>()
                        .Select((region, index) => StringValue(region, "regionId") ?? StringValue(region, "id") ?? $"region-{index + 1}")
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)
                        .ToArray() ?? [];
                    return (pageId, regions);
                })
                .Where(page => !string.IsNullOrWhiteSpace(page.pageId))
                .ToDictionary(page => page.pageId, page => (IReadOnlyList<string>)page.regions, StringComparer.Ordinal)
            ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        private static IReadOnlyList<MappingInfo> ReadUnsupportedDecisions(string root, string projectId, List<string> issues)
        {
            var node = ReadReviewedObject(root, UnsupportedDecisionsPath, "unsupported-pattern-decisions", projectId, issues);
            return node?["decisions"]?.AsArray().OfType<JsonObject>()
                .Where(decision => !string.Equals(StringValue(decision, "status"), "Approved", StringComparison.OrdinalIgnoreCase))
                .Select((decision, index) => new MappingInfo(
                    StringValue(decision, "itemId") ?? $"unsupported-{index + 1}",
                    [],
                    null,
                    null))
                .ToArray()
            ?? [];
        }
    }
}
