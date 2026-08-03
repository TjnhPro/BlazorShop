namespace BlazorShop.Tests.Architecture
{
    using Xunit;

    public sealed class StorefrontBuilderFinalClosureEvidenceTruthTests
    {
        [Fact]
        public void FinalClosureFixture_MarkerOnlyPortableHandoffFailsPreflight()
        {
            var fixtureRoot = RepositoryPath("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/phase4-11-closure/portable-handoff");

            Assert.True(
                File.Exists(Path.Combine(fixtureRoot, "analysis", "agent-handoff", "manifest.json")),
                "Problem: marker-only portable-handoff/README.md can be mistaken for closure evidence. Cause: the tracked final fixture lacks analysis/agent-handoff/manifest.json. Fix: replace the marker with a valid portable handoff package.");
        }

        [Fact]
        public void FinalClosureGate_RequiresHandoffRootAndSchemaForGeneration()
        {
            var script = ReadRepositoryFile("scripts/qa/run-storefront-phase4-final-closure-gate.ps1");

            Assert.Contains("HandoffSchemaRoot", script, StringComparison.Ordinal);
            Assert.Contains("-HandoffRoot", script, StringComparison.Ordinal);
            Assert.Contains("-HandoffSchemaRoot", script, StringComparison.Ordinal);
            Assert.Contains("preflight-only", script, StringComparison.Ordinal);
        }

        [Fact]
        public void FinalClosureGate_RejectsStaticGenerationPlanAndManualTaskPackage()
        {
            var script = ReadRepositoryFile("scripts/qa/run-storefront-phase4-final-closure-gate.ps1");

            Assert.Contains("handoff-project-skeleton", script, StringComparison.Ordinal);
            Assert.Contains("generationMode", script, StringComparison.Ordinal);
            Assert.Contains("agent-visual-task-package", script, StringComparison.Ordinal);
            Assert.DoesNotContain("Write-PilotAgentTaskPackage", script, StringComparison.Ordinal);
            Assert.DoesNotContain("plan-generation-files.mjs", script, StringComparison.Ordinal);
        }

        [Fact]
        public void Recorder_VerifiesCheckpointPostHashesAgainstCurrentSource()
        {
            var script = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs");

            Assert.Contains("post hash differs from current file content", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SFB-AGENT-WRITE-029", script, StringComparison.Ordinal);
            Assert.Contains("implementation report before/after hashes differ from checkpoint hashes", script, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("placeholder hash", script, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MvpGate_BindsReferenceQaReportToCurrentRuntimeSummaryAndScreenshots()
        {
            var script = ReadRepositoryFile("scripts/qa/run-storefront-phase4-mvp-gate.ps1");

            foreach (var required in new[]
            {
                "visual-qa-runtime-summary.json",
                "artifactKind",
                "storefront-builder.visual-qa-runtime-summary",
                "proofMode",
                "runtime",
                "BaseUrl",
                "operationId",
                "screenshot",
                "runtimeEvidencePaths",
                "referenceEvidencePaths",
                "current summary capture paths",
            })
            {
                Assert.Contains(required, script, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void RuntimeVisualQaSummary_IncludesOperationAndTimestampEvidence()
        {
            var script = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs");

            Assert.Contains("--operation-id", script, StringComparison.Ordinal);
            Assert.Contains("operationId", script, StringComparison.Ordinal);
            Assert.Contains("startedUtc", script, StringComparison.Ordinal);
            Assert.Contains("finishedUtc", script, StringComparison.Ordinal);
            Assert.Contains("proofMode: \"runtime\"", script, StringComparison.Ordinal);
        }

        [Fact]
        public void ReferenceQaReport_IsMaterializedAfterRuntimeVisualQa()
        {
            var finalGate = ReadRepositoryFile("scripts/qa/run-storefront-phase4-final-closure-gate.ps1");

            Assert.Contains("materialize-reference-visual-qa-report.mjs", finalGate, StringComparison.Ordinal);
            Assert.DoesNotContain("visual-artifacts\\visual-qa-report.json", finalGate, StringComparison.Ordinal);
            Assert.DoesNotContain("visual-artifacts/visual-qa-report.json", finalGate, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }
}
