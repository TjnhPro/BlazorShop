using System.Text.RegularExpressions;
using Xunit;

namespace BlazorShop.AI.StorefrontReverseEngineering.Tests;

[Trait("Phase", "4")]
[Trait("Proof", "StorefrontPhase4MvpGateVisualQaContract")]
public sealed class StorefrontPhase4MvpGateVisualQaContractTests
{
    [Fact]
    public void MvpGate_RuntimeClosureRequiresCurrentRuntimeSummaryBinding()
    {
        var script = ReadMvpGateScript();

        Assert.Contains("$visualQaRuntimeSummaryPath", script, StringComparison.Ordinal);
        Assert.Contains("visual-qa-runtime-summary.json", script, StringComparison.Ordinal);
        Assert.Contains("artifactKind must be storefront-builder.visual-qa-runtime-summary", script, StringComparison.Ordinal);
        Assert.Contains("proofMode must be runtime", script, StringComparison.Ordinal);
        Assert.Contains("baseUrl", script, StringComparison.Ordinal);
        Assert.Contains("Runtime evidence operationId mismatch", script, StringComparison.Ordinal);
        Assert.Contains("captures are missing visual-plan coverage", script, StringComparison.Ordinal);
        Assert.Contains("Runtime screenshot is older than visual-qa-runtime-summary.json startedUtc", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MvpGate_RuntimeClosureRejectsSeededOrStaleVisualQaReports()
    {
        var script = ReadMvpGateScript();

        Assert.True(
            script.IndexOf("run visual QA", StringComparison.Ordinal) < script.IndexOf("materialize Reference QA report from current runtime summary", StringComparison.Ordinal),
            "Runtime visual QA must run before Reference QA materialization.");
        Assert.True(
            script.IndexOf("materialize Reference QA report from current runtime summary", StringComparison.Ordinal) < script.IndexOf("validate runtime evidence binding", StringComparison.Ordinal),
            "The materialized report must be bound before final runtime evidence assertions.");
        Assert.Contains("runtimeEvidencePaths must match current summary capture paths", script, StringComparison.Ordinal);
        Assert.Contains("viewportCaptures screenshot is not one of the current summary capture paths", script, StringComparison.Ordinal);
        Assert.Contains("referenceEvidencePaths references missing evidence", script, StringComparison.Ordinal);
        Assert.DoesNotContain("visual-artifacts\\visual-qa-report.json", script, StringComparison.Ordinal);
        Assert.DoesNotContain("visual-artifacts/visual-qa-report.json", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MvpGate_RuntimeClosureRejectsBadQaDecisionsAndPlaceholderHashes()
    {
        var script = ReadMvpGateScript();

        Assert.Contains("unaccepted critical/major counters are nonzero", script, StringComparison.Ordinal);
        Assert.Contains("has unaccepted critical/major issues", script, StringComparison.Ordinal);
        Assert.Contains("passed=true and finalDecision='passed'", script, StringComparison.Ordinal);
        Assert.Contains("contains placeholder hash text", script, StringComparison.Ordinal);
        Assert.Contains("checkpoint-auto-detect", script, StringComparison.Ordinal);
        Assert.Contains("does not match current source file hash", script, StringComparison.Ordinal);
        Assert.Contains("generationMode must be handoff-project-skeleton", script, StringComparison.Ordinal);
        Assert.Contains("generationMode must be handoff", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MvpGate_SkeletonProofRemainsNonReleaseFeedbackOnly()
    {
        var script = ReadMvpGateScript();

        Assert.Contains("Skeleton is for early fixture proof only", script, StringComparison.Ordinal);
        Assert.Contains("Closure requires Runtime", script, StringComparison.Ordinal);
        Assert.Contains("if ($effectiveProofMode -eq \"Runtime\")", script, StringComparison.Ordinal);
        Assert.Contains("if ($SkeletonProof -and $effectiveProofMode -ne \"Skeleton\")", script, StringComparison.Ordinal);
        Assert.Contains("SkeletonProof mode does not prove the closure artifact chain.", script, StringComparison.Ordinal);
        Assert.Contains("This mode is only for early generated skeleton feedback, not release closure.", script, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalClosureGate_RunsNoSkipEvidenceChainInOrder()
    {
        var script = ReadFinalGateScript();
        var orderedMarkers = new[]
        {
            "clean working tree at start",
            "capture tested HEAD",
            "visual workspace static checks",
            "validate visual schema examples",
            "validate tracked Phase 4.11 closure fixture",
            "run StorefrontBuilder handoff preflight",
            "prepare fresh generated pilot output",
            "generate fresh Phase 4.11 pilot from tracked portable handoff fixture",
            "assert generated handoff metadata and task package",
            "apply deterministic final closure visual edit",
            "run automatic pilot changed-file detection",
            "restore generated pilot before runtime visual QA",
            "build generated pilot before runtime visual QA",
            "start runtime Commerce fixture if needed",
            "start generated pilot runtime host",
            "run runtime visual QA for current closure operation",
            "materialize Reference QA from current runtime evidence",
            "run Phase 4 MVP pilot gate",
            "run StorefrontBuilder generated fast functional proof",
            "run StorefrontBuilder regeneration ownership gate",
            "final HEAD and clean tree check",
            "cleanup disposable generated pilot output"
        };

        var previous = -1;
        foreach (var marker in orderedMarkers)
        {
            var index = script.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(index > previous, $"Expected '{marker}' after prior gate marker.");
            previous = index;
        }
    }

    [Fact]
    public void FinalClosureGate_RecordsRequiredEvidenceFieldsAndNoManualSeededArtifacts()
    {
        var script = ReadFinalGateScript();

        foreach (var field in new[]
        {
            "testedHead",
            "finalHead",
            "closureFixtureRoot",
            "handoffSchemaRoot",
            "handoffPreflightReportPath",
            "pilotGeneratedProjectRoot",
            "generatedMetadataPath",
            "generationPlanPath",
            "generationPlanHash",
            "taskPackagePath",
            "taskPackageHash",
            "checkpointPath",
            "checkpointHash",
            "implementationReportPath",
            "agentWrittenFilesPath",
            "runtimeSummaryPath",
            "screenshotRoot",
            "materializedQaReportPath",
            "mvpGateReportPath",
            "functionalProofReportPath",
            "regenerationGateReportPath",
            "finalDecision"
        })
        {
            Assert.Contains(field, script, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Write-PilotAgentTaskPackage", script, StringComparison.Ordinal);
        Assert.DoesNotContain("plan-generation-files.mjs", script, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"visual-artifacts[\\/]+visual-qa-report\.json", RegexOptions.IgnoreCase), script);
    }

    [Fact]
    public void FinalClosureGate_BypassesCannotSkipMandatoryClosureProof()
    {
        var script = ReadFinalGateScript();

        Assert.Contains("SkipFullFixtureProof", script, StringComparison.Ordinal);
        Assert.Contains("KeepGeneratedPilot", script, StringComparison.Ordinal);
        foreach (var forbidden in new[]
        {
            "SkipHandoffPreflight",
            "SkipRuntimeVisualQa",
            "SkipMaterializedQaReport",
            "SkipMvpGate",
            "SkipFastFunctionalProof",
            "SkipRegenerationGate",
            "AllowDirtyTree"
        })
        {
            Assert.DoesNotContain(forbidden, script, StringComparison.Ordinal);
        }
    }

    private static string ReadMvpGateScript() =>
        File.ReadAllText(Path.Combine(GetRepoRoot(), "scripts", "qa", "run-storefront-phase4-mvp-gate.ps1"));

    private static string ReadFinalGateScript() =>
        File.ReadAllText(Path.Combine(GetRepoRoot(), "scripts", "qa", "run-storefront-phase4-final-closure-gate.ps1"));

    private static string GetRepoRoot() => Phase3DNegativeReviewMutationTests.GetRepoRoot();
}
