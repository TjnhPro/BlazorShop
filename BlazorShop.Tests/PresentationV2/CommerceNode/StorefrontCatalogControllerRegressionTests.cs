namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using Xunit;

    public sealed class StorefrontCatalogControllerRegressionTests
    {
        [Fact]
        public void ProductFilterMetadata_DoesNotRunCatalogQueriesConcurrentlyOnScopedDbContext()
        {
            // Regression: CAT-002 - product filter metadata triggered concurrent EF operations on one scoped DbContext.
            // Found by /qa on 2026-07-18.
            // Report: .gstack/qa-reports/storefront-release-2026-07-18.md
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedCatalogController.cs");

            Assert.Contains("var categories = await this.publicCatalogService.GetPublishedCategoryTreeAsync();", controller);
            Assert.Contains("var metadata = await this.publicCatalogService.GetPublishedProductFilterMetadataAsync", controller);
            Assert.DoesNotContain("var categoriesTask = this.publicCatalogService.GetPublishedCategoryTreeAsync();", controller);
            Assert.DoesNotContain("var metadataTask = this.publicCatalogService.GetPublishedProductFilterMetadataAsync", controller);
        }

        [Fact]
        public void ProductMapping_DoesNotResolveDisplayMoneyConcurrentlyOnScopedDbContext()
        {
            // Regression: catalog product mapping used Task.WhenAll while ResolveDisplayMoneyAsync depends on scoped EF services.
            var controller = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront/StorefrontScopedCatalogController.cs");

            Assert.Contains("ToDisplayCatalogProductContractsAsync", controller);
            Assert.Contains("ToSearchSuggestionContractsAsync", controller);
            Assert.DoesNotContain("Task.WhenAll(products.Items.Select(product => this.ToDisplayCatalogProductContractAsync", controller);
            Assert.DoesNotContain("Task.WhenAll(products.Select(product => this.ToDisplayCatalogProductContractAsync", controller);
            Assert.DoesNotContain("Task.WhenAll(categoryPage.Products.Select(product => this.ToDisplayCatalogProductContractAsync", controller);
            Assert.DoesNotContain("Task.WhenAll(suggestions.Select(product => this.ToSearchSuggestionContractAsync", controller);
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
