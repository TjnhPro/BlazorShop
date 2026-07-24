namespace BlazorShop.Tests.Architecture
{
    using Xunit;

    public sealed class StorefrontBuilderQaRegenerationTests
    {
        [Fact]
        public void AssetPipeline_RecordsProvenanceAndReplacementPlaceholders()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/build-asset-manifest.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderAssets.ps1");

            foreach (var marker in new[]
            {
                "sourceUrl",
                "checksum",
                "contentType",
                "detectedUsage",
                "normalizedFilename",
                "duplicateOf",
                "allowedToCopy",
                "replacementNeeded",
                "replacementList",
                "makes no production licensing claim",
            })
            {
                Assert.Contains(marker, generator, StringComparison.Ordinal);
                Assert.Contains(marker, validator, StringComparison.Ordinal);
            }

            Assert.Contains("SFB-ASSET-003", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void StaticValidationGate_CoversArtifactsGuardsRoutesAssetsAndPackages()
        {
            var command = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/validate-storefront.ps1");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderStaticGate.ps1");
            var fixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/bad-static-project/Pages/Duplicate.razor");

            foreach (var marker in new[]
            {
                "Test-StorefrontBuilderSchemas.ps1",
                "Test-StorefrontBuilderGeneratedProject.ps1",
                "Test-StorefrontBuilderAssets.ps1",
                "Test-StorefrontBuilderGuard.ps1",
                "generated-files.yaml",
                "Duplicate route",
                "Package compatibility metadata",
                "PackageReference",
            })
            {
                Assert.Contains(marker, validator, StringComparison.Ordinal);
            }

            Assert.Contains("validate-storefront", command, StringComparison.Ordinal);
            Assert.Contains("@page \"/duplicate\"", fixture, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-001", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-002", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-003", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-004", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void BuildIsolationGate_RestoresBuildsPacksAndRejectsForbiddenReferences()
        {
            var gate = ReadRepositoryFile("scripts/qa/run-storefront-builder-isolation-gate.ps1");

            foreach (var marker in new[]
            {
                "dotnet restore",
                "dotnet build",
                "dotnet pack",
                "BlazorShop.Storefront.Client",
                "BlazorShop.Storefront.Runtime",
                "PackageReference",
                "BlazorShop.Storefront.V2",
                "BlazorShop.Application",
                "BlazorShop.Domain",
                "BlazorShop.Infrastructure",
                "BlazorShop.CommerceNode.API",
                "BlazorShop.ControlPlane.API",
                "StorefrontClientPackageVersion",
                "StorefrontRuntimePackageVersion",
                "Describe",
            })
            {
                Assert.Contains(marker, gate, StringComparison.Ordinal);
            }

            Assert.Contains("SFB-ISOLATION-001", gate, StringComparison.Ordinal);
            Assert.Contains("SFB-ISOLATION-002", gate, StringComparison.Ordinal);
            Assert.Contains("SFB-ISOLATION-003", gate, StringComparison.Ordinal);
        }

        [Fact]
        public void VisualQaGate_CapturesCorePagesAcrossViewportsAndReportsSeverity()
        {
            var runner = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs");
            var report = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/docs/storefront-analysis/visual-qa-report.md");

            foreach (var marker in new[]
            {
                "desktop-1440",
                "tablet-768",
                "mobile-390",
                "shell-home",
                "catalog",
                "product",
                "cart",
                "checkout",
                "account",
                "Critical",
                "Major",
                "Minor",
                "output/playwright/storefront-builder-visual-qa",
            })
            {
                Assert.Contains(marker, runner, StringComparison.Ordinal);
                Assert.Contains(marker, report, StringComparison.Ordinal);
            }

            Assert.Contains("Critical: 0", report, StringComparison.Ordinal);
            Assert.Contains("MVP result: pass", report, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceRegressionGate_CoversStarterFlowsAndRejectsDirectCommerceCalls()
        {
            var runner = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-commerce-regression.mjs");
            var report = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/docs/storefront-analysis/functional-commerce-report.md");

            foreach (var marker in new[]
            {
                "Home renders",
                "Catalog renders",
                "Product renders",
                "Product link navigation works",
                "Product image/gallery region renders",
                "Quantity control can change",
                "Add-to-cart command works through same-origin BFF",
                "Cart badge updates",
                "Cart page renders",
                "Checkout route renders",
                "Account route renders",
                "Login/register shell renders according to store policy",
                "Product SEO initial HTML exists",
                "Browser does not call Commerce Node protected APIs directly",
                "/api/storefront/",
                "/api/commerce/",
            })
            {
                Assert.Contains(marker, runner, StringComparison.Ordinal);
            }

            Assert.Contains("No direct Commerce Node browser calls detected", report, StringComparison.Ordinal);
            Assert.Contains("PayPal/Stripe production providers are outside this MVP gate", report, StringComparison.Ordinal);
        }

        [Fact]
        public void IdempotentRegeneration_TracksHashesCommandsAndManualEditConflicts()
        {
            var command = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1");
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/update-generated-files-manifest.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderIdempotency.ps1");
            var fixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manual-edit-conflict/generated-files.yaml");

            foreach (var marker in new[]
            {
                "filePath",
                "ownership",
                "generatorVersion",
                "sourceArtifactIds",
                "sourceSpecHash",
                "generatedHash",
                "lastGeneratedTimestamp",
                "manualEditDetected",
                "conflictStatus",
            })
            {
                Assert.Contains(marker, generator, StringComparison.Ordinal);
                Assert.Contains(marker, validator, StringComparison.Ordinal);
            }

            foreach (var scope in new[] { "all", "page", "component", "css", "validate", "conflicts" })
            {
                Assert.Contains(scope, command, StringComparison.Ordinal);
            }

            Assert.Contains("manualEditDetected: true", fixture, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-002", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-003", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void HumanReviewWorkflow_ProvidesModesAndDecisionArtifacts()
        {
            var command = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1");
            var writer = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-review-artifacts.mjs");
            var summary = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/docs/storefront-analysis/review-summary.md");

            foreach (var mode in new[] { "analyze-only", "plan-only", "generate", "update", "validate-only", "full" })
            {
                Assert.Contains(mode, command, StringComparison.Ordinal);
            }

            foreach (var artifact in new[]
            {
                "Visual Decision Summary",
                "Unsupported Feature List",
                "Hidden Target Feature List",
                "Starter Fallback List",
                "Asset Replacement List",
                "AI Inference Review List",
                "Manual Tuning Checklist",
            })
            {
                Assert.Contains(artifact, writer, StringComparison.Ordinal);
                Assert.Contains(artifact, summary, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void SkillPackaging_DocumentsCommandsOptionsExamplesAndProtectedRules()
        {
            var readme = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/README.md");
            var skill = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/skills/storefront-builder/SKILL.md");
            var snapshot = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/help-snapshot.txt");

            foreach (var command in new[] { "/analyze-storefront <url>", "/map-storefront", "/generate-storefront", "/validate-storefront", "/build-storefront <url>" })
            {
                Assert.Contains(command, readme, StringComparison.Ordinal);
                Assert.Contains(command, skill, StringComparison.Ordinal);
                Assert.Contains(command, snapshot, StringComparison.Ordinal);
            }

            foreach (var option in new[] { "--name", "--store-key", "--starter", "--output-root", "--mode", "--force", "--skip-visual-qa", "--skip-commerce-regression" })
            {
                Assert.Contains(option, readme, StringComparison.Ordinal);
                Assert.Contains(option, skill, StringComparison.Ordinal);
                Assert.Contains(option, snapshot, StringComparison.Ordinal);
            }

            Assert.Contains("Quick Start", readme, StringComparison.Ordinal);
            Assert.Contains("Single reference URL", readme, StringComparison.Ordinal);
            Assert.Contains("Multiple reference URLs", readme, StringComparison.Ordinal);
            Assert.Contains("Troubleshooting", readme, StringComparison.Ordinal);
            Assert.Contains("Protected Files", readme, StringComparison.Ordinal);
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
