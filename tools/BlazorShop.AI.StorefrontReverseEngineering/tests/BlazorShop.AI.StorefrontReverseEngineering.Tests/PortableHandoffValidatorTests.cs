using System.Text.Json.Nodes;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "PortableProof")]
public sealed class PortableHandoffValidatorTests
{
    [Fact]
    public async Task PortableValidator_SucceedsOnCopiedPackage()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable Validator Success");
        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.True(report.ReadinessPassed);
        Assert.False(string.IsNullOrWhiteSpace(report.ProjectId));
        Assert.Equal(0, report.Findings.Count(finding => finding.Severity == "blocking"));
        Assert.False(string.IsNullOrWhiteSpace(report.PackageHash));
        Assert.True(report.ArtifactCount > 0);
        Assert.True(report.SchemaCount > 0);
        Assert.True(report.ConsumerReferenceCount > 0);
        Assert.True(report.DiagnosticProvenanceCount > 0);
    }

    [Fact]
    public async Task PortableValidator_MissingSchemaRootFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable Missing Schema Root");
        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, Path.Combine(fixture.PortableRoot, "missing-schemas"), CancellationToken.None);

        Assert.False(report.ReadinessPassed);
        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-schema-root-missing");
        Assert.Contains(report.Findings, finding => finding.Problem == "Portable schema root is missing.");
    }

    [Fact]
    public async Task PortableValidator_MissingHandoffRootFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable Missing Handoff Root");
        var report = await new PortableHandoffValidator().ValidateAsync(Path.Combine(fixture.PortableRoot, "missing"), fixture.SchemaRoot, CancellationToken.None);

        Assert.False(report.ReadinessPassed);
        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-root-missing");
    }

    [Fact]
    public async Task PortableValidator_ReadinessFalseFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable Readiness False");
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/handoff-readiness.json", json => json["passed"] = false);

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.False(report.ReadinessPassed);
        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-readiness-false");
    }

    [Fact]
    public async Task PortableValidator_ReferenceEscapeFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable Reference Escape");
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/manifest.json", json =>
        {
            json["artifactList"]!.AsArray().Add("../outside.json");
        });

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "handoff-consumer-reference-escape");
    }

    [Fact]
    public async Task PortableValidator_CorruptArtifactFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 3E Portable Corrupt Artifact");
        await File.AppendAllTextAsync(Path.Combine(fixture.PortableRoot, "analysis", "agent-handoff", "allowed-files.json"), " ");

        var report = await new PortableHandoffValidator().ValidateAsync(fixture.PortableRoot, fixture.SchemaRoot, CancellationToken.None);

        Assert.Contains(report.Findings, finding => finding.Code == "portable-handoff-artifact-hash-mismatch");
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
