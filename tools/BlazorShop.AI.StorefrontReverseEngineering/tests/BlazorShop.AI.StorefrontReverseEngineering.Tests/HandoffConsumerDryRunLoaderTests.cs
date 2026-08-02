using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "PortableProof")]
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
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/visual-blueprint.json", json =>
        {
            json["consumerReferences"]!.AsObject()["pageCompositions"] = "../outside.json";
        });
        await RehashPortableManifestAsync(fixture.PortableRoot, "analysis/agent-handoff/visual-blueprint.json");

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

    private static async Task RehashPortableManifestAsync(string projectRoot, string changedRelativePath)
    {
        var manifestPath = Path.Combine(projectRoot, "analysis", "agent-handoff", "manifest.json");
        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(manifestPath))!.AsObject();
        var artifactEntries = manifest["artifactEntries"]!.AsArray();
        var changedPath = Path.Combine(projectRoot, changedRelativePath.Replace('/', Path.DirectorySeparatorChar));

        foreach (var entry in artifactEntries.Select(node => node!.AsObject()))
        {
            if (!string.Equals(entry["path"]!.GetValue<string>(), changedRelativePath, StringComparison.Ordinal))
            {
                continue;
            }

            var fileInfo = new FileInfo(changedPath);
            entry["sha256"] = PortableHandoffPackageHasher.ComputeFileHash(changedPath);
            entry["sizeBytes"] = fileInfo.Length;
            break;
        }

        var portableEntries = artifactEntries
            .Select(node => node!.AsObject())
            .Select(entry => new PortableHandoffArtifactEntry(
                entry["path"]!.GetValue<string>(),
                entry["artifactKind"]!.GetValue<string>(),
                entry["schemaKind"]?.GetValue<string>() ?? "",
                entry["schemaVersion"]?.GetValue<string>() ?? "1.0",
                entry["sha256"]!.GetValue<string>(),
                entry["sizeBytes"]!.GetValue<long>(),
                entry["required"]!.GetValue<bool>(),
                entry["includeInPackageHash"]?.GetValue<bool>() ?? true))
            .ToArray();
        var schemaRequirements = manifest["schemaRequirements"]!.AsArray()
            .Select(node => node!.AsObject())
            .Select(schema => new PortableHandoffSchemaRequirement(
                schema["schemaKind"]!.GetValue<string>(),
                schema["artifactKind"]!.GetValue<string>(),
                schema["schemaVersion"]!.GetValue<string>(),
                schema["schemaFileName"]!.GetValue<string>(),
                schema["sha256"]!.GetValue<string>(),
                schema["required"]!.GetValue<bool>()))
            .ToArray();

        manifest["packageHash"] = PortableHandoffPackageHasher.ComputePackageHash(portableEntries, schemaRequirements);
        await File.WriteAllTextAsync(manifestPath, manifest.ToJsonString(VisualJson.Options));
    }
}
