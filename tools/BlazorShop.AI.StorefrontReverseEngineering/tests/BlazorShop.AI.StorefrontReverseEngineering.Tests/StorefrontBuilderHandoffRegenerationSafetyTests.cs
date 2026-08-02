using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontBuilderHandoffRegenerationSafety")]
public sealed class StorefrontBuilderHandoffRegenerationSafetyTests
{
    private static readonly Lazy<Task<string>> BaseProjectRoot = new(CreateBaseProjectAsync);

    [Fact]
    public async Task HandoffRegeneration_NoOpProducesNoDiff()
    {
        var projectRoot = await CopyBaseProjectAsync("noop");
        var before = SnapshotProject(projectRoot);

        var result = await RunRegenerationAsync(projectRoot, "all");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Equal(before, SnapshotProject(projectRoot));
    }

    [Fact]
    public async Task HandoffRegeneration_ScopedCssTouchesOnlyPlannedCss()
    {
        var projectRoot = await CopyBaseProjectAsync("css");
        var cssPath = Path.Combine(projectRoot, "wwwroot", "css", "storefront-builder.generated.css");
        await File.WriteAllTextAsync(cssPath, "/* stale generated css */\n.sfb-stale-css { display: block; }\n");
        await UpdateManifestAsync(projectRoot, "wwwroot/css/storefront-builder.generated.css");
        var before = SnapshotProject(projectRoot);

        var result = await RunRegenerationAsync(projectRoot, "css");

        Assert.True(result.ExitCode == 0, result.Output);
        var changed = ChangedFiles(before, SnapshotProject(projectRoot));
        Assert.Equal(["wwwroot/css/storefront-builder.generated.css"], changed);
        Assert.DoesNotContain("sfb-stale-css", await File.ReadAllTextAsync(cssPath), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffRegeneration_ScopedComponentTouchesOnlyPlannedComponent()
    {
        var projectRoot = await CopyBaseProjectAsync("component");
        const string componentPath = "Components/Catalog/ProductSummaryCard.razor";
        var fullPath = Path.Combine(projectRoot, componentPath.Replace('/', Path.DirectorySeparatorChar));
        await File.WriteAllTextAsync(fullPath, "<section class=\"stale-generated-component\">stale generated component</section>\n");
        await UpdateManifestAsync(projectRoot, componentPath);
        var before = SnapshotProject(projectRoot);

        var result = await RunRegenerationAsync(projectRoot, "component", "ProductSummaryCard");

        Assert.True(result.ExitCode == 0, result.Output);
        var changed = ChangedFiles(before, SnapshotProject(projectRoot));
        Assert.Equal([componentPath], changed);
        Assert.DoesNotContain("stale-generated-component", await File.ReadAllTextAsync(fullPath), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffRegeneration_SupportsPageFoundationValidateAndConflictsScopes()
    {
        var projectRoot = await CopyBaseProjectAsync("scopes");

        var page = await RunRegenerationAsync(projectRoot, "page", "HomePage", whatIf: true);
        var foundation = await RunRegenerationAsync(projectRoot, "foundation", whatIf: true);
        var validate = await RunRegenerationAsync(projectRoot, "validate");
        var conflicts = await RunRegenerationAsync(projectRoot, "conflicts");

        Assert.True(page.ExitCode == 0, page.Output);
        Assert.True(foundation.ExitCode == 0, foundation.Output);
        Assert.True(validate.ExitCode == 0, validate.Output);
        Assert.True(conflicts.ExitCode == 0, conflicts.Output);
        Assert.Contains("WhatIf report:", page.Output, StringComparison.Ordinal);
        Assert.Contains("WhatIf report:", foundation.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffRegeneration_ManualEditConflictIsReportedNotOverwritten()
    {
        var projectRoot = await CopyBaseProjectAsync("manual");
        const string componentPath = "Components/Catalog/ProductSummaryCard.razor";
        var fullPath = Path.Combine(projectRoot, componentPath.Replace('/', Path.DirectorySeparatorChar));
        await File.AppendAllTextAsync(fullPath, "\n@* manual visual edit that must survive regeneration *@\n");

        var result = await RunRegenerationAsync(projectRoot, "component", "ProductSummaryCard", whatIf: true);

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Contains("conflict manual edit", result.Output, StringComparison.Ordinal);
        Assert.Contains("manual visual edit that must survive regeneration", await File.ReadAllTextAsync(fullPath), StringComparison.Ordinal);
        var report = await ReadWhatIfReportAsync(result.Output);
        Assert.Contains(componentPath, report, StringComparison.Ordinal);
        Assert.Contains("conflict manual edit", report, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffRegeneration_ObsoleteGeneratedFileIsReported()
    {
        var projectRoot = await CopyBaseProjectAsync("obsolete");
        const string componentPath = "Components/Catalog/ProductSummaryCard.razor";

        var result = await RunRegenerationAsync(
            projectRoot,
            "component",
            "ProductSummaryCard",
            whatIf: true,
            environment: new Dictionary<string, string> { ["SFB_DROP_CANDIDATE_FILE_PATHS"] = componentPath });

        Assert.True(result.ExitCode == 0, result.Output);
        var report = await ReadWhatIfReportAsync(result.Output);
        Assert.Contains(componentPath, report, StringComparison.Ordinal);
        Assert.Contains("obsolete candidate", report, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffRegeneration_WhatIfReportSurvivesCandidateCleanup()
    {
        var projectRoot = await CopyBaseProjectAsync("whatif");

        var result = await RunRegenerationAsync(projectRoot, "all", whatIf: true);

        Assert.True(result.ExitCode == 0, result.Output);
        var reportPath = ExtractWhatIfReportPath(result.Output);
        Assert.True(File.Exists(reportPath), result.Output);
        var candidateRoot = Path.Combine(Directory.GetParent(projectRoot)!.FullName, ".regeneration-candidate");
        Assert.False(Directory.Exists(candidateRoot) && Directory.EnumerateFileSystemEntries(candidateRoot).Any());
        Assert.Contains("StorefrontBuilder Regeneration Report", await File.ReadAllTextAsync(reportPath), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffRegeneration_ChangedHandoffHashWithoutReplanFails()
    {
        var projectRoot = await CopyBaseProjectAsync("hash-drift");
        var metadataPath = Path.Combine(projectRoot, "docs", "storefront-analysis", "metadata.yaml");
        var metadata = await File.ReadAllTextAsync(metadataPath);
        await File.WriteAllTextAsync(metadataPath, metadata.Replace("sourceHandoffPackageHash: ", "sourceHandoffPackageHash: deadbeef"));

        var result = await RunRegenerationAsync(projectRoot, "all", whatIf: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-REGEN-HANDOFF-010", result.Output, StringComparison.Ordinal);
        Assert.Contains("re-plan", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandoffRegeneration_ProtectedFileTargetInPlanFails()
    {
        var projectRoot = await CopyBaseProjectAsync("protected-plan");
        var planPath = Path.Combine(projectRoot, "docs", "storefront-analysis", "generation-plan.json");
        var plan = JsonNode.Parse(await File.ReadAllTextAsync(planPath))!.AsObject();
        var firstFile = plan["files"]!.AsArray()[0]!.AsObject();
        firstFile["targetPath"] = "StorefrontPackageVersions.props";
        firstFile["ownership"] = "generated";
        firstFile["allowedOperation"] = "replace";
        await File.WriteAllTextAsync(planPath, plan.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
        await RewriteMetadataPlanHashAsync(projectRoot);

        var result = await RunRegenerationAsync(projectRoot, "all", whatIf: true);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("SFB-REGEN-HANDOFF-020", result.Output, StringComparison.Ordinal);
    }

    private static async Task<string> CopyBaseProjectAsync(string suffix)
    {
        var source = await BaseProjectRoot.Value;
        var target = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-regeneration-tests", suffix + "-" + Guid.NewGuid().ToString("N"), Path.GetFileName(source));
        CopyDirectory(source, target);
        await RewriteMetadataValueAsync(target, "outputRoot", Directory.GetParent(target)!.FullName.Replace('\\', '/'));
        await UpdateManifestAsync(target, "docs/storefront-analysis/metadata.yaml");
        return target;
    }

    private static async Task<string> CreateBaseProjectAsync()
    {
        var fixture = await PortableHandoffTestFixture.CreateAsync("Phase 4 Regeneration");
        fixture.DeleteSourceProject();
        var outputRoot = Path.Combine(GetRepoRoot(), "obj", "storefront-builder", "generated", "phase4-regeneration-base", Guid.NewGuid().ToString("N"));
        const string projectName = "BlazorShop.Storefront.Phase4Regeneration";
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

    private static Task<ProcessResult> RunRegenerationAsync(
        string projectRoot,
        string scope,
        string target = "",
        bool whatIf = false,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var args = new List<string>
        {
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "regenerate-storefront.ps1"),
            "-ProjectRoot",
            projectRoot,
            "-Scope",
            scope
        };
        if (!string.IsNullOrWhiteSpace(target))
        {
            args.Add("-Target");
            args.Add(target);
        }

        if (whatIf)
        {
            args.Add("-WhatIf");
        }

        return RunProcessAsync("pwsh", args, TimeSpan.FromMinutes(5), environment);
    }

    private static async Task UpdateManifestAsync(string projectRoot, string intentionalChanges)
    {
        var result = await RunProcessAsync(
            "node",
            [
                Path.Combine(GetRepoRoot(), "tools", "BlazorShop.AI.StorefrontBuilder", "scripts", "generate", "update-generated-files-manifest.mjs"),
                "--project-root",
                projectRoot,
                "--intentional-changes",
                intentionalChanges
            ],
            TimeSpan.FromMinutes(2));
        Assert.True(result.ExitCode == 0, result.Output);
    }

    private static async Task RewriteMetadataPlanHashAsync(string projectRoot)
    {
        var planPath = Path.Combine(projectRoot, "docs", "storefront-analysis", "generation-plan.json");
        var planHash = Sha256Normalized(await File.ReadAllTextAsync(planPath));
        await RewriteMetadataValueAsync(projectRoot, "planSha256", planHash);
    }

    private static async Task RewriteMetadataValueAsync(string projectRoot, string key, string value)
    {
        var metadataPath = Path.Combine(projectRoot, "docs", "storefront-analysis", "metadata.yaml");
        var lines = (await File.ReadAllTextAsync(metadataPath)).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].TrimStart().StartsWith(key + ":", StringComparison.Ordinal))
            {
                var indent = lines[index][..(lines[index].Length - lines[index].TrimStart().Length)];
                lines[index] = $"{indent}{key}: {value}".TrimEnd('\r');
                break;
            }
        }

        await File.WriteAllTextAsync(metadataPath, string.Join('\n', lines));
    }

    private static SortedDictionary<string, string> SnapshotProject(string projectRoot)
    {
        var snapshot = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
            if (relative.StartsWith("bin/", StringComparison.Ordinal)
                || relative.StartsWith("obj/", StringComparison.Ordinal)
                || relative.StartsWith(".regeneration-candidate/", StringComparison.Ordinal)
                || relative.StartsWith(".regeneration-backup/", StringComparison.Ordinal)
                || relative is "docs/storefront-analysis/generated-files.yaml" or "docs/storefront-analysis/regeneration-report.md")
            {
                continue;
            }

            snapshot[relative] = Sha256File(file);
        }

        return snapshot;
    }

    private static List<string> ChangedFiles(SortedDictionary<string, string> before, SortedDictionary<string, string> after)
    {
        var keys = before.Keys.Concat(after.Keys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return keys
            .Where(key => !before.TryGetValue(key, out var beforeHash) || !after.TryGetValue(key, out var afterHash) || beforeHash != afterHash)
            .ToList();
    }

    private static async Task<string> ReadWhatIfReportAsync(string output) =>
        await File.ReadAllTextAsync(ExtractWhatIfReportPath(output));

    private static string ExtractWhatIfReportPath(string output)
    {
        const string prefix = "WhatIf report:";
        var line = output.Split('\n').Select(item => item.Trim()).First(item => item.StartsWith(prefix, StringComparison.Ordinal));
        return line[prefix.Length..].Trim();
    }

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Sha256Normalized(string content)
    {
        var normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
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

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string>? environment = null)
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

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                process.StartInfo.Environment[pair.Key] = pair.Value;
            }
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
