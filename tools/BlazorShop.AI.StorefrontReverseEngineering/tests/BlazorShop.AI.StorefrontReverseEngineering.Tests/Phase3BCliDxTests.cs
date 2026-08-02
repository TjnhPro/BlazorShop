using System.Text.Json;
using BlazorShop.AI.StorefrontReverseEngineering.Cli;
using BlazorShop.AI.StorefrontReverseEngineering.Contracts;
using BlazorShop.AI.StorefrontReverseEngineering.Validation;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "3")]
[Trait("Proof", "ClosureProof")]
public sealed class Phase3BCliDxTests
{
    [Fact]
    public async Task CliHelp_ListsPhase3BForceStepValues()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(["--help"], stdout, stderr, CancellationToken.None);

        Assert.Equal(0, exitCode);
        var text = stdout.ToString();
        Assert.Contains("Phase 3B force-step values:", text, StringComparison.Ordinal);
        Assert.Contains("aggregate-evidence", text, StringComparison.Ordinal);
        Assert.Contains("map-presentation-components", text, StringComparison.Ordinal);
        Assert.Contains("assemble-blueprint-v1", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inspect_PrintsPhase3BArtifactLocationsWithoutPlaywright()
    {
        var projectRoot = await InitializeProjectAsync("Phase 3B Inspect Empty");

        var output = await RunInspectAsync(projectRoot);

        Assert.Contains("Phase 3B artifacts:", output, StringComparison.Ordinal);
        Assert.Contains("Evidence snapshot: missing - analysis/evidence-snapshot.json", output, StringComparison.Ordinal);
        Assert.Contains("Tokens: raw=missing (analysis/tokens/raw-design-tokens.json); semantic=missing (analysis/tokens/semantic-tokens.draft.json)", output, StringComparison.Ordinal);
        Assert.Contains("Archetypes: missing - no page artifacts found", output, StringComparison.Ordinal);
        Assert.Contains("Review queue count: unknown (missing; review/review-queue.json)", output, StringComparison.Ordinal);
        Assert.Contains("Generation readiness: missing - reports/generation-readiness.json", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Inspect_InvalidPhase3BState_PrintsProblemCauseFix()
    {
        var projectRoot = await InitializeProjectAsync("Phase 3B Invalid Tokens");
        await WriteReadyPhase3AReportAsync(projectRoot, "phase-3b-invalid-tokens");
        var tokenPath = Path.Combine(projectRoot, "analysis", "tokens", "raw-design-tokens.json");
        Directory.CreateDirectory(Path.GetDirectoryName(tokenPath)!);
        await File.WriteAllTextAsync(tokenPath, "{\"schemaVersion\":\"1.0\",\"artifactKind\":\"wrong-kind\"}");

        var output = await RunInspectAsync(projectRoot);

        Assert.Contains("Phase 3B problem: invalid token schema", output, StringComparison.Ordinal);
        Assert.Contains("Cause: The raw or semantic token artifact is present but does not satisfy the registered schema.", output, StringComparison.Ordinal);
        Assert.Contains("Fix: Regenerate tokens with --force-step extract-raw-tokens or --force-step normalize-semantic-tokens.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Docs_Phase3BCommandsAreCopyPasteValid()
    {
        var repoRoot = GetRepoRoot();
        var docs = string.Join(
            Environment.NewLine,
            File.ReadAllText(Path.Combine(repoRoot, "tools", "BlazorShop.AI.StorefrontReverseEngineering", "README.md")),
            File.ReadAllText(Path.Combine(repoRoot, "docs", "visual-reverse-engineering-skill", "README.md")),
            File.ReadAllText(Path.Combine(repoRoot, "docs", "visual-reverse-engineering-skill", "reference.md")));

        Assert.Contains("dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project", docs, StringComparison.Ordinal);
        Assert.Contains("--force-step aggregate-evidence", docs, StringComparison.Ordinal);
        Assert.Contains("--force-step assemble-blueprint-v1", docs, StringComparison.Ordinal);
        Assert.Contains("StorefrontBuilder does not consume", docs, StringComparison.Ordinal);
    }

    private static async Task<string> InitializeProjectAsync(string name)
    {
        var outputRoot = Path.Combine("obj", "storefront-reverse-engineering", "projects", "phase3b-cli-dx-" + Guid.NewGuid().ToString("N"));
        var service = new Application.VisualProjectService(GetRepoRoot());
        var project = await service.InitializeAsync("https://example.test", name, outputRoot, force: false, CancellationToken.None);
        return project.ArtifactRoot;
    }

    private static async Task WriteReadyPhase3AReportAsync(string projectRoot, string projectId)
    {
        var report = new ReadinessReport(
            "1.0",
            "readiness-report",
            "readiness-" + projectId,
            DateTimeOffset.UtcNow,
            projectId,
            true,
            [],
            []);

        var path = Path.Combine(projectRoot, "reports", "readiness-report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, VisualJson.Options));
    }

    private static async Task<string> RunInspectAsync(string projectRoot)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await CliHost.RunAsync(
            ["inspect", "--project", projectRoot],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Equal("", stderr.ToString());
        return stdout.ToString();
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
