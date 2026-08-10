namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontCommerceScriptRegressionTests
    {
        [Fact]
        public void F1_54_V2Script_DoesNotInvokeApplicationCommandsOrBuildCommercePayloads()
        {
            var visualScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var forbiddenTokens = new[]
            {
                "getStorefrontApplication().cart.addLine",
                "getStorefrontApplication().productSelection.preview",
                "window.blazorShopStorefront?.application",
                "window.blazorShopStorefront.application",
                "blazorShopStorefront.bindings",
                ".application.cart.",
                ".application.productSelection.",
                "ProductId:",
                "ProductVariantId:",
                "SelectedAttributes:",
                "CurrencyCode:",
                "preview.sku",
                "preview.gtin",
                "preview.stockQuantity",
                "preview.canAddToCart",
                "canAddToCart",
                "stockQuantity",
                "data-storefront-address-select",
                "manualAddressFieldSelector",
                "syncManualAddressFields",
                "initCheckoutAddressSelection",
                "field.disabled",
            };

            foreach (var forbiddenToken in forbiddenTokens)
            {
                Assert.DoesNotContain(forbiddenToken, visualScript, StringComparison.Ordinal);
            }

            Assert.Contains("storefront:product-purchase:selection-changed", visualScript, StringComparison.Ordinal);
            Assert.Contains("storefront:product-purchase:add-line-succeeded", visualScript, StringComparison.Ordinal);
            Assert.Contains("storefront:product-purchase:add-line-failed", visualScript, StringComparison.Ordinal);
            Assert.Contains("showToast(\"success\", \"Cart\", message)", visualScript, StringComparison.Ordinal);
            Assert.Contains("syncGalleryMainImage(rootElement, selection.mainImageUrl)", visualScript, StringComparison.Ordinal);
            Assert.Contains("selectGalleryThumbnail", visualScript, StringComparison.Ordinal);
            Assert.Contains("const sku = scope.querySelector(\"[data-storefront-selection-sku]\")", visualScript, StringComparison.Ordinal);
            Assert.Contains("const gtin = scope.querySelector(\"[data-storefront-selection-gtin]\")", visualScript, StringComparison.Ordinal);
            Assert.Contains("setText(sku, selection.skuText || \"\")", visualScript, StringComparison.Ordinal);
            Assert.Contains("setText(gtin, selection.gtinText || \"\")", visualScript, StringComparison.Ordinal);
            Assert.Contains("toggleHidden(sku, !selection.skuText)", visualScript, StringComparison.Ordinal);
            Assert.Contains("toggleHidden(gtin, !selection.gtinText)", visualScript, StringComparison.Ordinal);
            Assert.Contains("selection.ready", visualScript, StringComparison.Ordinal);
            Assert.Contains("selection.valid", visualScript, StringComparison.Ordinal);
            Assert.Contains("selection.mainImageUrl", visualScript, StringComparison.Ordinal);
            Assert.Contains("selection.message", visualScript, StringComparison.Ordinal);

            foreach (var forbiddenSelectionRead in new[]
            {
                "selection.productId",
                "selection.productVariantId",
                "selection.selectedAttributes",
                "selection.quantity",
                "selection.currencyCode",
                "selection.unitPrice",
            })
            {
                Assert.DoesNotContain(forbiddenSelectionRead, visualScript, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void F1_54_PresentationScript_OwnsProductPurchaseBinderAndCommandPayloads()
        {
            var applicationScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");

            foreach (var marker in new[]
            {
                "root.events = Object.freeze({ ...events })",
                "root.initialize = initializeBindings",
                "root.refreshPageBindings = refreshPageBindings",
                "initializeGlobalListenersOnce",
                "refreshPageBindings(document)",
                "document.addEventListener(\"enhancedload\"",
                "new WeakSet()",
                "let bindingsInitialized = false",
                "if (bindingsInitialized)",
                "productPurchaseRootSelector",
                "productPurchaseSubmitMarkerSelector = \"[data-storefront-product-purchase-submit]\"",
                "productPurchaseSubmitSelector = '[data-storefront-command=\"cart.add-line\"][data-storefront-product-purchase-submit]'",
                "readPurchaseDescriptor",
                "readSelectedAttributes",
                "buildSelectionPreviewPayload",
                "buildAddLinePayload",
                "ProductId: descriptor.productId",
                "ProductVariantId: descriptor.selectedVariantId || null",
                "SelectedAttributes: descriptor.selectedAttributes.length > 0 ? descriptor.selectedAttributes : null",
                "CurrencyCode: descriptor.currencyCode || null",
                "productSelection.preview(descriptor.previewRoute, payload)",
                "cart.addLine(payload)",
                "storefront:product-purchase:selection-changed",
                "storefront:product-purchase:selection-error",
                "storefront:product-purchase:add-line-succeeded",
                "storefront:product-purchase:add-line-failed",
                "dispatch(events.productPurchaseSelectionChanged",
                "priceText:",
                "comparePriceText:",
                "stockText:",
                "skuText:",
                "gtinText:",
                "mainImageUrl:",
                "message",
                "ready: isReady",
                "valid: isValid",
            })
            {
                Assert.Contains(marker, applicationScript, StringComparison.Ordinal);
            }

            foreach (var forbidden in new[]
            {
                "root.application",
                "root.bindings",
                LegacyAlias("add-to-cart") + "]",
                "addToCart: { addPurchaseLine }",
                "productSelection: { previewPurchase }",
                "preview: preview || null",
                "summary: summary || null",
                "selection: selection || null",
                "blazorshop:cart-changed",
            })
            {
                Assert.DoesNotContain(forbidden, applicationScript, StringComparison.Ordinal);
            }

            var publicProjection = ExtractFunction(applicationScript, "projectVisualSelection");
            foreach (var marker in new[]
            {
                "ready:",
                "valid:",
                "priceText:",
                "comparePriceText:",
                "stockText:",
                "skuText:",
                "gtinText:",
                "mainImageUrl:",
                "message:",
            })
            {
                Assert.Contains(marker, publicProjection, StringComparison.Ordinal);
            }

            foreach (var forbiddenProjectionField in new[]
            {
                "productId",
                "productVariantId",
                "selectedAttributes",
                "quantity",
                "currencyCode",
                "unitPrice",
                "available",
            })
            {
                Assert.DoesNotContain(forbiddenProjectionField, publicProjection, StringComparison.Ordinal);
            }

            Assert.Contains("selection: projectVisualSelection(selection)", applicationScript, StringComparison.Ordinal);
            Assert.Contains("dispatch(events.cartChanged, { count })", applicationScript, StringComparison.Ordinal);
            Assert.Contains("count: cartCount(summary)", applicationScript, StringComparison.Ordinal);
        }

        [Fact]
        public void F1_54_PresentationScript_StillOwnsSameOriginTransportAndAntiforgery()
        {
            var applicationScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");

            Assert.Contains("fetch(assertLocalRoute(route), options)", applicationScript, StringComparison.Ordinal);
            Assert.Contains("readAntiforgeryHeader", applicationScript, StringComparison.Ordinal);
            Assert.Contains("value.startsWith(\"//\")", applicationScript, StringComparison.Ordinal);
            Assert.Contains("cartApiRoute = \"/api/cart\"", applicationScript, StringComparison.Ordinal);
            Assert.Contains("productSelectionPreviewRoute = \"/api/product-selection-preview\"", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:cart:changed", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:cart:error", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:product-selection:changed", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:product-selection:error", applicationScript, StringComparison.Ordinal);
        }

        [Fact]
        public void ProductDetailExtraction_KeepsGalleryAndSelectionHooksScopedByMain()
        {
            var visualScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            foreach (var selector in new[]
            {
                "const productGallerySelector = \"[data-storefront-product-gallery]\"",
                "const galleryThumbnailSelector = \"[data-storefront-gallery-thumbnail]\"",
                "const galleryMainImageSelector = \"[data-storefront-gallery-main-image]\"",
                "const galleryPlaceholderSelector = \"[data-storefront-gallery-placeholder]\"",
                "const galleryPreviousSelector = \"[data-storefront-gallery-prev]\"",
                "const galleryNextSelector = \"[data-storefront-gallery-next]\"",
                "scope.querySelector(\"[data-storefront-selection-price]\")",
                "scope.querySelector(\"[data-storefront-selection-compare]\")",
                "scope.querySelector(\"[data-storefront-selection-stock]\")",
                "scope.querySelector(\"[data-storefront-selection-sku]\")",
                "scope.querySelector(\"[data-storefront-selection-gtin]\")",
            })
            {
                Assert.Contains(selector, visualScript, StringComparison.Ordinal);
            }

            var gallerySync = ExtractFunction(visualScript, "syncGalleryMainImage");
            Assert.Contains("const scope = container.closest(\"main\") || document", gallerySync, StringComparison.Ordinal);
            Assert.Contains("const gallery = scope.querySelector(productGallerySelector)", gallerySync, StringComparison.Ordinal);
            Assert.DoesNotContain("container.querySelector(productGallerySelector)", gallerySync, StringComparison.Ordinal);

            var selectionVisual = ExtractFunction(visualScript, "applySelectionVisual");
            Assert.Contains("const scope = rootElement?.closest(\"main\") || document", selectionVisual, StringComparison.Ordinal);
            Assert.Contains("const price = scope.querySelector(\"[data-storefront-selection-price]\")", selectionVisual, StringComparison.Ordinal);
            Assert.Contains("const compare = scope.querySelector(\"[data-storefront-selection-compare]\")", selectionVisual, StringComparison.Ordinal);
            Assert.Contains("const stock = scope.querySelector(\"[data-storefront-selection-stock]\")", selectionVisual, StringComparison.Ordinal);
            Assert.Contains("const sku = scope.querySelector(\"[data-storefront-selection-sku]\")", selectionVisual, StringComparison.Ordinal);
            Assert.Contains("const gtin = scope.querySelector(\"[data-storefront-selection-gtin]\")", selectionVisual, StringComparison.Ordinal);
            Assert.DoesNotContain("rootElement?.querySelector(\"[data-storefront-selection-price]\")", selectionVisual, StringComparison.Ordinal);
        }

        [Fact]
        public void F1_63_LegacySelectorAliases_AreAbsentFromBrowserSourcesAndGeneratedTransforms()
        {
            var sourceRoots = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
                "tools/BlazorShop.AI.StorefrontBuilder/scripts/generate",
            };
            var generatedRoot = Path.Combine(FindRepositoryRoot(), "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof");
            var forbiddenAliases = new[]
            {
                LegacyAlias("selection-preview"),
                LegacyAlias("selection-quantity"),
                LegacyAlias("add-to-cart"),
                LegacyAlias("generated-quantity"),
                LegacyAlias("attribute-control"),
                LegacyAlias("variant-select"),
                "dataset.previewRoute",
                "dataset.variantSelect",
                "dataset.attributeName",
                "blazorshop:cart-changed",
            };
            var failures = new List<string>();

            foreach (var sourceRoot in sourceRoots)
            {
                AssertAliasesAbsent(Path.Combine(FindRepositoryRoot(), sourceRoot), forbiddenAliases, failures);
            }

            if (Directory.Exists(generatedRoot))
            {
                AssertAliasesAbsent(generatedRoot, forbiddenAliases, failures);
            }

            Assert.Empty(failures);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
        }

        private static string LegacyAlias(string suffix)
        {
            return "data-storefront-" + suffix;
        }

        private static string ExtractFunction(string source, string functionName)
        {
            var start = source.IndexOf($"function {functionName}", StringComparison.Ordinal);
            if (start < 0)
            {
                throw new InvalidOperationException($"Function '{functionName}' was not found.");
            }

            var nextFunction = source.IndexOf("\n  function ", start + 1, StringComparison.Ordinal);
            return nextFunction < 0 ? source[start..] : source[start..nextFunction];
        }

        private static void AssertAliasesAbsent(string root, IReadOnlyCollection<string> forbiddenAliases, List<string> failures)
        {
            var sourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".cs",
                ".razor",
                ".cshtml",
                ".js",
                ".mjs",
                ".ts",
                ".json",
                ".yaml",
                ".yml",
                ".ps1",
            };

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (!sourceExtensions.Contains(Path.GetExtension(file))
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || file.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                foreach (var forbiddenAlias in forbiddenAliases)
                {
                    if (source.Contains(forbiddenAlias, StringComparison.Ordinal))
                    {
                        failures.Add($"{Path.GetRelativePath(FindRepositoryRoot(), file)} contains {forbiddenAlias}");
                    }
                }
            }
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
        }
    }
}
