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
            var projectValidator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1");
            var fixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/bad-static-project/Pages/Duplicate.razor");

            foreach (var marker in new[]
            {
                "Test-StorefrontBuilderSchemas.ps1",
                "Test-StorefrontBuilderGeneratedProject.ps1",
                "Test-StorefrontBuilderAssets.ps1",
                "Test-StorefrontBuilderGuard.ps1",
                "Test-StorefrontBuilderIdempotency.ps1",
                "generated-files.yaml",
                "Generated storefront visual files must not declare @page routes",
                "Register Presentation view slots",
                "Package compatibility metadata",
                "PackageReference",
                "storefront-builder.functional.js",
                "wwwroot/js/visual",
                "Generated visual JS must not invoke application commands",
                "Generated visual source must not own browser transport",
                "Generated bootstrap files may only compose Storefront application and view registrations",
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
            Assert.Contains("SFB-STATIC-005", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-006", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-007", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-008", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-STATIC-009", validator, StringComparison.Ordinal);
            Assert.Contains("canonicalContractPath", projectValidator, StringComparison.Ordinal);
            Assert.Contains("contracts/storefront/storefront.openapi.json", projectValidator, StringComparison.Ordinal);
            Assert.Contains("storefrontContractSha256:", projectValidator, StringComparison.Ordinal);
            Assert.Contains("[a-f0-9]{64}", projectValidator, StringComparison.Ordinal);
            Assert.Contains("SFB-PROJECT-007", projectValidator, StringComparison.Ordinal);
        }

        [Fact]
        public void GeneratedMetadata_RecordsCanonicalStorefrontContractIdentity()
        {
            var projectGenerator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1");
            var metadataSchema = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/schemas/metadata.schema.json");
            var validFixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/schemas/fixtures/valid/metadata.json");

            foreach (var marker in new[]
            {
                "contracts/storefront/storefront.openapi.json",
                "Get-FileHash",
                "Algorithm SHA256",
                "storefrontContractPath:",
                "storefrontContractSha256:",
                "generatorVersion:",
                "createdUtc:",
                "updatedUtc:",
                "sourceStarterVersion:",
                "starterContractVersion:",
                "packageVersions:",
                "commandMode:",
                "normalizedProjectName:",
                "outputRoot:",
            })
            {
                Assert.Contains(marker, projectGenerator, StringComparison.Ordinal);
            }

            Assert.Contains("\"storefrontContractPath\"", metadataSchema, StringComparison.Ordinal);
            Assert.Contains("\"storefrontContractSha256\"", metadataSchema, StringComparison.Ordinal);
            Assert.Contains("\"updatedUtc\"", metadataSchema, StringComparison.Ordinal);
            Assert.Contains("\"pattern\": \"^[a-f0-9]{64}$\"", metadataSchema, StringComparison.Ordinal);
            Assert.Contains("\"storefrontContractPath\": \"contracts/storefront/storefront.openapi.json\"", validFixture, StringComparison.Ordinal);
            Assert.Contains("\"storefrontContractSha256\": \"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"", validFixture, StringComparison.Ordinal);
            Assert.Contains("\"updatedUtc\": \"2026-07-24T00:00:00Z\"", validFixture, StringComparison.Ordinal);
            Assert.Contains("\"starterContractVersion\": \"1\"", validFixture, StringComparison.Ordinal);
            Assert.Contains("\"packageVersions\"", validFixture, StringComparison.Ordinal);
        }

        [Fact]
        public void CreateGeneratorHardening_UsesNormalizationStagingAndAtomicReplacement()
        {
            var helper = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/StorefrontBuilderProjectSafety.ps1");
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1");
            var command = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1");
            var negativeTests = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/Test-StorefrontBuilderCreateHardening.ps1");

            foreach (var marker in new[]
            {
                "Normalize-StorefrontProjectName",
                "Normalize-StorefrontStoreKey",
                "Resolve-ApprovedStorefrontBuilderOutputRoot",
                "Assert-StorefrontBuilderPathUnderRoot",
                "Remove-StorefrontBuilderPath",
                "artifacts\\storefront-builder\\generated",
                "obj\\storefront-builder\\generated",
                "SFB-PROJECT-001",
                "SFB-PROJECT-002",
                "SFB-PROJECT-010",
            })
            {
                Assert.Contains(marker, helper, StringComparison.Ordinal);
            }

            foreach (var marker in new[]
            {
                ".staging",
                ".replace-backup",
                "Move-Item -LiteralPath $projectRoot -Destination $backupProjectRoot",
                "Move-Item -LiteralPath $backupProjectRoot -Destination $projectRoot",
                "Move-Item -LiteralPath $stagedProjectRoot -Destination $projectRoot",
                "Test-StorefrontBuilderGeneratedProject.ps1",
                "SFB-PROJECT-011",
                "generatorVersion:",
                "createdUtc:",
                "updatedUtc:",
                "commandMode:",
                "normalizedProjectName:",
            })
            {
                Assert.Contains(marker, generator, StringComparison.Ordinal);
            }

            Assert.Contains("-CommandMode generate", command, StringComparison.Ordinal);
            Assert.Contains("-CommandMode full", command, StringComparison.Ordinal);
            Assert.Contains("Normalize-StorefrontProjectName", command, StringComparison.Ordinal);
            Assert.Contains("Normalize-StorefrontStoreKey", command, StringComparison.Ordinal);

            foreach (var marker in new[]
            {
                "DemoExisting",
                "DemoPlan",
                "plan-only",
                "SFB-PROJECT-001",
                "SFB-PROJECT-002",
                "SFB-PROJECT-010",
                "SFB-PROJECT-011",
            })
            {
                Assert.Contains(marker, negativeTests, StringComparison.Ordinal);
            }
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
                "BlazorShop.Storefront.Components",
                "PackageReference",
                "BlazorShop.Storefront.V2",
                "BlazorShop.Web.SharedV2",
                "Web.SharedV2",
                "BlazorShop.Application",
                "BlazorShop.Domain",
                "BlazorShop.Infrastructure",
                "BlazorShop.CommerceNode.API",
                "BlazorShop.ControlPlane.API",
                "StorefrontClientPackageVersion",
                "StorefrontRuntimePackageVersion",
                "StorefrontPresentationPackageVersion",
                "StorefrontComponentsPackageVersion",
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
            var proof = ReadRepositoryFile("scripts/qa/run-storefront-builder-generated-proof.ps1");

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
                "StorefrontBuilder Visual Smoke QA Report",
                "No readable stylesheet rules are applied",
                "Visual fidelity result: not implemented",
            })
            {
                Assert.Contains(marker, runner, StringComparison.Ordinal);
            }

            Assert.Contains("visual-qa-report.md", runner, StringComparison.Ordinal);
            Assert.Contains("RunBrowserQa", proof, StringComparison.Ordinal);
            Assert.Contains("run-visual-qa.mjs", proof, StringComparison.Ordinal);
        }

        [Fact]
        public void CommerceRegressionGate_CoversStarterFlowsAndRejectsDirectCommerceCalls()
        {
            var runner = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-commerce-regression.mjs");
            var proof = ReadRepositoryFile("scripts/qa/run-storefront-builder-generated-proof.ps1");

            foreach (var marker in new[]
            {
                "Home renders",
                "Home renders with header",
                "Home renders with footer",
                "Catalog renders",
                "Product renders",
                "Category link navigates",
                "Product link navigates",
                "Product gallery or image area renders",
                "Product quantity control renders",
                "Product selection preview runs when available",
                "Add-to-cart succeeds through same-origin BFF",
                "Cart badge updates",
                "Cart page renders",
                "Checkout entry route loads or redirects according to auth/cart state",
                "Account link route loads or redirects according to auth state",
                "Consent accept/revoke path works",
                "Home SEO title/meta exists",
                "Product SEO title/meta exists",
                "Content page SEO title/meta exists",
                "Missing slug/not-found route renders visual not-found state",
                "Browser does not call Commerce Node protected APIs directly",
                "/api/storefront/",
                "/api/commerce/",
                "[data-storefront-product-purchase]",
                "[data-storefront-product-purchase-submit]",
                "[data-storefront-purchase-quantity]",
            })
            {
                Assert.Contains(marker, runner, StringComparison.Ordinal);
            }

            Assert.Contains("functional-commerce-report.md", runner, StringComparison.Ordinal);
            Assert.Contains("Functional Foundation Browser Report", runner, StringComparison.Ordinal);
            Assert.Contains("same-origin Presentation BFF", runner, StringComparison.Ordinal);
            Assert.DoesNotContain("explicit fixture gap is reported", runner, StringComparison.Ordinal);
            Assert.DoesNotContain("[data-storefront-generated-add-to-cart]", runner, StringComparison.Ordinal);
            Assert.DoesNotContain("window.blazorShopStorefront?.application", runner, StringComparison.Ordinal);
            Assert.Contains("run-commerce-regression.mjs", proof, StringComparison.Ordinal);
            Assert.Contains("FixtureCategorySlug", proof, StringComparison.Ordinal);
            Assert.Contains("FixtureProductSlug", proof, StringComparison.Ordinal);
        }

        [Fact]
        public void IdempotentRegeneration_TracksHashesCommandsAndManualEditConflicts()
        {
            var command = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1");
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/update-generated-files-manifest.mjs");
            var helper = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/generated-file-manifest.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderIdempotency.ps1");
            var fixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manual-edit-conflict/generated-files.yaml");
            var regenerationSafety = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/Test-StorefrontBuilderRegenerationSafety.ps1");

            foreach (var marker in new[]
            {
                "filePath",
                "ownership",
                "capability",
                "scope",
                "generatorVersion",
                "sourceArtifactIds",
                "sourceSpecHash",
                "generatedHash",
                "currentHash",
                "lastGeneratedTimestamp",
                "manualEditDetected",
                "conflictStatus",
                "conflictReason",
                "protected",
                "obsolete",
                "templateVersion",
            })
            {
                Assert.Contains(marker, helper, StringComparison.Ordinal);
                Assert.Contains(marker, validator, StringComparison.Ordinal);
            }

            foreach (var marker in new[]
            {
                "scanProjectFiles",
                "readPreviousManifest",
                "buildManifestEntries",
                "writeManifestYaml",
                "buildRegenerationReport",
                "replace(/\\r\\n/g, \"\\n\")",
                "ownershipValues",
                "\"generated\"",
                "\"managed\"",
                "\"user-owned\"",
                "\"protected\"",
                "\"artifact-only\"",
                "manual-edit",
                "user-owned-modified",
                "protected-modified",
                "missing",
                "obsolete",
            })
            {
                Assert.Contains(marker, helper, StringComparison.Ordinal);
            }

            Assert.Contains("generated-file-manifest.mjs", generator, StringComparison.Ordinal);

            foreach (var scope in new[] { "all", "page", "component", "css", "validate", "conflicts" })
            {
                Assert.Contains(scope, command, StringComparison.Ordinal);
            }

            foreach (var marker in new[]
            {
                "New-RegenerationPlan",
                "Copy-ChangedFile",
                "Write-RegenerationReport",
                "skip unchanged",
                "skip user-owned",
                "skip protected",
                "conflict manual edit",
                "obsolete candidate",
                "delete only if explicitly allowed",
                ".regeneration-staging",
                ".regeneration-backup",
                "ValidateAfterApply",
                "BuildAfterApply",
                "Copy-Item -LiteralPath $backupRoot -Destination $resolvedProjectRoot",
                "WhatIf completed without writing generated project files.",
            })
            {
                Assert.Contains(marker, command, StringComparison.Ordinal);
            }

            Assert.Contains("manualEditDetected: true", fixture, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-002", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-003", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-005", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-006", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-007", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-008", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-009", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-010", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-IDEMPOTENCY-011", validator, StringComparison.Ordinal);

            foreach (var marker in new[]
            {
                "WhatIf wrote files",
                "No-op regeneration produced file diffs",
                "CSS scope touched unrelated files",
                "Page scope touched unrelated files",
                "Component scope touched unrelated files",
                "Manual Razor edit was not reported as conflict",
                "User-owned custom file was overwritten",
                "SFB-IDEMPOTENCY-002",
                "Rollback restore path is missing",
            })
            {
                Assert.Contains(marker, regenerationSafety, StringComparison.Ordinal);
            }

            foreach (var fixturePath in new[]
            {
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/unchanged-generated-file/generated-files.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/manually-edited-generated-file/generated-files.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/manually-edited-user-owned-file/generated-files.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/protected-file-modified/generated-files.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/missing-generated-file/generated-files.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/obsolete-generated-file/generated-files.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/duplicate-manifest-entry/generated-files.yaml",
                "tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/manifest-cases/manifest-path-traversal/generated-files.yaml",
            })
            {
                var manifestCase = ReadRepositoryFile(fixturePath);
                Assert.Contains("filePath:", manifestCase, StringComparison.Ordinal);
                Assert.Contains("currentHash:", manifestCase, StringComparison.Ordinal);
                Assert.Contains("conflictReason:", manifestCase, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void HumanReviewWorkflow_ProvidesModesAndDecisionArtifacts()
        {
            var command = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1");
            var writer = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-review-artifacts.mjs");
            var proof = ReadRepositoryFile("scripts/qa/run-storefront-builder-generated-proof.ps1");

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
            }

            Assert.Contains("write-review-artifacts.mjs", proof, StringComparison.Ordinal);
            Assert.Contains("artifacts/storefront-builder/generated", command, StringComparison.Ordinal);
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

        [Fact]
        public void CiReleaseGate_ProtectsFastChecksAndKeepsExpensiveBrowserRunsManualOrNightly()
        {
            var workflow = ReadRepositoryFile(".github/workflows/storefront-builder.yml");

            foreach (var marker in new[]
            {
                "Schema tests",
                "Preflight tests",
                "Protected file guard tests",
                "Generation fixture tests",
                "Idempotency tests",
                "Regeneration ownership gate",
                "Isolation gate describe mode",
                "Generated proof structure gate",
                "Generated proof fast foundation functional browser gate",
                "Generated proof full foundation functional browser gate",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/**",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/**",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/**",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Components/**",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/**",
                "scripts/qa/run-storefront-builder-*.ps1",
                "workflow_dispatch",
                "schedule",
                "Full external reference-site capture",
                "Full visual diff against live target",
                "Full payment/order browser regression",
            })
            {
                Assert.Contains(marker, workflow, StringComparison.Ordinal);
            }

            Assert.Contains("run_browser_gates", workflow, StringComparison.Ordinal);
            Assert.Contains("run-storefront-builder-regeneration-gate.ps1", workflow, StringComparison.Ordinal);
            Assert.Contains("-ProofLevel Structure", workflow, StringComparison.Ordinal);
            Assert.Contains("-ProofLevel FoundationFunctionalFast", workflow, StringComparison.Ordinal);
            Assert.Contains("-ProofLevel FoundationFunctionalFull", workflow, StringComparison.Ordinal);
        }

        [Fact]
        public void MvpPocReport_ProvesGeneratedStorefrontAndDeferredScope()
        {
            var proof = ReadRepositoryFile("scripts/qa/run-storefront-builder-generated-proof.ps1");
            var plan = ReadRepositoryFile("docs/visual-reverse-engineering-skill/04-StorefrontBuilder-Generated-Store-Cleanup.todo.md");

            foreach (var marker in new[]
            {
                "Generate proof storefront",
                "Write StorefrontBuilder artifacts",
                "Restore generated proof",
                "Build generated proof",
                "Run static StorefrontBuilder validation",
                "Run StorefrontBuilder isolation gate",
                "Run shared visual consumer boundary validator",
                "Run post-regeneration build proof",
                "Run regenerate no-op proof",
                "Run manual-edit conflict fixture proof",
                "RunBrowserQa",
                "ProofLevel",
                "FoundationFunctionalFast",
                "FoundationFunctionalFull",
            })
            {
                Assert.Contains(marker, proof, StringComparison.Ordinal);
            }

            Assert.Contains("StorefrontBuilder generated proof completed", proof, StringComparison.Ordinal);
            Assert.Contains("Keep true visual generation improvements for the later StorefrontBuilder correction phases", plan, StringComparison.Ordinal);
        }

        [Fact]
        public void F1_52_GeneratedProof_SplitsStructureAndFoundationFunctionalProof()
        {
            var proof = ReadRepositoryFile("scripts/qa/run-storefront-builder-generated-proof.ps1");
            var composition = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs");
            var starterLayout = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Layout/MainLayout.razor");
            var starterProductPage = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/ProductPage.razor");
            var starterProductShell = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Catalog/ProductDetailShell.razor");
            var starterPurchasePanel = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Components/Catalog/PurchasePanelPlaceholder.razor");
            var workflow = ReadRepositoryFile(".github/workflows/ci.yml");

            foreach (var marker in new[]
            {
                "[ValidateSet(\"Structure\", \"FoundationFunctionalFast\", \"FoundationFunctionalFull\", \"FoundationFunctional\")]",
                "Assert-StorefrontFixtureData",
                "SFB-PROOF-FIXTURE-003",
                "SFB-PROOF-FIXTURE-006",
                "SFB-PROOF-FIXTURE-007",
                "SFB-PROOF-FIXTURE-009",
                "run-fast-foundation-functional.mjs",
                "Run shared visual consumer boundary validator",
                "Invoke-GeneratedProofRegenerationLifecycle",
                "SFB-PROOF-REGEN-001",
                "SFB-PROOF-REGEN-002",
                "StorefrontVisualConsumerBoundaryValidatorTests.F1_51_SharedValidator_PassesGeneratedProofWhenPresent",
            })
            {
                Assert.Contains(marker, proof, StringComparison.Ordinal);
            }

            foreach (var marker in new[]
            {
                "data-storefront-cart-badge",
                "Context.Search.Categories",
                "sfb-product-purchase",
            })
            {
                Assert.Contains(marker, composition, StringComparison.Ordinal);
            }

            foreach (var marker in new[]
            {
                "data-storefront-product-purchase",
                "data-selection-preview-route",
                "data-storefront-command=\"cart.add-line\"",
                "data-storefront-product-purchase-submit",
                "data-storefront-purchase-quantity",
                "data-storefront-purchase-feedback",
                "PurchasePanel=\"@Context.PurchasePanel\"",
                "PurchaseActions=\"@Context.PurchaseActions\"",
            })
            {
                Assert.Contains(marker, starterProductPage + starterProductShell + starterPurchasePanel, StringComparison.Ordinal);
            }

            Assert.Contains("content.replace", composition, StringComparison.Ordinal);
            Assert.Contains("<span data-storefront-cart-badge hidden>0</span>", starterLayout, StringComparison.Ordinal);
            Assert.Contains("sfb-cart-badge", composition, StringComparison.Ordinal);
            Assert.DoesNotContain("@Context.Links.Cart.Label</a>', '<a class=\"sfb-cart-badge\"", composition, StringComparison.Ordinal);
            Assert.DoesNotContain("PurchasePanel=\"@Context.PurchasePanel\"", composition, StringComparison.Ordinal);
            Assert.DoesNotContain("PurchaseActions=\"@Context.PurchaseActions\"", composition, StringComparison.Ordinal);
            Assert.DoesNotContain("ProductPurchasePanelModel.Empty", composition, StringComparison.Ordinal);
            Assert.DoesNotContain("writeFunctionalBrowserBridge", composition, StringComparison.Ordinal);
            Assert.DoesNotContain("data-storefront-generated-add-to-cart", composition, StringComparison.Ordinal);
            Assert.DoesNotContain("app.cart.addLine", composition, StringComparison.Ordinal);
            Assert.Contains("StorefrontBuilder generated proof structure gate", workflow, StringComparison.Ordinal);
            Assert.Contains("StorefrontBuilder generated proof fast foundation functional gate", workflow, StringComparison.Ordinal);
            Assert.Contains("StorefrontBuilder regeneration ownership gate", workflow, StringComparison.Ordinal);
            Assert.Contains("StorefrontVisualConsumerBoundaryValidatorTests", workflow, StringComparison.Ordinal);
            Assert.Contains("StorefrontStarterHostSmokeTests", workflow, StringComparison.Ordinal);
            Assert.Contains("StorefrontApplicationBootstrapTests", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontHostCompositionTests", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("$runFullFunctionalProof", proof, StringComparison.Ordinal);
        }

        [Fact]
        public void F1_62_FastFoundationFunctionalProof_UsesActualGeneratedHostAndStaticAssets()
        {
            var runner = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-fast-foundation-functional.mjs");

            foreach (var marker in new[]
            {
                "startGeneratedHost",
                "startFakeCommerceNode",
                "Presentation core script loads through static web assets",
                "actual generated Razor emitted purchase descriptors",
                "_content/BlazorShop.Storefront.Presentation/js/storefront.application.js",
                "Mock mode: generated ASP.NET host renders Razor/static web assets",
            })
            {
                Assert.Contains(marker, runner, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("function pageHtml", runner, StringComparison.Ordinal);
            Assert.DoesNotContain("function htmlShell", runner, StringComparison.Ordinal);
            Assert.DoesNotContain("resourceType() === \"document\"", runner, StringComparison.Ordinal);
            Assert.DoesNotContain("page.addScriptTag", runner, StringComparison.Ordinal);
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
