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
            Assert.Contains("@inject IStorefrontPageNavigationProvider PageNavigationProvider", markup);
            Assert.Contains("BrandLogoUrl", markup);
            Assert.Contains("bs-storefront-header__brand-logo", markup);
            Assert.Contains("DisplayContextProvider.GetAsync()", markup);
            Assert.Contains("StorefrontPageContentRules.Header", markup);
            Assert.DoesNotContain("StorefrontRoutes.About", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.CustomerService", markup, StringComparison.Ordinal);
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
            Assert.DoesNotContain("<HeadContent>", markup, StringComparison.Ordinal);
        }

        [Fact]
        public void AppHead_IncludesStorefrontBrandHeadBeforeHeadOutlet()
        {
            var appMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor");
            var layoutMarkup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");

            Assert.Contains("<StorefrontBrandHead />", appMarkup);
            Assert.Contains("<HeadOutlet />", appMarkup);
            Assert.True(
                appMarkup.IndexOf("<StorefrontBrandHead />", StringComparison.Ordinal) <
                appMarkup.IndexOf("<HeadOutlet />", StringComparison.Ordinal));
            Assert.DoesNotContain("<StorefrontBrandHead />", layoutMarkup, StringComparison.Ordinal);
            Assert.Contains("<StorefrontHeader />", layoutMarkup);
            Assert.Contains("<StorefrontFooter />", layoutMarkup);
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
            Assert.Contains("@inject IStorefrontPageNavigationProvider PageNavigationProvider", markup);
            Assert.Contains("DisplayContextProvider.GetAsync()", markup);
            Assert.Contains("StorefrontPageContentRules.FooterCompany", markup);
            Assert.Contains("StorefrontPageContentRules.FooterSupport", markup);
            Assert.Contains("StorefrontPageContentRules.FooterLegal", markup);
            Assert.Contains("ContactEmail", markup);
            Assert.Contains("ContactPhone", markup);
            Assert.Contains("_displayContext.CompanyAddress", markup);
            Assert.Contains("mailto:@ContactEmail", markup);
            Assert.DoesNotContain("BLAZORSHOP", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.About", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.Privacy", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.Terms", markup, StringComparison.Ordinal);
            Assert.DoesNotContain("StorefrontRoutes.CustomerService", markup, StringComparison.Ordinal);
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

        [Fact]
        public void ProductPage_UsesBackendSelectionPreviewForVariantAttributes()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor");
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var program = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs");

            Assert.Contains("data-storefront-selection-preview", markup);
            Assert.Contains("data-preview-route=\"/api/product-selection-preview\"", markup);
            Assert.Contains("data-storefront-attribute-control", markup);
            Assert.Contains("data-storefront-selection-quantity", markup);
            Assert.Contains("data-storefront-selection-price", markup);
            Assert.Contains("data-storefront-selection-stock", markup);

            Assert.Contains("const selectionPreviewSelector", script);
            Assert.Contains("collectSelectedAttributes", script);
            Assert.Contains("SelectedAttributes: payload.SelectedAttributes || null", script);
            Assert.Contains("/api/product-selection-preview", script);

            Assert.Contains("app.MapPost(\"/api/product-selection-preview\"", program);
            Assert.Contains("PreviewProductSelectionAsync", program);
            Assert.Contains("StorefrontLocalProductSelectionPreviewResponse", program);
        }

        [Fact]
        public void ProductCard_RendersSellabilitySafeActions()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor");

            Assert.Contains("Product.Purchasable && QuantityOneAllowed", markup);
            Assert.Contains("Product.MinOrderQuantity <= 1", markup);
            Assert.Contains("Product.QuantityStep <= 1", markup);
            Assert.Contains("Product.ManageStock ? Math.Max(0, Product.AvailableQuantity ?? Product.Quantity) : 999999", markup);
            Assert.Contains("IsPurchasePaused", markup);
            Assert.Contains("\"purchase_disabled\" => \"Purchasing is paused.\"", markup);
            Assert.Contains("\"below_min_quantity\" => $\"Minimum order quantity is {Product.MinOrderQuantity}.\"", markup);
            Assert.Contains("View Product", markup);
        }

        [Fact]
        public void ProductPage_RendersSellabilityAndQuantityMetadata()
        {
            var markup = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor");

            Assert.Contains("min=\"@_product.MinOrderQuantity\"", markup);
            Assert.Contains("max=\"@_product.MaxOrderQuantity\"", markup);
            Assert.Contains("step=\"@_product.QuantityStep\"", markup);
            Assert.Contains("value=\"@_product.MinOrderQuantity\"", markup);
            Assert.Contains("disabled=\"@(!CanSubmitInitialPurchase)\"", markup);
            Assert.Contains("data-stock=\"@InitialStockValue\"", markup);
            Assert.Contains("Free shipping", markup);
            Assert.Contains("@_product.DeliveryEstimateText", markup);
            Assert.Contains("IsInitialPurchaseHardBlock", markup);
            Assert.Contains("or \"purchase_disabled\"", markup);
            Assert.Contains("or \"out_of_stock\"", markup);
            Assert.Contains("private int InitialStockValue => _product?.ManageStock == false ? 999999", markup);
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
