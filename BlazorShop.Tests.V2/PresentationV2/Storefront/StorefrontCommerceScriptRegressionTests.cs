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
