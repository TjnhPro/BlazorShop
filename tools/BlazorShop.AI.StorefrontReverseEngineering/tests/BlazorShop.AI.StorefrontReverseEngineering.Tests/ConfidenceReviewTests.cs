using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Review;
using BlazorShop.AI.StorefrontReverseEngineering.Application;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class ConfidenceReviewTests
{
    [Fact]
    public async Task Confidence_ScoreIsDeterministic()
    {
        var projectRoot = await CreateReadyProjectAsync("Confidence Deterministic");

        var first = await new ConfidenceScorer(GetRepoRoot()).ScoreAsync(projectRoot, CancellationToken.None);
        var second = await new ConfidenceScorer(GetRepoRoot()).ScoreAsync(projectRoot, CancellationToken.None);

        Assert.Equal(first.Items.Select(item => (item.ItemId, item.Confidence)), second.Items.Select(item => (item.ItemId, item.Confidence)));
    }

    [Fact]
    public async Task Confidence_LowConfidenceCriticalMappingEntersReviewQueue()
    {
        var projectRoot = await CreateReadyProjectAsync("Confidence Queue");

        var queue = await ReadQueueAsync(projectRoot);

        Assert.Contains(queue.Items, item => item.Blocking || item.OriginalConfidence < 0.60m);
        Assert.True(File.Exists(Path.Combine(projectRoot, "review", "review-pack.md")));
    }

    [Fact]
    public async Task ReviewDecision_ApproveAndModifyPreserveOriginalProposal()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [
            Decision(queue, "token:text-body", "Approved", null, "looks good"),
            Decision(queue, "mapping:family-product-card", "Modified", new { variant = "featured" }, "adjust variant")
        ]);

        var reviewed = await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);

        Assert.Contains(reviewed.Items, item => item.ItemId == "token:text-body" && item.Status == "Approved" && item.OriginalProposal is not null);
        Assert.Contains(reviewed.Items, item => item.ItemId == "mapping:family-product-card" && item.Status == "Modified" && item.ModifiedValue is not null);
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "resolved", "unsupported-pattern-decisions.json")));
    }

    [Fact]
    public async Task ReviewDecision_MissingDecisionIsDeferredAndBlocksReadiness()
    {
        var projectRoot = await CreateReviewProjectAsync();
        await WriteDecisionsAsync(projectRoot, []);

        var reviewed = await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);

        Assert.True(reviewed.BlocksReadiness);
        Assert.Contains(reviewed.Items, item => item.ItemId == "token:text-body" && item.Status == "Deferred" && item.ReviewerNote == "No decision recorded.");
        Assert.All(reviewed.Items, item => Assert.Equal("Deferred", item.Status));
    }

    [Fact]
    public async Task ReviewDecision_SafeDecisionMetadataIsPreserved()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        var decision = Decision(queue, "token:text-body", "Approved", null, "safe visual token");
        await WriteDecisionsAsync(projectRoot, [decision]);

        var reviewed = await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);

        var item = Assert.Single(reviewed.Items, candidate => candidate.ItemId == "token:text-body");
        Assert.Equal(decision.ReviewerNote, item.ReviewerNote);
        Assert.False(string.IsNullOrWhiteSpace(decision.SourceArtifactId));
        Assert.False(string.IsNullOrWhiteSpace(decision.SourceArtifactHash));
        Assert.False(string.IsNullOrWhiteSpace(decision.Reviewer));
        Assert.False(string.IsNullOrWhiteSpace(decision.DecisionId));
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "resolved", "review-resolution-manifest.json")));
    }

    [Fact]
    public async Task SafeReviewDecisionMaterializer_ApprovesSafeVisualItemsAndBlocksUnsupported()
    {
        var projectRoot = await CreateReviewProjectAsync();

        var summary = await new SafeReviewDecisionMaterializer(GetRepoRoot()).MaterializeAsync(projectRoot, CancellationToken.None);
        var decisions = await ReadDecisionsAsync(projectRoot);
        var tokenDecision = Assert.Single(decisions.Decisions, decision => decision.ItemId == "token:text-body");

        Assert.True(summary.Approved > 0);
        Assert.Equal(1, summary.Blocked);
        Assert.Contains(summary.Items, item => item.ItemId == "unsupported:family-unsafe" && item.Status == "Blocked");
        Assert.Equal("Approved", tokenDecision.Status);
        Assert.Equal("storefront-reverse-engineering-safe-review", tokenDecision.Reviewer);
        Assert.Equal("hash-token", tokenDecision.SourceArtifactHash);
        Assert.StartsWith("safe-", tokenDecision.DecisionId, StringComparison.Ordinal);
        Assert.DoesNotContain(decisions.Decisions, decision => decision.ItemId == "unsupported:family-unsafe");
        Assert.True(File.Exists(Path.Combine(projectRoot, "review", "review-decision-summary.json")));
    }

    [Fact]
    public async Task SafeReviewDecisionMaterializer_RefusesStaleExistingDecisionWithoutDuplicate()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [Decision(queue, "token:text-body", "Approved", null, "old") with { SourceArtifactHash = "stale" }]);

        var summary = await new SafeReviewDecisionMaterializer(GetRepoRoot()).MaterializeAsync(projectRoot, CancellationToken.None);
        var decisions = await ReadDecisionsAsync(projectRoot);

        Assert.Equal(1, summary.Stale);
        Assert.Contains(summary.Items, item => item.ItemId == "token:text-body" && item.Status == "Blocked");
        Assert.Single(decisions.Decisions, decision => decision.ItemId == "token:text-body");
    }

    [Fact]
    public async Task ReviewDecision_ModifiedValuesAreAppliedToReviewedArtifacts()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [
            Decision(queue, "token:text-body", "Modified", new { role = "text-heading" }, "semantic adjustment"),
            Decision(queue, "mapping:family-product-card", "Modified", new { targetGeneratedPath = "Components/Catalog/FeaturedCard.razor", variant = "featured" }, "mapping adjustment"),
            Decision(queue, "region:home:region-01", "Modified", new { role = "featured product card" }, "region adjustment"),
            Decision(queue, "page:home", "Modified", new { primaryArchetype = "content-page" }, "page adjustment"),
            Decision(queue, "section:home:section-01", "Modified", new { sectionType = "hero product grid" }, "section adjustment"),
            Decision(queue, "component:family-product-card", "Modified", new { family = "featured product card" }, "component adjustment"),
            Decision(queue, "unsupported:family-unsafe", "Approved", null, "unsupported disposition recorded"),
            Decision(queue, "originality:asset-01", "Modified", new { usage = "reference-only" }, "originality adjustment")
        ]);

        await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);

        Assert.Equal("text-heading", ReadNode(projectRoot, "analysis/resolved/semantic-tokens.reviewed.json")["tokens"]![0]!["role"]!.GetValue<string>());
        Assert.Equal("featured", ReadNode(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json")["mappings"]![0]!["variant"]!.GetValue<string>());
        Assert.Equal("featured product card", ReadNode(projectRoot, "analysis/resolved/ecommerce-regions.reviewed.json")["pages"]![0]!["regions"]![0]!["role"]!.GetValue<string>());
        Assert.Equal("content-page", ReadNode(projectRoot, "analysis/resolved/page-archetypes.reviewed.json")["pages"]![0]!["primaryArchetype"]!.GetValue<string>());
        Assert.Equal("hero product grid", ReadNode(projectRoot, "analysis/resolved/page-sections.reviewed.json")["pages"]![0]!["sections"]![0]!["sectionType"]!.GetValue<string>());
        Assert.Equal("featured product card", ReadNode(projectRoot, "analysis/resolved/component-candidates.reviewed.json")["candidates"]![0]!["family"]!.GetValue<string>());
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "resolved", "originality-restrictions.reviewed.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "analysis", "resolved", "review-resolution-manifest.json")));
    }

    [Fact]
    public async Task ReviewDecision_RejectedCriticalMappingIsExcludedAndBlocksReadiness()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [
            Decision(queue, "token:text-body", "Approved", null, "ok"),
            Decision(queue, "mapping:family-product-card", "Rejected", null, "unsafe mapping")
        ]);

        var reviewed = await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);
        var mappings = ReadNode(projectRoot, "analysis/resolved/presentation-mappings.reviewed.json")["mappings"]!.AsArray();

        Assert.True(reviewed.BlocksReadiness);
        Assert.DoesNotContain(mappings, mapping => mapping?["sourceCandidateId"]?.GetValue<string>() == "family-product-card");
    }

    [Fact]
    public async Task ReviewDecision_RejectAndDeferBlockReadiness()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [
            Decision(queue, "token:text-body", "Rejected", null, "bad match"),
            Decision(queue, "mapping:family-product-card", "Deferred", null, "needs design")
        ]);

        var reviewed = await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);

        Assert.True(reviewed.BlocksReadiness);
        Assert.Contains(reviewed.Items, item => item.Status == "Rejected");
        Assert.Contains(reviewed.Items, item => item.Status == "Deferred");
    }

    [Fact]
    public async Task ReviewDecision_UnknownTargetIsRejected()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [Decision(queue, "token:text-body", "Approved", null, "ok") with { ItemId = "missing-item" }]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None));
    }

    [Fact]
    public async Task ReviewDecision_UnknownStatusIsRejected()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [Decision(queue, "token:text-body", "Done", null, "ok")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None));
    }

    [Fact]
    public async Task ReviewDecision_InvalidModifiedValueShapeFails()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [Decision(queue, "token:text-body", "Modified", "bad-shape", "invalid")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None));
    }

    [Fact]
    public async Task ReviewDecision_UnknownReviewItemFamilyFails()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        var unknown = new ReviewQueue(
            queue.SchemaVersion,
            queue.ArtifactKind,
            queue.ArtifactId,
            queue.CreatedUtc,
            queue.ProjectId,
            queue.Items.Concat([new ReviewQueueItem("unknown:item", "unknown", 0.1m, new { value = "x" }, [], true, "review/unknown.json", "hash-unknown")]).ToArray());
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "review", "review-queue.json"), JsonSerializer.Serialize(unknown, VisualJson.Options) + Environment.NewLine);
        await WriteDecisionsAsync(projectRoot, [new ReviewDecision("unknown:item", "Approved", null, "ok", DateTimeOffset.UtcNow, "reviewer@example.test", "review/unknown.json", "hash-unknown", "decision-unknown")]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None));
    }

    [Fact]
    public async Task ReviewDecision_DuplicateDecisionWithoutSupersedeIsRejected()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [
            Decision(queue, "token:text-body", "Approved", null, "ok"),
            Decision(queue, "token:text-body", "Approved", null, "still ok") with { DecisionId = "decision-duplicate" }
        ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None));
    }

    [Fact]
    public async Task ReviewDecision_StaleSourceHashIsRejected()
    {
        var projectRoot = await CreateReviewProjectAsync();
        var queue = await ReadQueueAsync(projectRoot);
        await WriteDecisionsAsync(projectRoot, [Decision(queue, "token:text-body", "Approved", null, "ok") with { SourceArtifactHash = "stale" }]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None));
    }

    [Fact]
    public async Task ReviewDecision_ResolutionManifestHashChangesWithDecisionBundle()
    {
        var firstRoot = await CreateReviewProjectAsync();
        var firstQueue = await ReadQueueAsync(firstRoot);
        await WriteDecisionsAsync(firstRoot, [Decision(firstQueue, "token:text-body", "Approved", null, "ok")]);
        await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(firstRoot, CancellationToken.None);

        var secondRoot = await CreateReviewProjectAsync();
        var secondQueue = await ReadQueueAsync(secondRoot);
        await WriteDecisionsAsync(secondRoot, [Decision(secondQueue, "token:text-body", "Deferred", null, "needs review")]);
        await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(secondRoot, CancellationToken.None);

        var firstHash = ReadNode(firstRoot, "analysis/resolved/review-resolution-manifest.json")["decisionBundleHash"]!.GetValue<string>();
        var secondHash = ReadNode(secondRoot, "analysis/resolved/review-resolution-manifest.json")["decisionBundleHash"]!.GetValue<string>();
        Assert.NotEqual(firstHash, secondHash);
    }

    private static async Task<string> CreateReadyProjectAsync(string name)
    {
        var repoRoot = GetRepoRoot();
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "confidence-" + Guid.NewGuid().ToString("N"));
        var fixtureUrl = new Uri(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "tests", "BlazorShop.AI.StorefrontReverseEngineering.Tests", "Fixtures", "static-storefront.html")).AbsoluteUri;
        var summary = await new VisualProjectWorkflowService(repoRoot)
            .RunAsync(fixtureUrl, name, outputRoot, force: true, resume: false, noAi: true, CancellationToken.None, runId: "confidence-fixture");

        Assert.True(summary.ReadinessPassed);
        return summary.ArtifactRoot;
    }

    private static async Task<string> CreateReviewProjectAsync()
    {
        var root = Path.Combine(GetRepoRoot(), "obj", "storefront-reverse-engineering", "projects", "review-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "review"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "tokens"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "mapping"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "components"));
        Directory.CreateDirectory(Path.Combine(root, "analysis", "pages", "home"));
        var queue = new ReviewQueue(
            "1.0",
            "review-queue",
            "review-queue-test",
            DateTimeOffset.UtcNow,
            "review",
            [
                new ReviewQueueItem("token:text-body", "semantic tokens", 0.50m, new { role = "text-body" }, ["ev-1"], Blocking: true, "analysis/tokens/semantic-tokens.draft.json", "hash-token"),
                new ReviewQueueItem("mapping:family-product-card", "Presentation mappings", 0.42m, new { sourceCandidateId = "family-product-card" }, ["ev-2"], Blocking: true, "analysis/mapping/presentation-mappings.draft.json", "hash-mapping"),
                new ReviewQueueItem("region:home:region-01", "ecommerce roles", 0.50m, new { role = "product card" }, ["ev-3"], Blocking: true, "analysis/pages/*/ecommerce-regions.json", "hash-region"),
                new ReviewQueueItem("page:home", "page archetype", 0.50m, new { primaryArchetype = "home" }, ["ev-4"], Blocking: true, "analysis/pages/*/page-archetype.json", "hash-page"),
                new ReviewQueueItem("section:home:section-01", "sections", 0.50m, new { sectionType = "product card" }, ["ev-5"], Blocking: true, "analysis/pages/*/sections.draft.json", "hash-section"),
                new ReviewQueueItem("component:family-product-card", "component families", 0.50m, new { family = "product card" }, ["ev-6"], Blocking: true, "analysis/components/component-candidates.json", "hash-component"),
                new ReviewQueueItem("unsupported:family-unsafe", "unsupported patterns", 0.20m, new { sourceCandidateId = "family-unsafe" }, ["ev-7"], Blocking: true, "analysis/mapping/unsupported-patterns.json", "hash-unsupported"),
                new ReviewQueueItem("originality:asset-01", "originality restrictions", 0.50m, new { usage = "reference-only" }, ["ev-8"], Blocking: false, "analysis/originality-audit.json", "hash-originality")
            ]);
        await File.WriteAllTextAsync(Path.Combine(root, "review", "review-queue.json"), JsonSerializer.Serialize(queue, VisualJson.Options) + Environment.NewLine);
        await WriteDraftArtifactsAsync(root);
        await WriteDecisionsAsync(root, []);
        return root;
    }

    private static async Task WriteDraftArtifactsAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "tokens", "semantic-tokens.draft.json"), """
        {"schemaVersion":"1.0","artifactKind":"semantic-tokens","artifactId":"semantic-tokens-review","createdUtc":"2026-01-01T00:00:00Z","projectId":"review","sourceArtifact":"raw","tokens":[{"role":"text-body","category":"typography","normalizedValues":["16px"],"sourceValues":["16px"],"evidenceIds":["ev-1"],"confidence":0.5,"reasonCodes":["test"],"humanReviewRequired":true}],"pageLocalOverrides":[],"componentLocalOverrides":[],"humanReviewRequired":true,"reviewReasons":[]}
        """ + Environment.NewLine);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "mapping", "presentation-mappings.draft.json"), """
        {"schemaVersion":"1.0","artifactKind":"presentation-mappings","artifactId":"presentation-mappings-review","createdUtc":"2026-01-01T00:00:00Z","projectId":"review","mappings":[{"sourceCandidateId":"family-product-card","presentationComponentId":"catalog.product-card","starterSlotId":"catalog.product-card","variant":"default","slotAssignments":[],"responsiveProperties":[],"tokenBindings":[],"interactionBindings":[],"dataRequirements":[],"behaviorOwnership":"presentation","confidence":0.5,"evidenceIds":["ev-2"],"mappingReason":"test","alternativeMappings":[],"humanReviewRequired":true,"sourcePageId":"home","sourceSectionId":"section-01","ecommerceRegionId":"region-01","pageArchetype":"home","targetGeneratedPath":"Components/Catalog/ProductSummaryCard.razor","generatedZone":"catalog-components","routeOwnership":"presentation","reasonCodes":[],"reviewState":"NeedsReview"}]}
        """ + Environment.NewLine);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "components", "component-candidates.json"), """
        {"schemaVersion":"1.0","artifactKind":"component-candidates","artifactId":"component-candidates-review","createdUtc":"2026-01-01T00:00:00Z","projectId":"review","candidates":[{"familyId":"family-product-card","family":"product card","variant":"default","confidence":0.5,"instanceIds":["instance-1"],"slots":[],"tokenReferences":[],"localOverrideIds":[],"responsiveBehaviorRefs":[],"interactionBehaviorRefs":[],"alternatives":[],"humanReviewRequired":true,"evidenceIds":["ev-6"]}],"issues":[]}
        """ + Environment.NewLine);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "pages", "home", "page-archetype.json"), """
        {"schemaVersion":"1.0","artifactKind":"page-archetype","artifactId":"page-archetype-home","createdUtc":"2026-01-01T00:00:00Z","projectId":"review","pageId":"home","primaryArchetype":"home","confidence":0.5,"evidenceIds":["ev-4"],"reasonCodes":["test"],"alternatives":[]}
        """ + Environment.NewLine);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "pages", "home", "sections.draft.json"), """
        {"schemaVersion":"1.0","artifactKind":"sections","artifactId":"sections-home","createdUtc":"2026-01-01T00:00:00Z","projectId":"review","pageId":"home","sections":[{"sectionId":"section-01","sectionType":"product card","evidenceIds":["ev-5"],"confidence":0.5,"reasonCodes":["test"],"bounds":{"x":0,"y":0,"width":100,"height":100},"parentSectionId":null,"childSectionIds":[]}]}
        """ + Environment.NewLine);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "pages", "home", "ecommerce-regions.json"), """
        {"schemaVersion":"1.0","artifactKind":"ecommerce-regions","artifactId":"regions-home","createdUtc":"2026-01-01T00:00:00Z","projectId":"review","pageId":"home","regions":[{"regionId":"region-01","role":"product card","dataDomain":"catalog","behaviorOwnership":"presentation-only","requiresInteraction":false,"visualOnly":true,"unsupported":false,"sourceSectionIds":["section-01"],"sourceComponentFamilyIds":["family-product-card"],"evidenceIds":["ev-3"],"alternatives":[]}]}
        """ + Environment.NewLine);
        await File.WriteAllTextAsync(Path.Combine(root, "analysis", "mapping", "unsupported-patterns.json"), """
        {"schemaVersion":"1.0","artifactKind":"unsupported-patterns","artifactId":"unsupported-review","createdUtc":"2026-01-01T00:00:00Z","projectId":"review","patterns":[{"sourceCandidateId":"family-unsafe","group":"unsafe-browser-action","reason":"direct api","evidenceIds":["ev-7"],"humanReviewRequired":true}]}
        """ + Environment.NewLine);
    }

    private static ReviewDecision Decision(ReviewQueue queue, string itemId, string status, object? modifiedValue, string note)
    {
        var item = queue.Items.First(candidate => candidate.ItemId == itemId);
        return new ReviewDecision(
            itemId,
            status,
            modifiedValue,
            note,
            DateTimeOffset.UtcNow,
            "reviewer@example.test",
            item.SourceArtifactId,
            item.SourceArtifactHash,
            "decision-" + itemId);
    }

    private static async Task WriteDecisionsAsync(string projectRoot, IReadOnlyList<ReviewDecision> decisions)
    {
        var document = new ReviewDecisions("1.0", "review-decisions", "review-decisions-test", DateTimeOffset.UtcNow, "review", decisions);
        await File.WriteAllTextAsync(Path.Combine(projectRoot, "review", "review-decisions.json"), JsonSerializer.Serialize(document, VisualJson.Options) + Environment.NewLine);
    }

    private static async Task<ReviewQueue> ReadQueueAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "review", "review-queue.json"));
        return JsonSerializer.Deserialize<ReviewQueue>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Review queue did not deserialize.");
    }

    private static async Task<ReviewDecisions> ReadDecisionsAsync(string projectRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(projectRoot, "review", "review-decisions.json"));
        return JsonSerializer.Deserialize<ReviewDecisions>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Review decisions did not deserialize.");
    }

    private static JsonNode ReadNode(string projectRoot, string relativePath) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))))
        ?? throw new InvalidOperationException("Artifact did not parse: " + relativePath);

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
