using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class PortableHandoffContractTests
{
    [Fact]
    public void PortableHandoffContract_RequiredSchemasComeFromRequiredJsonArtifacts()
    {
        var jsonArtifacts = AgentHandoffContract.RequiredArtifacts
            .Where(artifact => artifact.ContentType == "application/json")
            .Select(artifact => artifact.SchemaName)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(jsonArtifacts, AgentHandoffContract.RequiredSchemaKinds.Select(schema => schema.SchemaKind).ToArray());
        Assert.All(AgentHandoffContract.RequiredSchemaKinds, schema =>
        {
            Assert.Equal("1.0", schema.SchemaVersion);
            Assert.EndsWith(".schema.json", schema.SchemaFileName, StringComparison.Ordinal);
            Assert.NotEmpty(schema.Sha256);
            Assert.True(schema.Required);
        });
    }

    [Fact]
    public void PortableHandoffPackageHash_IsStableForSortedFileLevelEntries()
    {
        var artifacts = new[]
        {
            Artifact("analysis/agent-handoff/page-compositions.json", "agent-handoff-page-compositions", "agent-handoff-page-compositions", "bbb", 20),
            Artifact("analysis/agent-handoff/allowed-files.json", "allowed-files", "allowed-files", "aaa", 10)
        };
        var schemas = new[]
        {
            Schema("agent-handoff-page-compositions", "agent-handoff-page-compositions", "222"),
            Schema("allowed-files", "allowed-files", "111")
        };

        var first = PortableHandoffPackageHasher.ComputePackageHash(artifacts, schemas);
        var second = PortableHandoffPackageHasher.ComputePackageHash(artifacts.Reverse(), schemas.Reverse());

        Assert.Equal(first, second);
    }

    [Fact]
    public void PortableHandoffPackageHash_ChangesWhenConsumerArtifactChanges()
    {
        var original = PortableHandoffPackageHasher.ComputePackageHash(
            [Artifact("analysis/agent-handoff/allowed-files.json", "allowed-files", "allowed-files", "aaa", 10)],
            [Schema("allowed-files", "allowed-files", "111")]);
        var changed = PortableHandoffPackageHasher.ComputePackageHash(
            [Artifact("analysis/agent-handoff/allowed-files.json", "allowed-files", "allowed-files", "bbb", 10)],
            [Schema("allowed-files", "allowed-files", "111")]);

        Assert.NotEqual(original, changed);
    }

    [Fact]
    public void PortableHandoffPackageHash_IgnoresManifestSelfHashAndDiagnostics()
    {
        var first = PortableHandoffPackageHasher.ComputePackageHash(
            [
                Artifact("analysis/agent-handoff/manifest.json", "agent-handoff-manifest", "agent-handoff-manifest", "self-one", 10, includeInPackageHash: false),
                Artifact("analysis/agent-handoff/allowed-files.json", "allowed-files", "allowed-files", "aaa", 10)
            ],
            [Schema("allowed-files", "allowed-files", "111")]);
        var second = PortableHandoffPackageHasher.ComputePackageHash(
            [
                Artifact("analysis/agent-handoff/manifest.json", "agent-handoff-manifest", "agent-handoff-manifest", "self-two", 999, includeInPackageHash: false),
                Artifact("analysis/agent-handoff/allowed-files.json", "allowed-files", "allowed-files", "aaa", 10)
            ],
            [Schema("allowed-files", "allowed-files", "111")]);

        Assert.Equal(first, second);
    }

    [Fact]
    public void PortableHandoffReferencePolicy_DefinesConsumerAndDiagnosticCategories()
    {
        var categories = PortableHandoffReferenceCategories.All.ToDictionary(category => category.Category, StringComparer.Ordinal);

        Assert.True(categories[PortableHandoffReferenceCategories.ConsumerDependency].RequiredFileDependency);
        Assert.True(categories[PortableHandoffReferenceCategories.ConsumerDependency].MustStayInsideHandoffRoot);
        Assert.False(categories[PortableHandoffReferenceCategories.DiagnosticProvenance].RequiredFileDependency);
        Assert.False(categories[PortableHandoffReferenceCategories.ExternalInformationalUrl].RequiredFileDependency);
        Assert.False(categories[PortableHandoffReferenceCategories.GeneratedTargetPath].RequiredFileDependency);
    }

    private static PortableHandoffArtifactEntry Artifact(
        string path,
        string kind,
        string schema,
        string hash,
        long size,
        bool includeInPackageHash = true) =>
        new(path, kind, schema, "1.0", hash, size, Required: true, includeInPackageHash);

    private static PortableHandoffSchemaRequirement Schema(string schemaKind, string artifactKind, string hash) =>
        new(schemaKind, artifactKind, "1.0", $"{schemaKind}.schema.json", hash, Required: true);
}
