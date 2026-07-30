using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3BGateScriptTests
{
    [Fact]
    public void Phase3BGateScript_DefinesRequiredChecksAndReportFields()
    {
        var script = File.ReadAllText(Path.Combine(GetRepoRoot(), "scripts", "qa", "run-storefront-reverse-engineering-phase3b-gate.ps1"));

        Assert.Contains("dotnet", script, StringComparison.Ordinal);
        Assert.Contains("build", script, StringComparison.Ordinal);
        Assert.Contains("run Phase 3A regression fast subset", script, StringComparison.Ordinal);
        Assert.Contains("run all ReverseEngineering tests", script, StringComparison.Ordinal);
        Assert.Contains("--blame-hang-timeout", script, StringComparison.Ordinal);
        Assert.Contains("phase3b-home.html", script, StringComparison.Ordinal);
        Assert.Contains("phase3b-plp.html", script, StringComparison.Ordinal);
        Assert.Contains("phase3b-pdp.html", script, StringComparison.Ordinal);
        Assert.Contains("phase3b-unsupported.html", script, StringComparison.Ordinal);
        Assert.Contains("StorefrontBuilder plan-only smoke", script, StringComparison.Ordinal);
        Assert.Contains("visual-blueprint\\.v1", script, StringComparison.Ordinal);
        Assert.Contains("no Razor/CSS generation code", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Commit SHA:", script, StringComparison.Ordinal);
        Assert.Contains("Presentation catalog version:", script, StringComparison.Ordinal);
        Assert.Contains("Unsupported pattern count:", script, StringComparison.Ordinal);
        Assert.Contains("Review queue count:", script, StringComparison.Ordinal);
        Assert.Contains("Known limitations:", script, StringComparison.Ordinal);
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
