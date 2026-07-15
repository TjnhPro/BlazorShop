namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontBrandingMarkupTests
    {
        [Fact]
        public void StorefrontHeader_ConsumesDisplayContextAndRendersLogo()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor");

            Assert.Contains("@inject IStorefrontDisplayContextProvider DisplayContextProvider", markup);
            Assert.Contains("BrandLogoUrl", markup);
            Assert.Contains("bs-storefront-header__brand-logo", markup);
            Assert.Contains("DisplayContextProvider.GetAsync()", markup);
        }

        [Fact]
        public void StorefrontBrandHead_RendersStoreSpecificIconsAndLanguage()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Seo/StorefrontBrandHead.razor");

            Assert.Contains("@inject IStorefrontDisplayContextProvider DisplayContextProvider", markup);
            Assert.Contains("<link rel=\"icon\" href=\"@_displayContext.FaviconUrl\" />", markup);
            Assert.Contains("<link rel=\"icon\" type=\"image/png\" href=\"@_displayContext.PngIconUrl\" />", markup);
            Assert.Contains("<link rel=\"apple-touch-icon\" href=\"@_displayContext.AppleTouchIconUrl\" />", markup);
            Assert.Contains("msapplication-TileImage", markup);
            Assert.Contains("document.documentElement.lang", markup);
        }

        [Fact]
        public void MainLayout_IncludesStorefrontBrandHead()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");

            Assert.Contains("<StorefrontBrandHead />", markup);
            Assert.Contains("<StorefrontHeader />", markup);
        }

        [Fact]
        public void StorefrontCss_DefinesStableBrandLogoDimensions()
        {
            var styles = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css");

            Assert.Contains(".bs-storefront-header__brand-logo", styles);
            Assert.Contains("height: 2rem;", styles);
            Assert.Contains("object-fit: contain;", styles);
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
