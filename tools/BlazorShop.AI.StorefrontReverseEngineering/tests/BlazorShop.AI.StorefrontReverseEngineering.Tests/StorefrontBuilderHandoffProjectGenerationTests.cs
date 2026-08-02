using System.Diagnostics;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderHandoffProjectGeneration")]
public sealed class StorefrontBuilderHandoffProjectGenerationTests
{
    [Fact]
    public async Task HandoffGenerate_CreatesDisposableProjectArtifactsAndBuilds()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Project Skeleton");
        fixture.DeleteSourceProject();
        var outputRoot = CreateGeneratedOutputRoot();
        var projectName = "BlazorShop.Storefront.Phase4ProjectSkeleton";

        var generate = await RunBuildStorefrontAsync(fixture.PortableRoot, outputRoot, projectName, "generate");

        Assert.True(generate.ExitCode == 0, generate.Output);
        var projectRoot = Path.Combine(outputRoot, projectName);
        Assert.True(File.Exists(Path.Combine(projectRoot, $"{projectName}.csproj")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "docs", "storefront-analysis", "generation-plan.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "docs", "storefront-analysis", "generation-plan.yaml")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "docs", "storefront-analysis", "handoff-generation-summary.md")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "docs", "storefront-analysis", "handoff-placeholders.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "docs", "storefront-analysis", "generated-files.yaml")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "wwwroot", "css", "storefront-builder.generated.css")));

        var metadata = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "metadata.yaml"));
        Assert.Contains("generationMode: handoff-project-skeleton", metadata, StringComparison.Ordinal);
        Assert.Contains("storefrontContractSha256:", metadata, StringComparison.Ordinal);
        Assert.Contains("starterContractSha256:", metadata, StringComparison.Ordinal);
        Assert.Contains("sourceHandoffPackageHash:", metadata, StringComparison.Ordinal);
        Assert.Contains("sourceHandoffReadinessHash:", metadata, StringComparison.Ordinal);
        Assert.Contains("planSha256:", metadata, StringComparison.Ordinal);

        var summary = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "handoff-generation-summary.md"));
        Assert.Contains("Placeholder Files", summary, StringComparison.Ordinal);
        Assert.Contains("Warnings", summary, StringComparison.Ordinal);

        var applicationHead = await File.ReadAllTextAsync(Path.Combine(projectRoot, "Components", "Layout", "ApplicationHead.razor"));
        Assert.Contains("css/storefront-builder.generated.css", applicationHead, StringComparison.Ordinal);

        var solution = await File.ReadAllTextAsync(Path.Combine(GetRepoRoot(), "BlazorShop.sln"));
        Assert.DoesNotContain(projectName, solution, StringComparison.Ordinal);

        var restore = await RunProcessAsync("dotnet", [ "restore", Path.Combine(projectRoot, $"{projectName}.csproj") ], TimeSpan.FromMinutes(5));
        Assert.True(restore.ExitCode == 0, restore.Output);

        var build = await RunProcessAsync("dotnet", [ "build", Path.Combine(projectRoot, $"{projectName}.csproj"), "--no-restore" ], TimeSpan.FromMinutes(5));
        Assert.True(build.ExitCode == 0, build.Output);
    }

    [Fact]
    public async Task HandoffGenerate_CreatesArtifactProjectRoot()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Project Artifact Root");
        fixture.DeleteSourceProject();
        var outputRoot = Path.Combine(GetRepoRoot(), "artifacts", "storefront-builder", "generated", "phase4-project-tests", Guid.NewGuid().ToString("N"));
        var projectName = "BlazorShop.Storefront.Phase4ArtifactProject";

        var generate = await RunBuildStorefrontAsync(fixture.PortableRoot, outputRoot, projectName, "generate");

        Assert.True(generate.ExitCode == 0, generate.Output);
        var projectRoot = Path.Combine(outputRoot, projectName);
        Assert.True(File.Exists(Path.Combine(projectRoot, $"{projectName}.csproj")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "docs", "storefront-analysis", "generation-plan.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "docs", "storefront-analysis", "handoff-generation-summary.md")));
    }

    [Fact]
    public async Task HandoffGenerate_UnsafeProjectNameFailsBeforeFilesAreCreated()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Project Unsafe Name");
        var outputRoot = CreateGeneratedOutputRoot();

        var result = await RunBuildStorefrontAsync(fixture.PortableRoot, outputRoot, "bad-name", "generate");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-PROJECT-001", result.Output, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(outputRoot, "bad-name")));
    }

    [Fact]
    public async Task HandoffSkeleton_PlanTargetingStarterFails()
    {
        var outputRoot = CreateGeneratedOutputRoot();
        var projectRoot = Path.Combine(outputRoot, "BlazorShop.Storefront.Phase4StarterTarget");
        var analysisRoot = Path.Combine(projectRoot, "docs", "storefront-analysis");
        Directory.CreateDirectory(analysisRoot);
        var planPath = Path.Combine(analysisRoot, "generation-plan.json");
        await File.WriteAllTextAsync(planPath, """
        {
          "generationMode": "handoff",
          "projectName": "BlazorShop.Storefront.Phase4StarterTarget",
          "storeKey": "sample",
          "blockedItems": [],
          "warnings": [],
          "tokens": [],
          "files": [
            {
              "id": "file.bad-starter-target",
              "targetPath": "BlazorShop.Storefront.Starter/Components/Bad.razor",
              "ownership": "generated",
              "action": "replace",
              "allowedOperation": "replace",
              "declaresRoute": false,
              "slots": [ "home.sections" ],
              "sourceHandoffArtifacts": [ "analysis/agent-handoff/page-compositions.json" ],
              "sourceEvidenceReferences": []
            }
          ]
        }
        """);

        var script = Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "generate", "apply-handoff-project-skeleton.mjs");
        var result = await RunProcessAsync("node", [ script, "--project-root", projectRoot, "--plan-json", planPath ], TimeSpan.FromMinutes(2));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-GEN-004", result.Output, StringComparison.Ordinal);
    }

    private static Task<ProcessResult> RunBuildStorefrontAsync(string handoffRoot, string outputRoot, string projectName, string mode)
    {
        var repoRoot = GetRepoRoot();
        var script = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontBuilder", "build-storefront.ps1");
        var schemaRoot = Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas");
        return RunProcessAsync(
            "pwsh",
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                script,
                "-Url",
                "https://example.test",
                "-Name",
                projectName,
                "-StoreKey",
                "sample",
                "-OutputRoot",
                outputRoot,
                "-Mode",
                mode,
                "-HandoffRoot",
                handoffRoot,
                "-HandoffSchemaRoot",
                schemaRoot,
                "-Force"
            ],
            TimeSpan.FromMinutes(5));
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = GetRepoRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var cts = new CancellationTokenSource(timeout);
        await process.WaitForExitAsync(cts.Token);
        var output = (await stdoutTask) + (await stderrTask);
        return new ProcessResult(process.ExitCode, output);
    }

    private static string CreateGeneratedOutputRoot() =>
        Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-project-tests", Guid.NewGuid().ToString("N"));

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();

    private sealed record ProcessResult(int ExitCode, string Output);
}
