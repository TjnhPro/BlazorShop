using System.Text.Json;
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
        await WriteDecisionsAsync(projectRoot, [
            new ReviewDecision("item-approve", "Approved", null, "looks good", DateTimeOffset.UtcNow),
            new ReviewDecision("item-modify", "Modified", new { role = "changed" }, "adjust role", DateTimeOffset.UtcNow)
        ]);

        var reviewed = await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);

        Assert.Contains(reviewed.Items, item => item.ItemId == "item-approve" && item.Status == "Approved" && item.OriginalProposal is not null);
        Assert.Contains(reviewed.Items, item => item.ItemId == "item-modify" && item.Status == "Modified" && item.ModifiedValue is not null);
    }

    [Fact]
    public async Task ReviewDecision_RejectAndDeferBlockReadiness()
    {
        var projectRoot = await CreateReviewProjectAsync();
        await WriteDecisionsAsync(projectRoot, [
            new ReviewDecision("item-approve", "Rejected", null, "bad match", DateTimeOffset.UtcNow),
            new ReviewDecision("item-modify", "Deferred", null, "needs design", DateTimeOffset.UtcNow)
        ]);

        var reviewed = await new ReviewDecisionApplier(GetRepoRoot()).ApplyAsync(projectRoot, CancellationToken.None);

        Assert.True(reviewed.BlocksReadiness);
        Assert.Contains(reviewed.Items, item => item.Status == "Rejected");
        Assert.Contains(reviewed.Items, item => item.Status == "Deferred");
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
        var queue = new ReviewQueue(
            "1.0",
            "review-queue",
            "review-queue-test",
            DateTimeOffset.UtcNow,
            "review",
            [
                new ReviewQueueItem("item-approve", "Presentation mappings", 0.42m, new { role = "original" }, ["ev-1"], Blocking: true),
                new ReviewQueueItem("item-modify", "semantic tokens", 0.50m, new { token = "original" }, ["ev-2"], Blocking: true)
            ]);
        await File.WriteAllTextAsync(Path.Combine(root, "review", "review-queue.json"), JsonSerializer.Serialize(queue, VisualJson.Options) + Environment.NewLine);
        await WriteDecisionsAsync(root, []);
        return root;
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
