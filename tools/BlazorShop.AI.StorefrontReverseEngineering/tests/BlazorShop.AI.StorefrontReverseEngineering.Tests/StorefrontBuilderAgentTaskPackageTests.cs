using System.Diagnostics;
using System.Text.Json.Nodes;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderAgentTaskPackage")]
public sealed class StorefrontBuilderAgentTaskPackageTests
{
    [Fact]
    public async Task AgentTaskPackage_ContainsOnlyApprovedInputs()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentPackage");
        var packageRoot = Path.Combine(projectRoot, "docs", "storefront-analysis", "agent-task-package");

        var manifest = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(packageRoot, "manifest.json")))!.AsObject();
        var inputs = manifest["inputs"]!.AsArray().Select(item => item!.GetValue<string>()).ToArray();
        Assert.Contains("inputs/generation-plan.json", inputs);
        Assert.Contains("inputs/handoff-evidence-references.json", inputs);
        Assert.Contains("inputs/design-token-style-summary.json", inputs);
        Assert.Contains("inputs/slot-contract-summary.json", inputs);
        Assert.Contains("inputs/file-boundary-manifest.json", inputs);
        Assert.Contains("inputs/originality-restrictions.json", inputs);
        Assert.Contains("instructions.md", inputs);

        Assert.Contains(manifest["allowedOutputFiles"]!.AsArray(), item => item!["targetPath"]!.GetValue<string>() == "Components/Catalog/PurchasePanelPlaceholder.razor");
        Assert.Contains(manifest["allowedOutputFiles"]!.AsArray(), item => item!["targetPath"]!.GetValue<string>() == "Pages/Hybrid/Commerce/CartPage.razor" && item["visualShellOnly"]!.GetValue<bool>());
        Assert.NotEmpty(manifest["copiedEvidence"]!.AsArray());

        foreach (var file in Directory.EnumerateFiles(packageRoot, "*", SearchOption.AllDirectories).Where(path => !Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)))
        {
            var content = await File.ReadAllTextAsync(file);
            foreach (var forbidden in new[] { "captures/", "analysis/pages/", "analysis/resolved/", "presentation-catalog/", "review/", "reports/", "BlazorShop.Storefront.V2", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API" })
            {
                Assert.DoesNotContain(forbidden, content, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task AgentWriteRecorder_AllowsGeneratedVisualAndUpdatesManifest()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentWrite");
        var target = Path.Combine(projectRoot, "Components", "Catalog", "ProductSummaryCard.razor");
        await File.AppendAllTextAsync(target, Environment.NewLine + "@* agent visual polish marker *@");

        var result = await RunRecordAsync(projectRoot, "Components/Catalog/ProductSummaryCard.razor");

        Assert.True(result.ExitCode == 0, result.Output);
        var record = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "agent-written-files.json"));
        Assert.Contains("sourcePlanEntryId", record, StringComparison.Ordinal);
        Assert.Contains("file.Components-Catalog-ProductSummaryCard.razor", record, StringComparison.Ordinal);
        Assert.Contains("checksum", record, StringComparison.Ordinal);
        var generatedManifest = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "generated-files.yaml"));
        Assert.Contains("agentWrittenFiles:", generatedManifest, StringComparison.Ordinal);
        Assert.Contains("sourcePlanEntryId: file.Components-Catalog-ProductSummaryCard.razor", generatedManifest, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("@page \"/bad\"\n<div></div>", "SFB-AGENT-WRITE-010")]
    [InlineData("@inject HttpClient Http\n<div></div>", "SFB-AGENT-WRITE-011")]
    [InlineData("<script>fetch('/api/storefront/stores/sample/cart')</script>", "SFB-AGENT-WRITE-011")]
    [InlineData("public record StorefrontCartResponse(string Id);", "SFB-AGENT-WRITE-012")]
    public async Task AgentWriteRecorder_RejectsForbiddenGeneratedVisualContent(string content, string expectedCode)
    {
        var projectRoot = await CreateHandoffProjectAsync($"Phase4AgentBad{expectedCode[^3..]}");
        var target = Path.Combine(projectRoot, "Components", "Catalog", "ProductSummaryCard.razor");
        await File.WriteAllTextAsync(target, content);

        var result = await RunRecordAsync(projectRoot, "Components/Catalog/ProductSummaryCard.razor");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(expectedCode, result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentWriteRecorder_RejectsProductPurchaseWhenDescriptorsAreRemoved()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentPurchase");
        var target = Path.Combine(projectRoot, "Components", "Catalog", "PurchasePanelPlaceholder.razor");
        await File.WriteAllTextAsync(target, "<section class=\"visual-purchase\"></section>");

        var result = await RunRecordAsync(projectRoot, "Components/Catalog/PurchasePanelPlaceholder.razor");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-AGENT-WRITE-013", result.Output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("BlazorShop.Storefront.Starter/Components/Bad.razor", "SFB-AGENT-WRITE-003")]
    [InlineData("BlazorShop.Storefront.Presentation/Components/Bad.razor", "SFB-AGENT-WRITE-003")]
    [InlineData("BlazorShop.Storefront.Runtime/Bad.cs", "SFB-AGENT-WRITE-003")]
    [InlineData("BlazorShop.Storefront.Client/Bad.cs", "SFB-AGENT-WRITE-003")]
    [InlineData("BlazorShop.Storefront.Browser/wwwroot/bad.js", "SFB-AGENT-WRITE-003")]
    [InlineData("BlazorShop.Storefront.Components/Bad.razor", "SFB-AGENT-WRITE-003")]
    [InlineData("wwwroot/js/bad.js", "SFB-AGENT-WRITE-004")]
    public async Task AgentWriteRecorder_RejectsProtectedPackageOrUnplannedOutputs(string writtenFile, string expectedCode)
    {
        var projectRoot = await CreateHandoffProjectAsync($"Phase4AgentPath{expectedCode[^3..]}");

        var result = await RunRecordAsync(projectRoot, writtenFile);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(expectedCode, result.Output, StringComparison.Ordinal);
    }

    private static async Task<string> CreateHandoffProjectAsync(string suffix)
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync($"Phase 4 Agent {suffix}");
        fixture.DeleteSourceProject();
        var outputRoot = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-agent-tests", Guid.NewGuid().ToString("N"));
        var projectName = $"BlazorShop.Storefront.{suffix}";
        var result = await RunBuildStorefrontAsync(fixture.PortableRoot, outputRoot, projectName);
        Assert.True(result.ExitCode == 0, result.Output);
        return Path.Combine(outputRoot, projectName);
    }

    private static Task<ProcessResult> RunBuildStorefrontAsync(string handoffRoot, string outputRoot, string projectName)
    {
        var repoRoot = GetRepoRoot();
        return RunProcessAsync(
            "pwsh",
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontBuilder", "build-storefront.ps1"),
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
                handoffRoot,
                "-HandoffSchemaRoot",
                Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "Schemas"),
                "-Force"
            ],
            TimeSpan.FromMinutes(5));
    }

    private static Task<ProcessResult> RunRecordAsync(string projectRoot, string writtenFiles) =>
        RunProcessAsync(
            "node",
            [
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "generate", "record-agent-visual-writes.mjs"),
                "--project-root",
                projectRoot,
                "--written-files",
                writtenFiles
            ],
            TimeSpan.FromMinutes(2));

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
