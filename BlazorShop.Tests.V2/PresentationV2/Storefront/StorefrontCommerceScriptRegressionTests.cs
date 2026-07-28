namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontCommerceScriptRegressionTests
    {
        [Fact]
        public void SelectionPreview_DoesNotOverwriteProductSummaryWhenPreviewIsInvalid()
        {
            // Regression: PRD-002 - purchase-disabled simple product was changed to EUR 0.00/out of stock after hydration.
            // Found by /qa on 2026-07-18.
            // Report: .gstack/qa-reports/storefront-release-2026-07-18.md
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js")
                .ReplaceLineEndings("\n");

            Assert.Contains("if (preview.isValid) {\n      setText(price", script);
            Assert.Contains("setText(message, validationMessages[0]", script);
            Assert.Contains("button.disabled = !preview.canAddToCart", script);
            Assert.Contains("if (preview.isValid) {\n        button.dataset.unitPrice", script);
        }

        [Fact]
        public void AddToCart_UsesBackendSellabilityFlagBeforeStockQuantityGuard()
        {
            // Regression: unmanaged-stock products can report stockQuantity=0 while still being purchasable.
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js")
                .ReplaceLineEndings("\n");

            Assert.Contains("button.dataset.canAddToCart = preview.canAddToCart ? \"true\" : \"false\";", script);
            Assert.Contains("const canAddToCart = (button.dataset.canAddToCart || \"\").toLowerCase();", script);
            Assert.Contains("if (!variantSelectSelector && canAddToCart === \"false\")", script);
            Assert.Contains("if (!variantSelectSelector && canAddToCart !== \"true\" && productStock <= 0)", script);
        }

        [Fact]
        public void F1_47_V2Script_IsVisualOnlyAndPresentationOwnsApplicationTransport()
        {
            var visualScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var applicationScript = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js");

            Assert.DoesNotContain("fetch(", visualScript, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/cart", visualScript, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/consent", visualScript, StringComparison.Ordinal);
            Assert.DoesNotContain("/api/product-selection-preview", visualScript, StringComparison.Ordinal);
            Assert.DoesNotContain("blazorshop-antiforgery-token", visualScript, StringComparison.Ordinal);
            Assert.DoesNotContain("blazorshop-antiforgery-header", visualScript, StringComparison.Ordinal);
            Assert.Contains("getStorefrontApplication().cart.addLine", visualScript, StringComparison.Ordinal);
            Assert.Contains("getStorefrontApplication().consent.save", visualScript, StringComparison.Ordinal);
            Assert.Contains("getStorefrontApplication().productSelection.preview", visualScript, StringComparison.Ordinal);

            Assert.Contains("fetch(assertLocalRoute(route), options)", applicationScript, StringComparison.Ordinal);
            Assert.Contains("readAntiforgeryHeader", applicationScript, StringComparison.Ordinal);
            Assert.Contains("let payload = null;", applicationScript, StringComparison.Ordinal);
            Assert.Contains("if (text) {", applicationScript, StringComparison.Ordinal);
            Assert.Contains("payload = JSON.parse(text);", applicationScript, StringComparison.Ordinal);
            Assert.Contains("value.startsWith(\"//\")", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:cart:changed", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:cart:error", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:consent:changed", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:product-selection:changed", applicationScript, StringComparison.Ordinal);
            Assert.Contains("storefront:product-selection:error", applicationScript, StringComparison.Ordinal);
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
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
