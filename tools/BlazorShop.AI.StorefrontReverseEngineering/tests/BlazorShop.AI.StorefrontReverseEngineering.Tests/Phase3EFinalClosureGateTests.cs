using System.Text.RegularExpressions;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

public sealed class Phase3EFinalClosureGateTests
{
    [Fact]
    public void Phase3EFinalClosureGate_IsNoSkipCleanHeadGate()
    {
        var script = ReadScript();

        Assert.Contains("[int]$CommandTimeoutSeconds", script, StringComparison.Ordinal);
        Assert.DoesNotContain("SkipPhase3AGate", script, StringComparison.Ordinal);
        Assert.DoesNotContain("SkipPhase3BGate", script, StringComparison.Ordinal);
        Assert.DoesNotContain("SkipPhase3CGate", script, StringComparison.Ordinal);
        Assert.DoesNotContain("SkipPhase3DGate", script, StringComparison.Ordinal);
        Assert.DoesNotContain("SkipPortableProof", script, StringComparison.Ordinal);
        Assert.DoesNotContain("SkipStorefrontBuilderSmoke", script, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowDirtyTree", script, StringComparison.Ordinal);
        Assert.Contains("Assert-SreCleanWorkingTree", script, StringComparison.Ordinal);
        Assert.Contains("Assert-SreHeadUnchanged", script, StringComparison.Ordinal);
        Assert.Contains("Invoke-SreRestore -Context $context", script, StringComparison.Ordinal);
        Assert.Contains("Invoke-SreBuild -Context $context", script, StringComparison.Ordinal);
        Assert.Contains("\"test\", $Context.TestProject, \"--no-build\", \"--no-restore\"", script, StringComparison.Ordinal);
        Assert.Contains("ToolDll", script, StringComparison.Ordinal);
        Assert.DoesNotContain("\"run\", \"--project\", $Context.ToolProject", script, StringComparison.Ordinal);
        Assert.Contains("Tested commit SHA", script, StringComparison.Ordinal);
        Assert.Contains("Final HEAD SHA", script, StringComparison.Ordinal);
        Assert.Contains("final HEAD check", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Phase3EFinalClosureGate_DoesNotInvokeNestedPhase3DGate()
    {
        var script = ReadScript();

        Assert.Empty(Regex.Matches(script, "run-storefront-reverse-engineering-phase3d-final-closure-gate\\.ps1"));
        Assert.DoesNotContain("run-storefront-reverse-engineering-phase3a-gate.ps1", script, StringComparison.Ordinal);
        Assert.DoesNotContain("run-storefront-reverse-engineering-phase3b-gate.ps1", script, StringComparison.Ordinal);
        Assert.DoesNotContain("run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1", script, StringComparison.Ordinal);
        Assert.Contains("Phase 3D correctness proof runs directly", script, StringComparison.Ordinal);
        Assert.Contains("Invoke-SrePhase3DProof", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Phase3EFinalClosureGate_RecordsPortableProof()
    {
        var script = ReadScript();

        Assert.Contains("full ReverseEngineering tests", script, StringComparison.Ordinal);
        Assert.Contains("handoff-specific blueprint tests", script, StringComparison.Ordinal);
        Assert.Contains("portable artifact set tests", script, StringComparison.Ordinal);
        Assert.Contains("typed reference containment tests", script, StringComparison.Ordinal);
        Assert.Contains("manifest portability/hash tests", script, StringComparison.Ordinal);
        Assert.Contains("evidence slot provenance tests", script, StringComparison.Ordinal);
        Assert.Contains("portable validator CLI tests", script, StringComparison.Ordinal);
        Assert.Contains("isolated copy proof", script, StringComparison.Ordinal);
        Assert.Contains("Phase 4 dry-run loader proof", script, StringComparison.Ordinal);
        Assert.Contains("negative portability mutation tests", script, StringComparison.Ordinal);
        Assert.Contains("boundary scans", script, StringComparison.Ordinal);
        Assert.Contains("StorefrontBuilder plan-only smoke", script, StringComparison.Ordinal);
        Assert.Contains("final inspect proof", script, StringComparison.Ordinal);
        Assert.Contains("Full test count:", script, StringComparison.Ordinal);
        Assert.Contains("Phase 3D proof result:", script, StringComparison.Ordinal);
        Assert.Contains("Portable package result:", script, StringComparison.Ordinal);
        Assert.Contains("Reference containment result:", script, StringComparison.Ordinal);
        Assert.Contains("Evidence slot provenance result:", script, StringComparison.Ordinal);
        Assert.Contains("Consumer dry-run result:", script, StringComparison.Ordinal);
        Assert.Contains("Negative mutation count:", script, StringComparison.Ordinal);
        Assert.Contains("StorefrontBuilder smoke result:", script, StringComparison.Ordinal);
        Assert.Contains("GitHub Actions status:", script, StringComparison.Ordinal);
        Assert.Contains("Closure decision:", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Phase3EFinalClosureGate_FailsDirtyTree()
    {
        var script = ReadScript();

        Assert.Contains("git status --porcelain", script, StringComparison.Ordinal);
        Assert.Contains("Working tree is dirty", script, StringComparison.Ordinal);
        Assert.Contains("clean tree check", script, StringComparison.Ordinal);
        Assert.Contains("Assert-SreCleanWorkingTree", script, StringComparison.Ordinal);
    }

    private static string ReadScript()
    {
        var root = GetRepoRoot();
        return File.ReadAllText(Path.Combine(root, "scripts", "qa", "run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1")) +
            Environment.NewLine +
            File.ReadAllText(Path.Combine(root, "scripts", "qa", "storefront-reverse-engineering-phase3-proof-steps.ps1"));
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
