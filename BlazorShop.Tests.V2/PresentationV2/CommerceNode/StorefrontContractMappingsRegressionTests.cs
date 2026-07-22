extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.CommerceNode.ProductSelections;
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
