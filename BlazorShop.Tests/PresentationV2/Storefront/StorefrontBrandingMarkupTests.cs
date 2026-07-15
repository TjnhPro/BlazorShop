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
            Assert.Contains("<StorefrontFooter />", markup);
        }

        [Fact]
        public void StorefrontCss_DefinesStableBrandLogoDimensions()
        {
            var styles = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css");

            Assert.Contains(".bs-storefront-header__brand-logo", styles);
            Assert.Contains("height: 2rem;", styles);
            Assert.Contains("object-fit: contain;", styles);
        }

        [Fact]
        public void StorefrontFooter_ConsumesDisplayContextAndContactFields()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontFooter.razor");

            Assert.Contains("@inject IStorefrontDisplayContextProvider DisplayContextProvider", markup);
            Assert.Contains("DisplayContextProvider.GetAsync()", markup);
            Assert.Contains("ContactEmail", markup);
            Assert.Contains("ContactPhone", markup);
            Assert.Contains("_displayContext.CompanyAddress", markup);
            Assert.Contains("mailto:@ContactEmail", markup);
            Assert.DoesNotContain("BLAZORSHOP", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontPricingMarkup_UsesStoreCurrencyContext()
        {
            var files = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CartPage.razor",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/CheckoutPage.razor",
            };

            foreach (var relativePath in files)
            {
                var markup = ReadRepositoryFile(relativePath);
                Assert.DoesNotContain("EUR @", markup, StringComparison.Ordinal);
                Assert.DoesNotContain("€ @", markup, StringComparison.Ordinal);
                Assert.DoesNotContain("€ {", markup, StringComparison.Ordinal);
            }

            Assert.Contains("data-currency-code", ReadRepositoryFile(files[0]));
            Assert.Contains("data-currency-code", ReadRepositoryFile(files[1]));
        }

        [Fact]
        public void StorefrontLocalCart_PostsCurrencyCode()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");
            var apiClient = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs");

            Assert.Contains("CurrencyCode: (button.dataset.currencyCode", script);
            Assert.Contains("CurrencyCode: payload.CurrencyCode || null", script);
            Assert.Contains("CurrencyCode = request.CurrencyCode", program);
            Assert.Contains("public string? CurrencyCode { get; set; }", apiClient);
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
