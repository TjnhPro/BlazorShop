using System.Diagnostics;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderHandoffRepairLoop")]
public sealed class StorefrontBuilderHandoffRepairLoopTests
{
    private static readonly Lazy<Task<string>> BaseProjectRoot = new(CreateBaseProjectAsync);

    [Fact]
    public async Task RepairLoop_CssLayoutFailureRepairsGeneratedOwnedCss()
    {
        var projectRoot = await CopyBaseProjectAsync("css");
        var failureReport = await WriteFailureReportAsync(projectRoot, "Critical: route=shell-home viewport=mobile-390 selector=html cause=Horizontal overflow detected fix=Constrain generated primary regions.");

        var result = await RunRepairAsync(projectRoot, failureReport);

        Assert.True(result.ExitCode == 0, result.Output);
        var css = await File.ReadAllTextAsync(Path.Combine(projectRoot, "wwwroot", "css", "storefront-builder.generated.css"));
        Assert.Contains("StorefrontBuilder repair: bounded visual layout stabilization", css, StringComparison.Ordinal);
        await AssertRepairHistoryAsync(projectRoot, "append bounded responsive CSS repair rules", "applied");
        Assert.Contains("wwwroot/css/storefront-builder.generated.css", await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "agent-written-files.json")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepairLoop_MissingSlotRepairsGeneratedOwnedMarkup()
    {
        var projectRoot = await CopyBaseProjectAsync("slot");
        var failureReport = await WriteFailureReportAsync(projectRoot, "Critical: route=shell-home viewport=desktop-1440 selector=.sfb-hero cause=Required handoff slot 'home.sections' is not visible. fix=Render the planned slot.");

        var result = await RunRepairAsync(projectRoot, failureReport);

        Assert.True(result.ExitCode == 0, result.Output);
        var home = await File.ReadAllTextAsync(Path.Combine(projectRoot, "Pages", "Ssr", "Home", "HomePage.razor"));
        Assert.Contains("StorefrontBuilder repair: home.sections", home, StringComparison.Ordinal);
        await AssertRepairHistoryAsync(projectRoot, "append bounded missing-slot marker for home.sections", "applied");
    }

    [Fact]
    public async Task RepairLoop_ProtectedFileFailureEscalatesToManualBlocker()
    {
        var projectRoot = await CopyBaseProjectAsync("protected");
        var failureReport = await WriteFailureReportAsync(projectRoot, "Protected generated file changed outside an approved foundation update: StorefrontPackageVersions.props");

        var result = await RunRepairAsync(projectRoot, failureReport);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("SFB-REPAIR-010", result.Output, StringComparison.Ordinal);
        await AssertRepairHistoryAsync(projectRoot, "Protected-file repair requires manual foundation scope review.", "manual-blocker");
    }

    [Fact]
    public async Task RepairLoop_RouteDeclarationFailureEscalatesToManualBlocker()
    {
        var projectRoot = await CopyBaseProjectAsync("route");
        var failureReport = await WriteFailureReportAsync(projectRoot, "Generated handoff visual file must not declare @page routes.");

        var result = await RunRepairAsync(projectRoot, failureReport);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("SFB-REPAIR-011", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepairLoop_DirectApiFailureEscalatesToManualBlocker()
    {
        var projectRoot = await CopyBaseProjectAsync("api");
        var failureReport = await WriteFailureReportAsync(projectRoot, "Generated browser source must not call Commerce Node: fetch('/api/storefront/stores/sample/cart')");

        var result = await RunRepairAsync(projectRoot, failureReport);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("SFB-REPAIR-012", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepairLoop_RepeatedFailureStopsWithManualBlocker()
    {
        var projectRoot = await CopyBaseProjectAsync("max");
        await File.WriteAllTextAsync(
            Path.Combine(projectRoot, "docs", "storefront-analysis", "repair-history.md"),
            "# StorefrontBuilder Repair History\n\n## Attempt 1\n\n- result: failed\n");
        var failureReport = await WriteFailureReportAsync(projectRoot, "Major: route=product viewport=mobile selector=.unknown cause=Unknown repeated visual failure.");

        var result = await RunRepairAsync(projectRoot, failureReport, maxAttempts: 1);

        Assert.Equal(3, result.ExitCode);
        Assert.Contains("SFB-REPAIR-020", result.Output, StringComparison.Ordinal);
        await AssertRepairHistoryAsync(projectRoot, "max-attempts-exceeded", "manual-blocker");
    }

    private static async Task AssertRepairHistoryAsync(string projectRoot, string expected, string status)
    {
        var history = await File.ReadAllTextAsync(Path.Combine(projectRoot, "docs", "storefront-analysis", "repair-history.md"));
        Assert.Contains(expected, history, StringComparison.Ordinal);
        Assert.Contains($"status: {status}", history, StringComparison.Ordinal);
        Assert.Contains("failure source:", history, StringComparison.Ordinal);
        Assert.Contains("plan entry id:", history, StringComparison.Ordinal);
        Assert.Contains("remaining blockers:", history, StringComparison.Ordinal);
    }

    private static async Task<string> WriteFailureReportAsync(string projectRoot, string content)
    {
        var path = Path.Combine(projectRoot, "docs", "storefront-analysis", "repair-test-failure.md");
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    private static Task<ProcessResult> RunRepairAsync(string projectRoot, string failureReport, int maxAttempts = 2) =>
        RunProcessAsync(
            "node",
            [
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "qa", "repair-visual-generation.mjs"),
                "--project-root",
                projectRoot,
                "--failure-report",
                failureReport,
                "--max-attempts",
                maxAttempts.ToString()
            ],
            TimeSpan.FromMinutes(2));

    private static async Task<string> CopyBaseProjectAsync(string suffix)
    {
        var source = await BaseProjectRoot.Value;
        var target = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-repair-tests", suffix + "-" + Guid.NewGuid().ToString("N"), Path.GetFileName(source));
        CopyDirectory(source, target);
        return target;
    }

    private static async Task<string> CreateBaseProjectAsync()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Repair Loop");
        fixture.DeleteSourceProject();
        var outputRoot = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-repair-base", Guid.NewGuid().ToString("N"));
        const string projectName = "BlazorShop.Storefront.Phase4Repair";
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

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
        }
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
