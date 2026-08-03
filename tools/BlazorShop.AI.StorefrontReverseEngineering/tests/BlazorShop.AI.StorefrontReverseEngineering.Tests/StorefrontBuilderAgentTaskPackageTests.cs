using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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
        Assert.Contains("agentFilePath: Components/Catalog/ProductSummaryCard.razor", generatedManifest, StringComparison.Ordinal);
        Assert.Contains("sourcePlanEntryId: file.Components-Catalog-ProductSummaryCard.razor", generatedManifest, StringComparison.Ordinal);
        Assert.Contains("manualEditDetected: true", generatedManifest, StringComparison.Ordinal);
        Assert.Contains("conflictStatus: agent-visual-edit", generatedManifest, StringComparison.Ordinal);
        Assert.Contains("currentHash: sha256:", generatedManifest, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentWriteRecorder_AutoDetectsGeneratedVisualFromCheckpoint()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentAutoDetect");
        var targetPath = "Components/Catalog/ProductSummaryCard.razor";
        var target = Path.Combine(projectRoot, targetPath.Replace('/', Path.DirectorySeparatorChar));
        await File.AppendAllTextAsync(target, Environment.NewLine + "@* auto-detected visual polish marker *@");
        var checkpointPath = await WriteCheckpointAsync(projectRoot, "auto-detect", targetPath);

        var result = await RunRecordAsync(projectRoot, checkpointPath: checkpointPath, closureMode: true);

        Assert.True(result.ExitCode == 0, result.Output);
        var record = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "agent-written-files.json"));
        Assert.Contains("\"detectionMode\": \"checkpoint-auto-detect\"", record, StringComparison.Ordinal);
        Assert.Contains("\"detectionSource\": \"auto-detected\"", record, StringComparison.Ordinal);
        Assert.Contains(targetPath, record, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentWriteRecorder_RejectsCheckpointUnexpectedFiles()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentUnexpected");
        var checkpointPath = await WriteCheckpointAsync(
            projectRoot,
            "unexpected",
            "Components/Catalog/ProductSummaryCard.razor",
            unexpectedFiles: ["wwwroot/js/bad.js"]);

        var result = await RunRecordAsync(projectRoot, checkpointPath: checkpointPath, closureMode: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-AGENT-WRITE-021", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentWriteRecorder_RejectsOmittedChangedFileHintInClosureMode()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentOmitted");
        var checkpointPath = await WriteCheckpointAsync(projectRoot, "omitted", "Components/Catalog/ProductSummaryCard.razor");

        var result = await RunRecordAsync(projectRoot, "wwwroot/css/storefront-builder.generated.css", checkpointPath, closureMode: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-AGENT-WRITE-025", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentWriteRecorder_RejectsCheckpointPostHashThatDoesNotMatchCurrentFile()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentPostHash");
        var targetPath = "Components/Catalog/ProductSummaryCard.razor";
        var target = Path.Combine(projectRoot, targetPath.Replace('/', Path.DirectorySeparatorChar));
        await File.AppendAllTextAsync(target, Environment.NewLine + "@* stale post hash marker *@");
        var checkpointPath = await WriteCheckpointAsync(projectRoot, "post-hash", targetPath, postHashOverride: "sha256:0000000000000000000000000000000000000000000000000000000000000000");

        var result = await RunRecordAsync(projectRoot, checkpointPath: checkpointPath, closureMode: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-AGENT-WRITE-029", result.Output, StringComparison.Ordinal);
        Assert.Contains("Checkpoint post hash differs from current file content", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentWriteRecorder_RejectsProtectedCheckpointChange()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentProtectedDetect");
        var checkpointPath = await WriteCheckpointAsync(projectRoot, "protected", "Program.cs");

        var result = await RunRecordAsync(projectRoot, checkpointPath: checkpointPath, closureMode: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-AGENT-WRITE-022", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AgentWriteRecorder_WarnsForUnchangedHintOutsideClosureMode()
    {
        var projectRoot = await CreateHandoffProjectAsync("Phase4AgentHintWarn");
        var targetPath = "Components/Catalog/ProductSummaryCard.razor";
        var checkpointPath = await WriteCheckpointAsync(projectRoot, "hint-warn", targetPath);

        var result = await RunRecordAsync(projectRoot, $"{targetPath},wwwroot/css/storefront-builder.generated.css", checkpointPath);

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Contains("SFB-AGENT-WRITE-WARN", result.Output, StringComparison.Ordinal);
        var record = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "agent-written-files.json"));
        Assert.Contains("\"detectionSource\": \"auto-detected+hint-agreed\"", record, StringComparison.Ordinal);
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

    private static Task<ProcessResult> RunRecordAsync(string projectRoot, string writtenFiles = "", string checkpointPath = "", bool closureMode = false)
    {
        var arguments = new List<string>
        {
            Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "generate", "record-agent-visual-writes.mjs"),
            "--project-root",
            projectRoot
        };

        if (!string.IsNullOrWhiteSpace(writtenFiles))
        {
            arguments.Add("--written-files");
            arguments.Add(writtenFiles);
        }

        if (!string.IsNullOrWhiteSpace(checkpointPath))
        {
            arguments.Add("--from-checkpoint");
            arguments.Add(checkpointPath);
        }

        if (closureMode)
        {
            arguments.Add("--closure-mode");
        }

        return RunProcessAsync("node", arguments, TimeSpan.FromMinutes(2));
    }

    private static async Task<string> WriteCheckpointAsync(
        string projectRoot,
        string operationId,
        string changedFile,
        IReadOnlyList<string>? unexpectedFiles = null,
        string? postHashOverride = null)
    {
        var checkpointRoot = Path.Combine(projectRoot, "docs", "storefront-analysis", "visual-checkpoints", operationId);
        Directory.CreateDirectory(checkpointRoot);
        var checkpointPath = Path.Combine(checkpointRoot, "visual-checkpoint.json");
        var normalizedFile = changedFile.Replace('\\', '/');
        var fullChangedPath = Path.Combine(projectRoot, normalizedFile.Replace('/', Path.DirectorySeparatorChar));
        var currentHash = ComputeNormalizedSha256(fullChangedPath);
        var postHash = postHashOverride ?? currentHash;
        var checkpoint = $$"""
        {
          "schemaVersion": "0.1.0",
          "checkpointId": "checkpoint-{{operationId}}",
          "operationId": "{{operationId}}",
          "visualPlanHash": "sha256:visual-plan",
          "checklistHash": "sha256:checklist",
          "preEditSnapshotHash": "sha256:before",
          "postEditSnapshotHash": "sha256:after",
          "changedFiles": [
            "{{normalizedFile}}"
          ],
          "unexpectedFiles": [
            {{string.Join(",\n            ", (unexpectedFiles ?? []).Select(file => $"\"{file}\""))}}
          ],
          "sourceTreeSnapshotScope": [
            "{{normalizedFile}}"
          ],
          "preEditFileHashes": [
            {
              "filePath": "{{normalizedFile}}",
              "sha256": "sha256:1111111111111111111111111111111111111111111111111111111111111111"
            }
          ],
          "postEditFileHashes": [
            {
              "filePath": "{{normalizedFile}}",
              "sha256": "{{postHash}}"
            }
          ],
          "diffSummary": [
            {
              "filePath": "{{normalizedFile}}",
              "changeType": "modified"
            }
          ]
        }
        """;
        await File.WriteAllTextAsync(checkpointPath, checkpoint);
        return Path.GetRelativePath(projectRoot, checkpointPath).Replace('\\', '/');
    }

    private static string ComputeNormalizedSha256(string path)
    {
        var text = Encoding.UTF8.GetString(File.ReadAllBytes(path))
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
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

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();

    private sealed record ProcessResult(int ExitCode, string Output);
}
