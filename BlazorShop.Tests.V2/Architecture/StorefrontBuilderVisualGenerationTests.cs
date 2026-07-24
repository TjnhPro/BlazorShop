namespace BlazorShop.Tests.Architecture
{
    using Xunit;

    public sealed class StorefrontBuilderVisualGenerationTests
    {
        [Fact]
        public void DesignTokenExtraction_UsesComputedStylesAndCoversRequiredTokenGroups()
        {
            var script = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/extract-design-tokens.mjs");
            var fixture = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/computed-styles.sample.json");

            Assert.Contains("computed-styles-first", script, StringComparison.Ordinal);
            Assert.Contains("screenshot", script, StringComparison.OrdinalIgnoreCase);
            foreach (var group in new[]
            {
                "colors",
                "semanticColors",
                "typographyFamilies",
                "fontSizes",
                "fontWeights",
                "lineHeights",
                "spacingScale",
                "containerWidths",
                "breakpoints",
                "borderWidths",
                "borderRadius",
                "shadows",
                "motionDurations",
                "motionEasing",
                "confidence",
                "evidenceIds",
                "inferenceIds",
            })
            {
                Assert.Contains(group, script, StringComparison.Ordinal);
            }

            Assert.Contains("fontFamily", fixture, StringComparison.Ordinal);
            Assert.Contains("backgroundColor", fixture, StringComparison.Ordinal);
        }

        [Fact]
        public void UiPatternInventory_CoversShellCatalogProductControlsAndStates()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/identify-ui-patterns.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderPatterns.ps1");

            foreach (var pattern in new[]
            {
                "header",
                "footer",
                "main-navigation",
                "mobile-navigation",
                "breadcrumb",
                "product-card",
                "category-card",
                "banner-hero-section",
                "product-grid",
                "product-gallery",
                "product-information-block",
                "product-purchase-block",
                "primary-button",
                "secondary-button",
                "icon-button",
                "text-input",
                "search-input",
                "select",
                "checkbox",
                "quantity-control",
                "pagination",
                "empty-state",
                "error-state",
                "loading-state",
            })
            {
                Assert.Contains(pattern, generator, StringComparison.Ordinal);
            }

            foreach (var field in new[] { "evidenceIds", "selectorSamples", "visualProperties", "statesObserved", "responsiveNotes", "targetSlot", "fallbackBehavior" })
            {
                Assert.Contains(field, generator, StringComparison.Ordinal);
                Assert.Contains(field, validator, StringComparison.Ordinal);
            }

            Assert.Contains("SFB-PATTERN-001", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void BehaviorResponsiveModel_ClassifiesInteractionsAndProtectsCommerceState()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/classify-behavior-responsive.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderBehaviorResponsive.ps1");

            foreach (var behaviorClass in new[]
            {
                "CSS-only",
                "Hover-driven",
                "Focus-driven",
                "Click-driven visual-only",
                "Scroll-driven visual-only",
                "Starter-feature-driven",
                "BFF-action-driven",
                "Approved JS interop",
                "Unsupported",
            })
            {
                Assert.Contains(behaviorClass, generator, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(behaviorClass, validator, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var responsiveField in new[]
            {
                "breakpoint",
                "layoutChange",
                "headerNavBehavior",
                "productGridColumns",
                "productDetailMediaActionStacking",
                "footerStacking",
                "stickyFixedElements",
                "drawerMenuBehavior",
            })
            {
                Assert.Contains(responsiveField, generator, StringComparison.Ordinal);
                Assert.Contains(responsiveField, validator, StringComparison.Ordinal);
            }

            Assert.Contains("add-to-cart", generator, StringComparison.Ordinal);
            Assert.Contains("BFF-action-driven", generator, StringComparison.Ordinal);
            Assert.Contains("SFB-BEHAVIOR-003", validator, StringComparison.Ordinal);
            Assert.Contains("direct JS", validator, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("direct HTTP", validator, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PageTopology_MapsRequiredPageRegionsAndStarterSlots()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/build-page-topology.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderTopology.ps1");

            foreach (var topology in new[]
            {
                "global-shell",
                "home-page-sections",
                "catalog-page-regions",
                "search-result-page-regions",
                "product-detail-regions",
                "cart-fallback-style-regions",
                "checkout-fallback-style-regions",
                "account-fallback-style-regions",
                "content-error-system-page-shell",
            })
            {
                Assert.Contains(topology, generator, StringComparison.Ordinal);
                Assert.Contains(topology, validator, StringComparison.Ordinal);
            }

            foreach (var field in new[] { "regionId", "parentRegion", "slotId", "renderOwner", "hydrationMode", "source", "evidenceIds", "responsiveBehavior" })
            {
                Assert.Contains(field, generator, StringComparison.Ordinal);
                Assert.Contains(field, validator, StringComparison.Ordinal);
            }

            Assert.Contains("Starter generation contract", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-TOPOLOGY-004", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void CapabilityMapping_BindsSupportedFeaturesAndHidesUnsupportedModules()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/map-capabilities.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderCapabilities.ps1");

            foreach (var input in new[]
            {
                "Starter feature manifest",
                "Backend public configuration feature map",
                "Store module manifest if available",
                "Target visual detections",
                "Starter generation contract slots",
            })
            {
                Assert.Contains(input, generator, StringComparison.Ordinal);
                Assert.Contains(input, validator, StringComparison.Ordinal);
            }

            foreach (var decision in new[] { "target", "target-with-starter-binding", "starter", "hidden", "unsupported" })
            {
                Assert.Contains(decision, generator, StringComparison.Ordinal);
                Assert.Contains(decision, validator, StringComparison.Ordinal);
            }

            Assert.Contains("product-gallery", generator, StringComparison.Ordinal);
            Assert.Contains("wishlist", generator, StringComparison.Ordinal);
            Assert.Contains("product-reviews", generator, StringComparison.Ordinal);
            Assert.Contains("cart-badge", generator, StringComparison.Ordinal);
            Assert.Contains("SFB-CAPABILITY-004", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-CAPABILITY-005", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void CompositionManifest_IncludesGenerationInputsAndPackageVersions()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/build-composition-manifest.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderComposition.ps1");

            foreach (var field in new[]
            {
                "projectName",
                "storeKey",
                "sourceStarterPath",
                "starterContractVersion",
                "StorefrontClientPackageVersion",
                "StorefrontRuntimePackageVersion",
                "generatedFileRoot",
                "assetRoot",
                "shellComposition",
                "pageComposition",
                "slotBindings",
                "featureDecisions",
                "fallbackPages",
                "evidenceReferences",
                "inferenceReferences",
            })
            {
                Assert.Contains(field, generator, StringComparison.Ordinal);
                Assert.Contains(field, validator, StringComparison.Ordinal);
            }

            Assert.Contains("SFB-COMPOSITION-002", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-COMPOSITION-003", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void GenerationOwnershipPlan_DeclaresFileActionsAndRejectsProtectedEdits()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/plan-generation-files.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGenerationPlan.ps1");

            foreach (var field in new[]
            {
                "filePath",
                "ownership",
                "action",
                "sourceArtifactIds",
                "expectedSlot",
                "validationRuleIds",
                "conflictBehavior",
                "sourceSpecHash",
                "generatedHash",
            })
            {
                Assert.Contains(field, generator, StringComparison.Ordinal);
                Assert.Contains(field, validator, StringComparison.Ordinal);
            }

            Assert.Contains("--dry-run", generator, StringComparison.Ordinal);
            Assert.Contains("generate-from-starter", generator, StringComparison.Ordinal);
            Assert.Contains("apply-visual-files", generator, StringComparison.Ordinal);
            Assert.Contains("ownership: protected", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-GENPLAN-003", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void GeneratedStorefrontProjectCreation_WrapsStarterGenerationAndWritesMetadata()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1");

            foreach (var text in new[]
            {
                "BlazorShop.Storefront.{Name}",
                "BlazorShop.PresentationV2",
                "scripts\\generate-storefront-sample.ps1",
                "StoreKey",
                "Copy Starter",
                "StorefrontPackageVersions.props",
                "Features\\feature-manifest.json",
                "docs\\storefront-analysis",
                "metadata.yaml",
                "BlazorShop.Storefront.Client",
                "BlazorShop.Storefront.Runtime",
                "Endpoints/StarterBffEndpoints.cs",
                "Security/StarterReturnUrlValidator.cs",
            })
            {
                Assert.Contains(text, generator, StringComparison.Ordinal);
            }

            Assert.Contains("SFB-PROJECT-003", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-PROJECT-004", validator, StringComparison.Ordinal);
            Assert.Contains("SFB-PROJECT-005", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void VisualFoundationGeneration_ProducesScopedThemeCssWithoutScriptInjection()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-visual-foundation.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderCss.ps1");
            var app = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/Components/App.razor");

            foreach (var cssSurface in new[]
            {
                "--sfb-color-",
                "--sfb-font-",
                "--sfb-text-",
                "--sfb-space-",
                "--sfb-container",
                "--sfb-border-width",
                "--sfb-radius",
                "--sfb-shadow",
                "--sfb-motion",
                "--sfb-ease",
                "button",
                "input",
                "starter-product-card",
                "aspect-ratio: 1 / 1",
                ":focus-visible",
                "@media",
            })
            {
                Assert.Contains(cssSurface, generator, StringComparison.Ordinal);
                Assert.Contains(cssSurface, validator, StringComparison.Ordinal);
            }

            Assert.Contains("wwwroot/css/storefront-builder.generated.css", generator, StringComparison.Ordinal);
            Assert.Contains("css/storefront-builder.generated.css", app, StringComparison.Ordinal);
            Assert.Contains("SFB-CSS-002", validator, StringComparison.Ordinal);
        }

        [Fact]
        public void CompositionFiles_GenerateShellCatalogProductAndFallbackPresentation()
        {
            var generator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs");
            var validator = ReadRepositoryFile("tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderCompositionFiles.ps1");

            foreach (var marker in new[]
            {
                "shell",
                "home",
                "catalog",
                "product",
                "fallback",
                "cart.add-line",
            })
            {
                Assert.Contains(marker, generator, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var rule in new[]
            {
                "SFB-COMPOSITION-001",
                "SFB-COMPOSITION-004",
                "SFB-COMPOSITION-006",
                "SFB-COMPOSITION-007",
                "SFB-COMPOSITION-008",
                "SFB-COMMERCE-001",
                "SFB-COMMERCE-002",
            })
            {
                Assert.Contains(rule, validator, StringComparison.Ordinal);
            }
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
