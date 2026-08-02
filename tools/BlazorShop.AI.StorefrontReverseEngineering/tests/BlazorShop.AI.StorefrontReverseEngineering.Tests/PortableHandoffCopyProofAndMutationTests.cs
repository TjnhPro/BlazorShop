using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class PortableHandoffCopyProofTests
{
    [Fact]
    public async Task PortableCopyProofLoadsCopiedPackageWithoutSourceProjectRoot()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable Copy Proof");
        var portableCopyRootA = CreateTempRoot("portable-copy-a");
        var portableCopyRootB = CreateTempRoot("portable-copy-b");
        var schemaCopyRootA = CreateTempRoot("schema-copy-a");
        var schemaCopyRootB = CreateTempRoot("schema-copy-b");

        CopyDirectory(fixture.PortableRoot, portableCopyRootA);
        CopyDirectory(fixture.PortableRoot, portableCopyRootB);
        CopyDirectory(fixture.SchemaRoot, schemaCopyRootA);
        CopyDirectory(fixture.SchemaRoot, schemaCopyRootB);
        fixture.DeleteSourceProject();

        var validator = new PortableHandoffValidator();
        var reportA = await validator.ValidateAsync(portableCopyRootA, schemaCopyRootA, CancellationToken.None);
        var reportB = await validator.ValidateAsync(portableCopyRootB, schemaCopyRootB, CancellationToken.None);
        var loader = new HandoffConsumerDryRunLoader();
        var before = Directory.EnumerateFiles(portableCopyRootA, "*", SearchOption.AllDirectories).Count();
        var package = await loader.LoadAsync(portableCopyRootA, schemaCopyRootA, CancellationToken.None);
        var after = Directory.EnumerateFiles(portableCopyRootA, "*", SearchOption.AllDirectories).Count();

        Assert.False(Directory.Exists(fixture.SourceProjectRoot));
        Assert.True(reportA.ReadinessPassed);
        Assert.True(reportB.ReadinessPassed);
        Assert.Equal(reportA.PackageHash, reportB.PackageHash);
        Assert.Equal(0, reportA.Findings.Count(finding => finding.Severity == "blocking"));
        Assert.Equal(0, reportB.Findings.Count(finding => finding.Severity == "blocking"));
        Assert.Equal(before, after);
        Assert.False(string.IsNullOrWhiteSpace(package.ProjectId));
        Assert.Equal(package.Pages.Select(page => page.PageId).Order(StringComparer.Ordinal), package.Pages.Select(page => page.PageId).ToArray());
        Assert.NotNull(package.DesignTokens);
        Assert.NotNull(package.VisualStyle);
        Assert.NotNull(package.ResponsiveBehavior);
        Assert.NotNull(package.InteractionModels);
        Assert.NotEmpty(package.AllowedTargetFiles);
        Assert.NotEmpty(package.ProtectedFiles);
        Assert.NotEmpty(package.EvidenceFilePaths);
        Assert.Contains(package.EvidenceFilePaths, path => path.StartsWith("analysis/agent-handoff/screenshots/", StringComparison.Ordinal));
        Assert.Contains(package.EvidenceFilePaths, path => path.StartsWith("analysis/agent-handoff/section-screenshots/", StringComparison.Ordinal));
        Assert.True(package.ReadinessReport.Passed);
    }

    private static string CreateTempRoot(string prefix)
    {
        var root = Path.Combine(Path.GetTempPath(), "sre-portable-copy-proof-" + prefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void CopyDirectory(string sourceRoot, string destinationRoot)
    {
        Directory.CreateDirectory(destinationRoot);
        foreach (var directory in Directory.EnumerateDirectories(sourceRoot, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file)), overwrite: true);
        }
    }
}

