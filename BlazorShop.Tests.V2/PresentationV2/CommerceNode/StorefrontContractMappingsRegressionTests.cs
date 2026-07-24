extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Domain.Constants;

    using Xunit;

    using StorefrontContractMappings = CommerceNodeApi::BlazorShop.CommerceNode.API.Contracts.Storefront.StorefrontContractMappings;

    public sealed class StorefrontContractMappingsRegressionTests
    {
        [Fact]
        public void ToStorefrontContract_KeepsInStockSimpleProductPurchasable()
        {
            // Regression: PRD-001 - in-stock simple product detail was blocked as out_of_stock.
            // Found by /qa on 2026-07-18.
            // Report: .gstack/qa-reports/storefront-release-2026-07-18.md
            var product = CreateSimpleProduct(quantity: 20);

            var response = StorefrontContractMappings.ToStorefrontContract(product);

            Assert.True(response.Purchasable);
            Assert.True(response.InStock);
            Assert.Empty(response.PurchaseBlockReasons);
            Assert.Equal(ProductStockStatuses.InStock, response.StockStatus);
            Assert.Equal(20, response.AvailableQuantity);
        }

        [Fact]
        public void ToStorefrontContract_DoesNotAddOutOfStockWhenInStockSimpleProductHasPurchaseDisabled()
        {
            // Regression: PRD-002 - purchase-disabled simple product also reported a false out_of_stock reason.
            // Found by /qa on 2026-07-18.
            // Report: .gstack/qa-reports/storefront-release-2026-07-18.md
            var product = CreateSimpleProduct(quantity: 20);
            product.PurchasingDisabled = true;

            var response = StorefrontContractMappings.ToStorefrontContract(product);

            Assert.False(response.Purchasable);
            Assert.Contains(ProductPurchaseBlockReasons.PurchaseDisabled, response.PurchaseBlockReasons);
            Assert.DoesNotContain(ProductPurchaseBlockReasons.OutOfStock, response.PurchaseBlockReasons);
            Assert.Equal(ProductStockStatuses.InStock, response.StockStatus);
        }

        [Fact]
        public void ToPublicConfigurationContract_ProjectsMachineReadableCapabilities()
        {
            var store = new CommerceCurrentStore(
                Guid.NewGuid(),
                "default",
                "Default Store",
                "active",
                "https://store.example",
                "store.example",
                true,
                CdnHost: null,
                LogoUrl: null,
                CompanyName: null,
                CompanyEmail: null,
                CompanyPhone: null,
                CompanyAddress: null,
                FaviconUrl: null,
                PngIconUrl: null,
                AppleTouchIconUrl: null,
                MsTileImageUrl: null,
                MsTileColor: null,
                DefaultCurrencyCode: "USD",
                DefaultCulture: "en-US",
                SupportEmail: null,
                SupportPhone: null,
                MaintenanceModeEnabled: false,
                MaintenanceMessage: null,
                HtmlBodyId: null);
            var seo = new SeoSettingsDto
            {
                SiteName = "Default Store",
            };
            var configuration = StorefrontContractMappings.ToPublicConfigurationContract(
                store,
                [],
                seo,
                new StoreFeatureStateSnapshot(
                    CustomerAccountsEnabled: true,
                    CheckoutEnabled: false,
                    NewsletterEnabled: true,
                    RecommendationsEnabled: false,
                    ReviewsEnabled: true),
                new StorefrontConsentOptions(),
                new CaptchaOptions(),
                new StoreRegistrationRuntimeSettings("disabled", RegistrationAllowed: false),
                ["USD"]);

            Assert.True(configuration.FeatureFlags.CustomerAccountsEnabled);
            Assert.False(configuration.FeatureFlags.CheckoutEnabled);
            Assert.True(configuration.Features["customerAccounts"].Supported);
            Assert.True(configuration.Features["customerAccounts"].Enabled);
            Assert.True(configuration.Features["registration"].Supported);
            Assert.False(configuration.Features["registration"].Enabled);
            Assert.Equal("disabled", configuration.Features["registration"].Reason);
            Assert.False(configuration.Features["checkout"].Enabled);
            Assert.Equal("disabled", configuration.Features["checkout"].Reason);
            Assert.True(configuration.Features["cart"].Enabled);
            Assert.True(configuration.Features["payments"].Enabled);
            Assert.True(configuration.Features["newsletter"].Enabled);
            Assert.False(configuration.Features["recommendations"].Enabled);
            Assert.True(configuration.Features["contactForm"].Enabled);
            Assert.DoesNotContain("reviews", configuration.Features.Keys);
            Assert.DoesNotContain("wishlist", configuration.Features.Keys);
        }

        private static GetProduct CreateSimpleProduct(int quantity)
        {
            return new GetProduct
            {
                Id = Guid.NewGuid(),
                Slug = "qa-simple-product-100",
                Name = "QA Simple Product 100",
                Description = "QA simple product",
                Sku = "QA-SIMPLE-100",
                Price = 100m,
                Image = "/media/products/qa-simple-product-100",
                Quantity = quantity,
                ManageStock = true,
                ProductType = ProductTypes.Simple,
                MinOrderQuantity = 1,
                QuantityStep = 1,
            };
        }
    }
}
