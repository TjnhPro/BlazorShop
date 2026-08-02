using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class HandoffConsumerDryRunLoaderTests
{
    [Fact]
    public async Task LoaderReadsCopiedPortablePackageInDeterministicOrder()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Dry Run Loader Success");
        var before = Directory.EnumerateFiles(fixture.PortableRoot, "*", SearchOption.AllDirectories).Count();

        var package = await new HandoffConsumerDryRunLoader().LoadAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        var after = Directory.EnumerateFiles(fixture.PortableRoot, "*", SearchOption.AllDirectories).Count();
        Assert.Equal(before, after);
        Assert.False(string.IsNullOrWhiteSpace(package.ProjectId));
        Assert.Equal(package.Pages.Select(page => page.PageId).Order(StringComparer.Ordinal), package.Pages.Select(page => page.PageId).ToArray());
        Assert.All(package.Pages, page =>
        {
            Assert.NotEmpty(page.RequiredSlots);
            Assert.NotEmpty(page.AllowedTargetFiles);
            Assert.NotEmpty(page.ProtectedFiles);
            Assert.NotEmpty(page.EvidenceFilePaths);
        });
        Assert.NotNull(package.DesignTokens);
        Assert.NotNull(package.VisualStyle);
        Assert.NotNull(package.ResponsiveBehavior);
        Assert.NotNull(package.InteractionModels);
        Assert.NotEmpty(package.AllowedTargetFiles);
        Assert.NotEmpty(package.ProtectedFiles);
        Assert.NotEmpty(package.EvidenceFilePaths);
        Assert.Equal(package.UnresolvedRegions.Order(StringComparer.Ordinal), package.UnresolvedRegions.ToArray());
        Assert.True(package.ReadinessReport.Passed);
    }

    [Fact]
    public async Task LoaderDoesNotRequireSourceProjectRoot()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Dry Run Loader Detached Source");
        fixture.DeleteSourceProject();

        var package = await new HandoffConsumerDryRunLoader().LoadAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.False(Directory.Exists(fixture.SourceProjectRoot));
        Assert.NotEmpty(package.ProtectedFiles);
        Assert.All(package.Pages, page => Assert.NotEmpty(page.ProtectedFiles));
    }

    [Fact]
    public async Task LoaderRefusesReadinessFalse()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Dry Run Loader Readiness");
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/handoff-readiness.json", json => json["passed"] = false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new HandoffConsumerDryRunLoader().LoadAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None));

        Assert.Contains("portable-handoff-readiness-false", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoaderRefusesEscapingConsumerReference()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Dry Run Loader Escape");
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/manifest.json", json =>
        {
            json["artifactList"]!.AsArray().Add("../outside.json");
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new HandoffConsumerDryRunLoader().LoadAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None));

        Assert.Contains("handoff-consumer-reference-escape", exception.Message, StringComparison.Ordinal);
    }

    private static async Task MutateJsonAsync(string projectRoot, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))?.AsObject()
            ?? throw new InvalidOperationException("Artifact did not parse: " + relativePath);
        mutate(json);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
    }
}
