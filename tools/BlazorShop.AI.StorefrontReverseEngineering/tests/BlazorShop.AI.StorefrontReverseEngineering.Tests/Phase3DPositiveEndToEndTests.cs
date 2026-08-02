using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Blueprint;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Workflows;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "ClosureProof")]
public sealed class Phase3DPositiveEndToEndTests
{
    [Fact]
    public async Task PositivePipeline_ProducesReadySelfContainedHandoff()
    {
        var projectRoot = await CreatePositiveProjectAsync("Phase 3D Positive E2E");

        var reviewed = await ReadAsync<VisualBlueprintV1>(projectRoot, "analysis/visual-blueprint.v1.reviewed.json");
        var compositions = await ReadAsync<ReviewedPageCompositionsDocument>(projectRoot, "analysis/resolved/page-compositions.reviewed.json");
        var evidence = await ReadAsync<AgentHandoffEvidenceManifest>(projectRoot, "analysis/agent-handoff/evidence-manifest.json");
        var readiness = await ReadAsync<AgentHandoffReadinessReport>(projectRoot, "analysis/agent-handoff/handoff-readiness.json");
        var generation = await ReadAsync<GenerationReadinessReport>(projectRoot, "reports/generation-readiness.json");
        var inspect = await RunInspectAsync(projectRoot);

        Assert.True(generation.Passed, string.Join(Environment.NewLine, generation.Findings.Select(finding => finding.Code + ":" + finding.Message)));
        Assert.True(readiness.Passed, string.Join(Environment.NewLine, readiness.Findings.Select(finding => finding.Code + ":" + finding.Message)));
        Assert.DoesNotContain(BlueprintReferences(reviewed), reference => reference.Contains(".draft.json", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["account", "cart", "category", "checkout", "home", "maintenance", "product"], compositions.Compositions.Select(composition => composition.PageId).Order(StringComparer.Ordinal));
        AssertExactSlots(projectRoot, compositions, "home", ["layout.header", "home.sections", "layout.footer"]);
        AssertExactSlots(projectRoot, compositions, "category", ["layout.header", "catalog.product-card", "layout.footer"]);
        AssertExactSlots(projectRoot, compositions, "product", ["layout.header", "product.gallery", "product.information", "product.purchase", "layout.footer"]);
        AssertExactSlots(projectRoot, compositions, "cart", ["layout.header", "cart.page", "layout.footer"]);
        AssertExactSlots(projectRoot, compositions, "checkout", ["layout.header", "checkout.page", "layout.footer"]);
        AssertExactSlots(projectRoot, compositions, "account", ["layout.header", "account.shell", "layout.footer"]);
        AssertExactSlots(projectRoot, compositions, "maintenance", ["layout.header", "system.error", "layout.footer"]);
        foreach (var page in evidence.Pages)
        {
            Assert.Equal(["desktop-1440", "mobile-390", "tablet-768"], page.Screenshots.Select(screenshot => screenshot.ViewportId).Order(StringComparer.Ordinal));
            Assert.Contains(page.Sections, section => section.ViewportId == "desktop-1440");
            Assert.Contains(page.Sections, section => section.ViewportId == "mobile-390");
            Assert.Contains(page.Sections, section => section.ViewportId == "tablet-768");
            Assert.All(page.Screenshots.Select(screenshot => screenshot.HandoffPath).Concat(page.Sections.Select(section => section.HandoffPath)), path => Assert.StartsWith("analysis/agent-handoff/", path, StringComparison.Ordinal));
        }

        Assert.Contains("Run status: Succeeded", inspect, StringComparison.Ordinal);
        Assert.Contains("Final handoff readiness: true", inspect, StringComparison.Ordinal);
        Assert.Contains("Latest final blocker: (none)", inspect, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PositivePipeline_PropagatesModifiedDecisionsToHandoff()
    {
        var projectRoot = await CreatePositiveProjectAsync("Phase 3D Positive Modified");
        var resolvedSections = ReadNode(projectRoot, "analysis/resolved/page-sections.reviewed.json").ToJsonString();
        var compositions = ReadNode(projectRoot, "analysis/resolved/page-compositions.reviewed.json").ToJsonString();
        var handoffCompositions = ReadNode(projectRoot, "analysis/agent-handoff/page-compositions.json").ToJsonString();
        var task = await File.ReadAllTextAsync(Path.Combine(projectRoot, "analysis", "agent-handoff", "task.md"));

        Assert.Contains("featured hero", resolvedSections, StringComparison.Ordinal);
        Assert.Contains("featured hero", compositions, StringComparison.Ordinal);
        Assert.Contains("featured hero", handoffCompositions, StringComparison.Ordinal);
        Assert.Contains("featured hero", task, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PositivePipeline_IsDeterministicForStableInputs()
    {
        var projectRoot = await CreatePositiveProjectAsync("Phase 3D Positive Deterministic");
        var firstEvidence = StableEvidenceHashes(projectRoot);
        var firstCompositionIds = StableCompositionIds(projectRoot);

        var blueprint = await new BlueprintV1Assembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        Assert.True(blueprint.Readiness.Passed, string.Join(Environment.NewLine, blueprint.Readiness.Findings.Select(finding => finding.Code + ":" + finding.Message)));
        await new AgentHandoffAssembler(GetRepoRoot()).AssembleAsync(projectRoot, CancellationToken.None);
        var secondEvidence = StableEvidenceHashes(projectRoot);
        var secondCompositionIds = StableCompositionIds(projectRoot);

        Assert.Equal(firstEvidence, secondEvidence);
        Assert.Equal(firstCompositionIds, secondCompositionIds);
    }

    internal static async Task<string> CreatePositiveProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "phase3d-positive-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var service = new VisualProjectWorkflowService(repoRoot);
        var summary = await service.RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "phase3d-positive");
        Assert.True(summary.ReadinessPassed);
        await ExtendToPositiveMultiPageProjectAsync(summary.ArtifactRoot);
        await WriteModifiedReviewDecisionsAsync(summary.ArtifactRoot);
        var resumed = await service.RunAsync(fixtureUrl, name, outputRoot, force: false, resume: true, noAi: true, CancellationToken.None, runId: "phase3d-positive", forceStep: "assemble-blueprint-v1");
        Assert.Equal(WorkflowRunStatus.Succeeded, resumed.RunStatus);
        var readiness = await new AgentHandoffReadinessValidator(repoRoot).ValidateAsync(summary.ArtifactRoot, CancellationToken.None);
        Assert.True(readiness.Passed, string.Join(Environment.NewLine, readiness.Findings.Select(finding => finding.Code + ":" + finding.Message)));
        return summary.ArtifactRoot;
    }

    private static async Task ExtendToPositiveMultiPageProjectAsync(string projectRoot)
    {
        await ClonePageAsync(projectRoot, "category", "https://example.test/category/women", "product-listing", [("section-01", "header"), ("section-02", "product card"), ("section-03", "footer")]);
        await ClonePageAsync(projectRoot, "product", "https://example.test/product/linen-jacket", "product-detail", [("section-01", "header"), ("section-02", "product gallery"), ("section-03", "product information"), ("section-04", "purchase actions"), ("section-05", "footer")]);
        await ClonePageAsync(projectRoot, "cart", "https://example.test/cart", "cart-shell", [("section-01", "header"), ("section-02", "cart page"), ("section-03", "footer")]);
        await ClonePageAsync(projectRoot, "checkout", "https://example.test/checkout", "checkout-shell", [("section-01", "header"), ("section-02", "checkout page"), ("section-03", "footer")]);
        await ClonePageAsync(projectRoot, "account", "https://example.test/account", "account-auth-shell", [("section-01", "header"), ("section-02", "account shell"), ("section-03", "footer")]);
        await ClonePageAsync(projectRoot, "maintenance", "https://example.test/maintenance", "maintenance", [("section-01", "header"), ("section-02", "system error state"), ("section-03", "footer")]);

        await AddMappingAsync(projectRoot, "category", "section-01", "layout.header", "layout.header", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "category", "section-02", "catalog.product-card", "catalog.product-card", "Components/Catalog/ProductSummaryCard.razor", "catalog-components");
        await AddMappingAsync(projectRoot, "category", "section-03", "layout.footer", "layout.footer", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "product", "section-01", "layout.header", "layout.header", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "product", "section-02", "product.gallery", "product.gallery", "Components/Catalog/ProductGalleryPlaceholder.razor", "catalog-components");
        await AddMappingAsync(projectRoot, "product", "section-03", "product.information", "product.information", "Components/Catalog/ProductDetailShell.razor", "catalog-components");
        await AddMappingAsync(projectRoot, "product", "section-04", "product.purchase", "product.purchase", "Components/Catalog/PurchasePanelPlaceholder.razor", "catalog-components");
        await AddMappingAsync(projectRoot, "product", "section-05", "layout.footer", "layout.footer", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "cart", "section-01", "layout.header", "layout.header", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "cart", "section-02", "cart.page", "cart.page", "Pages/Hybrid/Cart/CartPage.razor", "pages");
        await AddMappingAsync(projectRoot, "cart", "section-03", "layout.footer", "layout.footer", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "checkout", "section-01", "layout.header", "layout.header", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "checkout", "section-02", "checkout.page", "checkout.page", "Pages/Hybrid/Checkout/CheckoutPage.razor", "pages");
        await AddMappingAsync(projectRoot, "checkout", "section-03", "layout.footer", "layout.footer", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "account", "section-01", "layout.header", "layout.header", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "account", "section-02", "account.shell", "account.shell", "Pages/Hybrid/Account/AccountPage.razor", "pages");
        await AddMappingAsync(projectRoot, "account", "section-03", "layout.footer", "layout.footer", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "maintenance", "section-01", "layout.header", "layout.header", "Components/Layout/MainLayout.razor", "layout-components");
        await AddMappingAsync(projectRoot, "maintenance", "section-02", "system.error", "system.error", "Pages/Ssr/System/MaintenancePage.razor", "pages");
        await AddMappingAsync(projectRoot, "maintenance", "section-03", "layout.footer", "layout.footer", "Components/Layout/MainLayout.razor", "layout-components");
    }

    private static async Task ClonePageAsync(string projectRoot, string pageId, string sourceUrl, string archetype, IReadOnlyList<(string SectionId, string Role)> sections)
    {
        CopyDirectory(Path.Combine(projectRoot, "analysis", "pages", "home"), Path.Combine(projectRoot, "analysis", "pages", pageId));
        CopyDirectory(Path.Combine(projectRoot, "captures", "home"), Path.Combine(projectRoot, "captures", pageId));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(projectRoot, "analysis", "pages", pageId), "*.json", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(Path.Combine(projectRoot, "captures", pageId), "*.json", SearchOption.AllDirectories)))
        {
            var node = JsonNode.Parse(await File.ReadAllTextAsync(file))!;
            ReplaceStrings(node, "/home/", $"/{pageId}/");
            ReplaceStrings(node, "home-", $"{pageId}-");
            ReplaceStrings(node, "home", pageId);
            ReplaceStrings(node, "ev-", $"ev-{pageId}-");
            if (Path.GetFileName(file).Equals("page-archetype.json", StringComparison.Ordinal))
            {
                node["pageId"] = pageId;
                node["sourceUrl"] = sourceUrl;
                node["primaryArchetype"] = archetype;
            }

            if (Path.GetFileName(file).Equals("sections.draft.json", StringComparison.Ordinal))
            {
                node["pageId"] = pageId;
                RewriteSections(node["sections"]!.AsArray(), pageId, sections);
            }

            await File.WriteAllTextAsync(file, node.ToJsonString(VisualJson.Options));
        }

        await AddEvidenceSnapshotPageAsync(projectRoot, pageId, sourceUrl, archetype);
    }

    private static void RewriteSections(JsonArray array, string pageId, IReadOnlyList<(string SectionId, string Role)> sections)
    {
        var templates = array.OfType<JsonObject>().ToArray();
        array.Clear();
        for (var index = 0; index < sections.Count; index++)
        {
            var template = (templates.ElementAtOrDefault(Math.Min(index, templates.Length - 1)) ?? templates[0]).DeepClone().AsObject();
            template["sectionId"] = sections[index].SectionId;
            template["sectionType"] = sections[index].Role;
            template["order"] = index + 1;
            template["crossViewportIdentityKey"] = sections[index].Role.Replace(' ', '-') + "-" + (index + 1).ToString("00");
            template["evidenceIds"] = new JsonArray($"ev-{pageId}-desktop-1440-{index + 1:000}");
            array.Add(template);
        }
    }

    private static async Task AddEvidenceSnapshotPageAsync(string projectRoot, string pageId, string sourceUrl, string archetype)
    {
        await MutateJsonAsync(projectRoot, "analysis/evidence-snapshot.json", json =>
        {
            var pages = json["pages"]!.AsArray();
            var clone = pages[0]!.DeepClone();
            ReplaceStrings(clone, "/home/", $"/{pageId}/");
            ReplaceStrings(clone, "home-", $"{pageId}-");
            ReplaceStrings(clone, "home", pageId);
            ReplaceStrings(clone, "ev-", $"ev-{pageId}-");
            clone["pageId"] = pageId;
            clone["url"] = sourceUrl;
            clone["label"] = archetype;
            pages.Add(clone);
        });
    }

    private static async Task AddMappingAsync(string projectRoot, string pageId, string sectionId, string componentId, string slotId, string targetPath, string generatedZone)
    {
        await MutateJsonAsync(projectRoot, "analysis/mapping/presentation-mappings.draft.json", json =>
        {
            json["mappings"]!.AsArray().Add(new JsonObject
            {
                ["sourceCandidateId"] = $"{pageId}-{slotId}",
                ["presentationComponentId"] = componentId,
                ["starterSlotId"] = slotId,
                ["variant"] = "default",
                ["slotAssignments"] = new JsonArray(),
                ["responsiveProperties"] = new JsonArray(),
                ["tokenBindings"] = new JsonArray(),
                ["interactionBindings"] = new JsonArray(),
                ["dataRequirements"] = new JsonArray(),
                ["behaviorOwnership"] = "presentation",
                ["confidence"] = 0.95,
                ["evidenceIds"] = new JsonArray($"ev-{pageId}-desktop-1440-{SectionIndex(sectionId):000}"),
                ["mappingReason"] = "positive end-to-end proof mapping",
                ["alternativeMappings"] = new JsonArray(),
                ["humanReviewRequired"] = false,
                ["sourcePageId"] = pageId,
                ["sourceSectionId"] = sectionId,
                ["ecommerceRegionId"] = sectionId,
                ["pageArchetype"] = pageId,
                ["targetGeneratedPath"] = targetPath,
                ["generatedZone"] = generatedZone,
                ["routeOwnership"] = "presentation",
                ["reasonCodes"] = new JsonArray("positive-proof"),
                ["reviewState"] = "Approved"
            });
        });
    }

    private static async Task WriteModifiedReviewDecisionsAsync(string projectRoot)
    {
        var queue = await ReadAsync<ReviewQueue>(projectRoot, "review/review-queue.json");
        var items = queue.Items.ToList();
        var section = ReadNode(projectRoot, "analysis/pages/home/sections.draft.json")["sections"]!.AsArray().OfType<JsonObject>().First(item => item["sectionId"]!.GetValue<string>() == "section-02");
        AddIfMissing(items, "section:home:section-02", "sections", section.DeepClone(), ["ev-desktop-1440-002"], "analysis/pages/*/sections.draft.json", "phase3d-positive-section-hash");
        queue = queue with { Items = items.OrderBy(item => item.ItemId, StringComparer.Ordinal).ToArray() };
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "review", "review-queue.json"), JsonSerializer.Serialize(queue, VisualJson.Options) + Environment.NewLine);
        var decisions = queue.Items.Select(item =>
        {
            var modified = item.ItemId == "section:home:section-02"
                ? new JsonObject { ["sectionType"] = "featured hero" }
                : null;
            return new ReviewDecision(
                item.ItemId,
                modified is null ? "Approved" : "Modified",
                modified,
                modified is null ? "Approved by positive proof." : "Modified by positive end-to-end proof.",
                DateTimeOffset.UtcNow,
                "reviewer@example.test",
                item.SourceArtifactId,
                item.SourceArtifactHash,
                "decision-" + item.ItemId);
        }).ToArray();
        var document = new ReviewDecisions("1.0", "review-decisions", "review-decisions-" + queue.ProjectId, DateTimeOffset.UtcNow, queue.ProjectId, decisions);
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "review", "review-decisions.json"), JsonSerializer.Serialize(document, VisualJson.Options) + Environment.NewLine);

        static void AddIfMissing(List<ReviewQueueItem> items, string itemId, string itemType, JsonNode proposal, IReadOnlyList<string> evidenceIds, string sourceArtifactId, string sourceArtifactHash)
        {
            if (!items.Any(item => item.ItemId == itemId))
            {
                items.Add(new ReviewQueueItem(itemId, itemType, 0.50m, proposal, evidenceIds, true, sourceArtifactId, sourceArtifactHash));
            }
        }
    }

    private static void AssertExactSlots(string projectRoot, ReviewedPageCompositionsDocument compositions, string pageId, IReadOnlyList<string> slots)
    {
        var mappings = ReadNode(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json")["mappings"]!.AsArray()
            .OfType<JsonObject>()
            .Where(mapping => mapping["sourcePageId"]?.GetValue<string>() == pageId)
            .Select(mapping => mapping["starterSlotId"]?.GetValue<string>())
            .Where(slot => !string.IsNullOrWhiteSpace(slot))
            .ToHashSet(StringComparer.Ordinal);
        var composition = compositions.Compositions.Single(candidate => candidate.PageId == pageId);
        if (composition.SectionTree.SelectMany(Flatten).Any(node => string.Equals(node.TargetFilePath, "Pages/Ssr/Home/HomePage.razor", StringComparison.Ordinal)))
        {
            mappings.Add("home.sections");
        }

        foreach (var slot in slots)
        {
            Assert.Contains(slot, mappings);
        }
    }

    private static IEnumerable<PageCompositionNode> Flatten(IEnumerable<PageCompositionNode> nodes)
    {
        foreach (var node in nodes.SelectMany(Flatten))
        {
            yield return node;
        }
    }

    private static IEnumerable<PageCompositionNode> Flatten(PageCompositionNode node)
    {
        yield return node;
        foreach (var child in node.Children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static IReadOnlyList<string> BlueprintReferences(VisualBlueprintV1 blueprint)
    {
        var references = new List<string>(blueprint.SourceProvenance);
        references.AddRange(blueprint.PageArchetypes);
        references.Add(blueprint.Tokens);
        references.AddRange(blueprint.Sections);
        references.AddRange(blueprint.ResponsiveBehavior);
        references.AddRange(blueprint.InteractionModels);
        references.Add(blueprint.ComponentDefinitions);
        references.Add(blueprint.ComponentInstances);
        references.AddRange(blueprint.EcommerceRegions);
        references.Add(blueprint.PresentationMappings);
        references.Add(blueprint.UnsupportedPatterns);
        references.Add(blueprint.OriginalityRestrictions);
        references.Add(blueprint.Confidence);
        references.Add(blueprint.ReviewState);
        return references;
    }

    private static IReadOnlyList<string> StableEvidenceHashes(string projectRoot)
    {
        var evidence = ReadAsync<AgentHandoffEvidenceManifest>(projectRoot, "analysis/agent-handoff/evidence-manifest.json").GetAwaiter().GetResult();
        return evidence.Pages
            .SelectMany(page => page.Screenshots.Select(screenshot => screenshot.HandoffPath + "=" + screenshot.Sha256)
                .Concat(page.Sections.Select(section => section.HandoffPath + "=" + section.Sha256)))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> StableCompositionIds(string projectRoot)
    {
        var compositions = ReadAsync<ReviewedPageCompositionsDocument>(projectRoot, "analysis/resolved/page-compositions.reviewed.json").GetAwaiter().GetResult();
        return compositions.Compositions
            .OrderBy(composition => composition.PageId, StringComparer.Ordinal)
            .SelectMany(composition => composition.SectionTree.SelectMany(Flatten).Select(node => composition.PageId + ":" + node.NodeId + ":" + node.StableFingerprint))
            .ToArray();
    }

    private static async Task<string> RunInspectAsync(string projectRoot)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = await CliHost.RunAsync(["inspect", "--project", projectRoot], stdout, stderr, CancellationToken.None);
        Assert.Equal(0, exitCode);
        Assert.Equal("", stderr.ToString());
        return stdout.ToString();
    }

    private static async Task<T> ReadAsync<T>(string projectRoot, string relativePath)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonSerializer.Deserialize<T>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Artifact did not deserialize: " + relativePath);
    }

    private static JsonObject ReadNode(string projectRoot, string relativePath) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))))?.AsObject()
        ?? throw new InvalidOperationException("Artifact did not parse: " + relativePath);

    private static async Task MutateJsonAsync(string projectRoot, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))?.AsObject()
            ?? throw new InvalidOperationException("Artifact did not parse: " + relativePath);
        mutate(json);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
    }

    private static int SectionIndex(string sectionId) =>
        int.TryParse(sectionId.Split('-').LastOrDefault(), out var index) ? index : 1;

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }

    private static void ReplaceStrings(JsonNode node, string oldValue, string newValue)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToArray())
            {
                if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    obj[property.Key] = text.Replace(oldValue, newValue, StringComparison.Ordinal);
                }
                else if (property.Value is not null)
                {
                    ReplaceStrings(property.Value, oldValue, newValue);
                }
            }
        }
        else if (node is JsonArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                if (array[index] is JsonValue value && value.TryGetValue<string>(out var text))
                {
                    array[index] = text.Replace(oldValue, newValue, StringComparison.Ordinal);
                }
                else if (array[index] is not null)
                {
                    ReplaceStrings(array[index]!, oldValue, newValue);
                }
            }
        }
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
