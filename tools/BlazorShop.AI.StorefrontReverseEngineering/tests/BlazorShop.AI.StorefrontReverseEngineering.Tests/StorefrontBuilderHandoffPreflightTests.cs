using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BlazorShop.AI.StorefrontReverseEngineering.Analysis.Handoff;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderHandoffPreflight")]
public sealed class StorefrontBuilderHandoffPreflightTests
{
    [Fact]
    public async Task PreflightOnly_AcceptsCopiedPortablePackageWithoutGeneratingProject()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Positive");
        fixture.DeleteSourceProject();
        var outputRoot = CreateBuilderOutputRoot();

        var result = await RunBuildStorefrontAsync(
            outputRoot,
            "Phase4PreflightPositive",
            fixture.PortableRoot,
            fixture.SchemaRoot);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Handoff preflight report:", result.Output, StringComparison.Ordinal);
        Assert.Contains("Readiness passed: True", File.ReadAllText(ExtractReportPath(result.Output)), StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(outputRoot, "BlazorShop.Storefront.Phase4PreflightPositive")));
    }

    [Fact]
    public async Task PreflightOnly_AcceptsAnalysisAgentHandoffFolder()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Direct Folder");

        var result = await RunBuildStorefrontAsync(
            CreateBuilderOutputRoot(),
            "Phase4PreflightFolder",
            Path.Combine(fixture.PortableRoot, "analysis", "agent-handoff"),
            fixture.SchemaRoot);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Status: passed", File.ReadAllText(ExtractReportPath(result.Output)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreflightOnly_MissingReadinessFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Missing Readiness");
        File.Delete(Path.Combine(fixture.PortableRoot, "analysis", "agent-handoff", "handoff-readiness.json"));

        var result = await RunBuildStorefrontAsync(CreateBuilderOutputRoot(), "Phase4MissingReadiness", fixture.PortableRoot, fixture.SchemaRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-006", result.Output, StringComparison.Ordinal);
        Assert.Contains("Status: failed", File.ReadAllText(ExtractReportPath(result.Output)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreflightOnly_ReadinessFalseFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Readiness False");
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/handoff-readiness.json", json => json["passed"] = false);

        var result = await RunBuildStorefrontAsync(CreateBuilderOutputRoot(), "Phase4ReadinessFalse", fixture.PortableRoot, fixture.SchemaRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-008", result.Output, StringComparison.Ordinal);
        Assert.Contains("portable-handoff-readiness-false", File.ReadAllText(ExtractReportPath(result.Output)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreflightOnly_ManifestReadinessMismatchFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Readiness Mismatch");
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/manifest.json", json => json["readinessPassed"] = false);

        var result = await RunBuildStorefrontAsync(CreateBuilderOutputRoot(), "Phase4ReadinessMismatch", fixture.PortableRoot, fixture.SchemaRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-008", result.Output, StringComparison.Ordinal);
        Assert.Contains("portable-handoff-readiness-mismatch", File.ReadAllText(ExtractReportPath(result.Output)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreflightOnly_ArtifactHashDriftFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Hash Drift");
        await File.AppendAllTextAsync(Path.Combine(fixture.PortableRoot, "analysis", "agent-handoff", "allowed-files.json"), " ");

        var result = await RunBuildStorefrontAsync(CreateBuilderOutputRoot(), "Phase4HashDrift", fixture.PortableRoot, fixture.SchemaRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-008", result.Output, StringComparison.Ordinal);
        Assert.Contains("portable-handoff-artifact-hash-mismatch", File.ReadAllText(ExtractReportPath(result.Output)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreflightOnly_MissingSchemaFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Missing Schema");
        var missingSchemaRoot = Path.Combine(fixture.PortableRoot, "missing-schemas");

        var result = await RunBuildStorefrontAsync(CreateBuilderOutputRoot(), "Phase4MissingSchema", fixture.PortableRoot, missingSchemaRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-007", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreflightOnly_ConsumerReferenceOutsideHandoffFails()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Preflight Reference Escape");
        await MutateJsonAsync(fixture.PortableRoot, "analysis/agent-handoff/visual-blueprint.json", json =>
        {
            json["consumerReferences"]!.AsObject()["pageCompositions"] = "../outside.json";
        });
        await RehashPortableManifestAsync(fixture.PortableRoot, "analysis/agent-handoff/visual-blueprint.json");

        var result = await RunBuildStorefrontAsync(CreateBuilderOutputRoot(), "Phase4ReferenceEscape", fixture.PortableRoot, fixture.SchemaRoot);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-008", result.Output, StringComparison.Ordinal);
        Assert.Contains("handoff-consumer-reference-escape", File.ReadAllText(ExtractReportPath(result.Output)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreflightOnly_RawCaptureFallbackPathFailsBeforeValidation()
    {
        var repoRoot = GetRepoRoot();
        var rawRoot = Path.Combine(repoRoot, "obj", "storefront-reverse-engineering", "projects", "phase4-raw-fallback", "analysis", "pages");
        Directory.CreateDirectory(rawRoot);

        var result = await RunBuildStorefrontAsync(
            CreateBuilderOutputRoot(),
            "Phase4RawFallback",
            rawRoot,
            Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas"));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-004", result.Output, StringComparison.Ordinal);
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

    private static async Task<ProcessResult> RunBuildStorefrontAsync(
        string outputRoot,
        string name,
        string handoffRoot,
        string schemaRoot)
    {
        var repoRoot = GetRepoRoot();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        foreach (var argument in new[]
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontBuilder", "build-storefront.ps1"),
            "-Url",
            "https://example.test",
            "-Name",
            name,
            "-StoreKey",
            "sample",
            "-OutputRoot",
            outputRoot,
            "-Mode",
            "preflight-only",
            "-HandoffRoot",
            handoffRoot,
            "-HandoffSchemaRoot",
            schemaRoot
        })
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await process.WaitForExitAsync(timeout.Token);
        var output = (await stdoutTask) + (await stderrTask);
        return new ProcessResult(process.ExitCode, output);
    }

    private static string ExtractReportPath(string output)
    {
        var match = Regex.Match(output, @"Handoff preflight report: (?<path>.+\.md)");
        if (!match.Success)
        {
            throw new InvalidOperationException("Preflight output did not contain a report path: " + output);
        }

        return match.Groups["path"].Value.Trim();
    }

    private static string CreateBuilderOutputRoot() =>
        Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "handoff-preflight-tests", Guid.NewGuid().ToString("N"));

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();

    private sealed record ProcessResult(int ExitCode, string Output);
}
