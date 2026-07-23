namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontSearchPageRegressionTests
    {
        [Fact]
        public void SearchPage_PassesQueryValuesToCatalogFilterPanel()
        {
            // Regression: CAT-002 - search category and q controls displayed literal/default values after filtered route load.
            // Found by /qa on 2026-07-18.
            // Report: .gstack/qa-reports/storefront-release-2026-07-18.md
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Catalog/SearchPage.razor");

            Assert.Contains("CategorySlug=\"@Category\"", markup);
            Assert.Contains("SearchTerm=\"@Q\"", markup);
            Assert.DoesNotContain("CategorySlug=\"Category\"", markup);
            Assert.DoesNotContain("SearchTerm=\"Q\"", markup);
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
