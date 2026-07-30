using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3CBaselineTests
{
    [Fact]
    public void Phase3CGateShell_DocumentsBoundaryLock()
    {
        var repoRoot = GetRepoRoot();
        var script = File.ReadAllText(Path.Combine(repoRoot, "scripts", "qa", "run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1"));

        Assert.Contains("run Phase 3B baseline gate", script, StringComparison.Ordinal);
        Assert.Contains("analysis/agent-handoff", script, StringComparison.Ordinal);
        Assert.Contains("agent-handoff-readiness", script, StringComparison.Ordinal);
        Assert.Contains("visual-blueprint\\.v1", script, StringComparison.Ordinal);
        Assert.Contains("BlazorShop\\.Storefront\\.Starter", script, StringComparison.Ordinal);
        Assert.Contains("--blame-hang-timeout", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Phase3CClosureReport_RecordsBaselineAndKnownGaps()
    {
        var repoRoot = GetRepoRoot();
        var report = File.ReadAllText(Path.Combine(repoRoot, "docs", "qa", "phase3c-final-handoff-closure.md"));

        Assert.Contains("Baseline commit SHA:", report, StringComparison.Ordinal);
        Assert.Contains("Existing ReverseEngineering tests: passed", report, StringComparison.Ordinal);
        Assert.Contains("Existing Phase 3B gate: passed", report, StringComparison.Ordinal);
        Assert.Contains("StorefrontBuilder generation does not consume", report, StringComparison.Ordinal);
        Assert.Contains("Phase 3C closes only when", report, StringComparison.Ordinal);
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
