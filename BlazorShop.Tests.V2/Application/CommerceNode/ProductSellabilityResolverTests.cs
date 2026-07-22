namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;

    using Xunit;

    public sealed class ProductSellabilityResolverTests
    {
        private static readonly DateTimeOffset Now = new(2026, 7, 16, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Resolve_WhenPublishedSimpleProductIsInStock_ReturnsPurchasable()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            var result = Resolve(storeId, product, requestedQuantity: 2);

            Assert.True(result.Purchasable);
            Assert.Empty(result.PurchaseBlockReasons);
            Assert.Equal(ProductStockStatuses.InStock, result.StockStatus);
            Assert.Equal(5, result.AvailableQuantity);
            Assert.Equal(1, result.MinOrderQuantity);
            Assert.Equal(1, result.QuantityStep);
            Assert.True(result.ManageStock);
        }

        [Fact]
        public void Resolve_WhenProductIsNotStoreVisible_ReturnsNotVisible()
        {
            var product = CreateProduct(Guid.NewGuid(), quantity: 5);
            var result = Resolve(Guid.NewGuid(), product);

            AssertBlocked(result, ProductPurchaseBlockReasons.NotVisible);
        }

        [Fact]
        public void Resolve_WhenProductIsNotPublished_ReturnsNotPublished()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            product.IsPublished = false;
            product.PublishedOn = null;

            var result = Resolve(storeId, product);

            AssertBlocked(result, ProductPurchaseBlockReasons.NotPublished);
        }

        [Fact]
        public void Resolve_WhenProductHasFutureAvailability_ReturnsNotStarted()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            product.AvailableStartUtc = Now.UtcDateTime.AddHours(1);

            var result = Resolve(storeId, product);

            AssertBlocked(result, ProductPurchaseBlockReasons.NotStarted);
        }

        [Fact]
        public void Resolve_WhenProductAvailabilityExpired_ReturnsExpired()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            product.AvailableEndUtc = Now.UtcDateTime;

            var result = Resolve(storeId, product);

            AssertBlocked(result, ProductPurchaseBlockReasons.Expired);
        }

        [Fact]
        public void Resolve_WhenPurchaseIsDisabled_ReturnsPurchaseDisabledReason()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            product.PurchasingDisabled = true;
            product.PurchasingDisabledReason = "Seasonal pause";

            var result = Resolve(storeId, product);

            AssertBlocked(result, ProductPurchaseBlockReasons.PurchaseDisabled);
            Assert.Contains("Seasonal pause", result.PurchaseBlockMessages);
        }

        [Fact]
        public void Resolve_WhenVariantInventoryHasNoSelectedVariant_ReturnsVariantRequired()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            product.ProductType = ProductTypes.VariantInventory;
            product.Variants.Add(new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, IsActive = true, Stock = 5 });

            var result = Resolve(storeId, product);

            AssertBlocked(result, ProductPurchaseBlockReasons.VariantRequired);
            Assert.Equal(ProductStockStatuses.VariantRequired, result.StockStatus);
        }

        [Fact]
        public void Resolve_WhenSelectedVariantIsInactive_ReturnsVariantInactive()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            product.ProductType = ProductTypes.VariantInventory;
            var variant = new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id, IsActive = false, Stock = 5 };
            product.Variants.Add(variant);

            var result = Resolve(storeId, product, variant);

            AssertBlocked(result, ProductPurchaseBlockReasons.VariantInactive);
        }

        [Fact]
        public void Resolve_WhenManagedStockIsZero_ReturnsOutOfStock()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 0);

            var result = Resolve(storeId, product);

            AssertBlocked(result, ProductPurchaseBlockReasons.OutOfStock);
            Assert.Equal(ProductStockStatuses.OutOfStock, result.StockStatus);
        }

        [Fact]
        public void Resolve_WhenRequestedQuantityIsBelowMinimum_ReturnsBelowMinQuantity()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 5);
            product.MinOrderQuantity = 2;

            var result = Resolve(storeId, product, requestedQuantity: 1);

            AssertBlocked(result, ProductPurchaseBlockReasons.BelowMinQuantity);
        }

        [Fact]
        public void Resolve_WhenRequestedQuantityIsAboveMaximum_ReturnsAboveMaxQuantity()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 10);
            product.MaxOrderQuantity = 4;

            var result = Resolve(storeId, product, requestedQuantity: 5);

            AssertBlocked(result, ProductPurchaseBlockReasons.AboveMaxQuantity);
        }

        [Fact]
        public void Resolve_WhenRequestedQuantityDoesNotMatchStep_ReturnsInvalidQuantityStep()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 10);
            product.MinOrderQuantity = 2;
            product.QuantityStep = 3;

            var result = Resolve(storeId, product, requestedQuantity: 4);

            AssertBlocked(result, ProductPurchaseBlockReasons.InvalidQuantityStep);
        }

        [Fact]
        public void Resolve_WhenRequestedQuantityExceedsManagedStock_ReturnsNotEnoughStock()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 2);

            var result = Resolve(storeId, product, requestedQuantity: 3);

            AssertBlocked(result, ProductPurchaseBlockReasons.NotEnoughStock);
        }

        [Fact]
        public void Resolve_WhenStockIsUnmanaged_AllowsZeroQuantityProduct()
        {
            var storeId = Guid.NewGuid();
            var product = CreateProduct(storeId, quantity: 0);
            product.ManageStock = false;

            var result = Resolve(storeId, product, requestedQuantity: 3);

            Assert.True(result.Purchasable);
            Assert.Equal(ProductStockStatuses.NotManaged, result.StockStatus);
            Assert.Null(result.AvailableQuantity);
        }

        private static ProductSellabilityResult Resolve(
            Guid storeId,
            Product product,
            ProductVariant? variant = null,
            int requestedQuantity = 1)
        {
            return new ProductSellabilityResolver().Resolve(new ProductSellabilityRequest(
                storeId,
                product,
                variant,
                requestedQuantity,
                Now));
        }

        private static void AssertBlocked(ProductSellabilityResult result, string reason)
        {
            Assert.False(result.Purchasable);
            Assert.Contains(reason, result.PurchaseBlockReasons);
            Assert.NotEmpty(result.PurchaseBlockMessages);
        }

        private static Product CreateProduct(Guid storeId, int quantity)
        {
            var categoryId = Guid.NewGuid();
            return new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Name = "Published product",
                Slug = $"published-{Guid.NewGuid():N}",
                Price = 20m,
                Quantity = quantity,
                IsPublished = true,
                PublishedOn = Now.UtcDateTime.AddDays(-1),
                ArchivedAt = null,
                ProductType = ProductTypes.Simple,
                CategoryId = categoryId,
                Category = new Category
                {
                    Id = categoryId,
                    StoreId = storeId,
                    Name = "Published category",
                    Slug = "published-category",
                    IsPublished = true,
                },
            };
        }
    }
}
