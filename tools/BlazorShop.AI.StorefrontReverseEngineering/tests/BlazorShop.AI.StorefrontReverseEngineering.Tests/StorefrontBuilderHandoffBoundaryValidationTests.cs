using System.Diagnostics;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderHandoffBoundary")]
public sealed class StorefrontBuilderHandoffBoundaryValidationTests
{
    [Fact]
    public async Task HandoffGeneratedProject_PassesStaticBoundaryGate()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4BoundaryValid");

        var result = await RunStaticGateAsync(projectRoot, "BlazorShop.Storefront.Phase4BoundaryValid");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Contains("StorefrontBuilder handoff boundary validation passed", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffBoundary_RejectsForbiddenRouteDeclaration()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4BoundaryRoute");
        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "Components", "Catalog", "ProductSummaryCard.razor"),
            "@page \"/bad\"\n<div></div>");

        var result = await RunStaticGateAsync(projectRoot, "BlazorShop.Storefront.Phase4BoundaryRoute");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-BOUNDARY-051", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffBoundary_RejectsForbiddenDirectTransport()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4BoundaryTransport");
        await File.AppendAllTextAsync(
            Path.Combine(projectRoot, "Components", "Catalog", "ProductSummaryCard.razor"),
            "\n<script>fetch('/api/storefront/stores/sample/cart')</script>");

        var result = await RunStaticGateAsync(projectRoot, "BlazorShop.Storefront.Phase4BoundaryTransport");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-BOUNDARY-052", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffBoundary_RejectsForbiddenStorefrontV2Reference()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4BoundaryV2");
        await File.AppendAllTextAsync(
            Path.Combine(projectRoot, "Components", "Catalog", "ProductSummaryCard.razor"),
            "\n@* BlazorShop.Storefront.V2 *@");

        var result = await RunStaticGateAsync(projectRoot, "BlazorShop.Storefront.Phase4BoundaryV2");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("BlazorShop.Storefront.V2", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffBoundary_RejectsForbiddenRawEvidenceReference()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4BoundaryRawEvidence");
        await File.AppendAllTextAsync(
            Path.Combine(projectRoot, "Components", "Catalog", "ProductSummaryCard.razor"),
            "\n@* captures/home/raw.png *@");

        var result = await RunStaticGateAsync(projectRoot, "BlazorShop.Storefront.Phase4BoundaryRawEvidence");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-BOUNDARY-050", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffBoundary_RejectsProtectedFileMutation()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4BoundaryProtected");
        await File.AppendAllTextAsync(
            Path.Combine(projectRoot, "StorefrontPackageVersions.props"),
            "\n<!-- protected mutation -->");
        var updateResult = await RunUpdateManifestAsync(projectRoot);
        Assert.True(updateResult.ExitCode == 0, updateResult.Output);

        var result = await RunStaticGateAsync(projectRoot, "BlazorShop.Storefront.Phase4BoundaryProtected");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-BOUNDARY-062", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffBoundary_RejectsMissingPlanEntryInGeneratedManifest()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4BoundaryPlanId");
        var manifestPath = Path.Combine(projectRoot, "docs", "storefront-analysis", "generated-files.yaml");
        var manifest = await File.ReadAllTextAsync(manifestPath);
        await File.WriteAllTextAsync(
            manifestPath,
            manifest.Replace(
                "    sourcePlanEntryId: file.Components-Catalog-ProductSummaryCard.razor",
                "    sourcePlanEntryId: none",
                StringComparison.Ordinal));

        var result = await RunStaticGateAsync(projectRoot, "BlazorShop.Storefront.Phase4BoundaryPlanId");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-HANDOFF-BOUNDARY-031", result.Output, StringComparison.Ordinal);
    }

    private static async Task<string> CreateHandoffProjectAsync(string suffix)
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync($"Phase 4 Boundary {suffix}");
        fixture.DeleteSourceProject();
        var outputRoot = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-boundary-tests", Guid.NewGuid().ToString("N"));
        var projectName = $"BlazorShop.Storefront.{suffix}";
        var result = await RunProcessAsync(
            "pwsh",
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "build-storefront.ps1"),
                "-Url",
                "https://example.test",
                "-Name",
                projectName,
                "-StoreKey",
                "sample",
                "-OutputRoot",
                outputRoot,
                "-Mode",
                "generate",
                "-HandoffRoot",
                fixture.PortableRoot,
                "-HandoffSchemaRoot",
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas"),
                "-Force"
            ],
            TimeSpan.FromMinutes(5));
        Assert.True(result.ExitCode == 0, result.Output);
        return Path.Combine(outputRoot, projectName);
    }

    private static Task<ProcessResult> RunStaticGateAsync(string projectRoot, string projectName) =>
        RunProcessAsync(
            "pwsh",
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "validate", "Test-StorefrontBuilderStaticGate.ps1"),
                "-ProjectRoot",
                projectRoot,
                "-Name",
                projectName,
                "-StoreKey",
                "sample"
            ],
            TimeSpan.FromMinutes(3));

    private static Task<ProcessResult> RunUpdateManifestAsync(string projectRoot) =>
        RunProcessAsync(
            "node",
            [
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "generate", "update-generated-files-manifest.mjs"),
                "--project-root",
                projectRoot
            ],
            TimeSpan.FromMinutes(1));

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

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();

    private sealed record ProcessResult(int ExitCode, string Output);
}