public sealed class Phase3ENegativeReferenceMutationTests
{
    [Theory]
    [InlineData("analysis/resolved/foo.json", "handoff-diagnostic-reference-used-as-consumer")]
    [InlineData("analysis/resolved/page-compositions.reviewed.json", "handoff-diagnostic-reference-used-as-consumer")]
    [InlineData("../foo.json", "handoff-consumer-reference-escape")]
    [InlineData("C:/foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("C:\\foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("//server/share/foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("/tmp/foo.json", "handoff-consumer-reference-absolute")]
    [InlineData("analysis/agent-handoff/visual-blueprint.draft.json", "handoff-consumer-reference-draft")]
    public async Task ReferenceMutations_FailWithExactCode(string reference, string expectedCode)
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Negative Reference " + expectedCode);
        fixture.DeleteSourceProject();
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/visual-blueprint.json", json =>
        {
            json["consumerReferences"]!.AsObject()["pageCompositions"] = reference;
        });

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == expectedCode);
    }
}

public sealed class Phase3ENegativeArtifactMutationTests
{
    [Theory]
    [InlineData("analysis/agent-handoff/presentation-catalog.json")]
    [InlineData("analysis/agent-handoff/presentation-mappings.json")]
    [InlineData("analysis/agent-handoff/responsive-behavior.json")]
    [InlineData("analysis/agent-handoff/interaction-models.json")]
    public async Task RemovingRequiredPortableArtifact_Fails(string relativePath)
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Negative Artifact " + relativePath);
        fixture.DeleteSourceProject();
        File.Delete(Path.Combine(fixture.PortableRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-artifact-missing");
    }

    [Fact]
    public async Task RemovingOneSectionCrop_Fails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Negative Artifact Section Crop");
        fixture.DeleteSourceProject();
        var evidence = await PortableHandoffMutationTestHelpers.ReadJsonAsync<AgentHandoffEvidenceManifest>(fixture.PortableRoot, "analysis/agent-handoff/evidence-manifest.json");
        var section = evidence.Pages.SelectMany(page => page.Sections).First();
        File.Delete(Path.Combine(fixture.PortableRoot, section.HandoffPath.Replace('/', Path.DirectorySeparatorChar)));

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-artifact-missing");
    }

    [Fact]
    public async Task CorruptingPortableArtifactAfterCopy_Fails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Negative Artifact Corrupt");
        fixture.DeleteSourceProject();
        await File.AppendAllTextAsync(Path.Combine(fixture.PortableRoot, "analysis", "agent-handoff", "allowed-files.json"), " ");

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-artifact-hash-mismatch");
    }
}

public sealed class Phase3ENegativeSchemaMutationTests
{
    [Fact]
    public async Task RemovingRequiredSchemaFile_Fails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Negative Schema Missing");
        fixture.DeleteSourceProject();
        var schemaPath = Directory.EnumerateFiles(fixture.SchemaRoot, "*.schema.json", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal).First();
        File.Delete(schemaPath);

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-schema-missing");
    }
}

public sealed class Phase3ENegativeHashMutationTests
{
    [Fact]
    public async Task ReorderingCanonicalManifestLists_Fails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Negative Hash Ordering");
        fixture.DeleteSourceProject();
        await PortableHandoffMutationTestHelpers.MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/manifest.json", json =>
        {
            json["artifactEntries"] = new JsonArray(json["artifactEntries"]!.AsArray().Reverse().Select(node => node!.DeepClone()).ToArray());
            json["artifactList"] = new JsonArray(json["artifactList"]!.AsArray().Reverse().Select(node => node!.DeepClone()).ToArray());
            json["schemaRequirements"] = new JsonArray(json["schemaRequirements"]!.AsArray().Reverse().Select(node => node!.DeepClone()).ToArray());
        });

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-manifest-order-mismatch");
    }

}

internal static class PortableHandoffMutationTestHelpers
{
    internal static async Task<T> ReadJsonAsync<T>(string projectRoot, string relativePath)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = await File.ReadAllTextAsync(path);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, VisualJson.Options)
            ?? throw new InvalidOperationException("Artifact did not deserialize: " + relativePath);
    }

    internal static async Task MutateJsonAsync(string projectRoot, string relativePath, Action<JsonObject> mutate)
    {
        var path = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))?.AsObject()
            ?? throw new InvalidOperationException("Artifact did not parse: " + relativePath);
        mutate(json);
        await File.WriteAllTextAsync(path, json.ToJsonString(VisualJson.Options));
    }
}
