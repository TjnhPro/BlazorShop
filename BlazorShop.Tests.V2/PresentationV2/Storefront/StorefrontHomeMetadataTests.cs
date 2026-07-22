namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontHomeMetadataTests
    {
        [Fact]
        public void HomePage_LoadsReservedHomeMetadataSlug()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Home.razor");

            Assert.Contains("GetPublishedPageBySlugAsync(StorefrontRoutes.HomeMetadataSlug)", markup);
            Assert.Contains("SeoComposer.ComposeHomePageAsync", markup);
            Assert.DoesNotContain("StorefrontRoutes.Page(StorefrontRoutes.HomeMetadataSlug)", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void SeoComposer_UsesHomeCanonicalForHomeMetadataPage()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontSeoComposer.cs");

            Assert.Contains("ComposeHomePageAsync(GetStorefrontPage? homePage", source);
            Assert.Contains("RelativePath = StorefrontRoutes.Home", source);
            Assert.DoesNotContain("CanonicalUrl = homePage.Seo.CanonicalUrl", source, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontRoutes_DocumentsReservedHomeMetadataSlug()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontRoutes.cs");

            Assert.Contains("HomeMetadataSlug = \"home\"", source);
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
