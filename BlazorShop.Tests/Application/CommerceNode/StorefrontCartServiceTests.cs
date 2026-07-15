namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Moq;

    using Xunit;

    public sealed class StorefrontCartServiceTests
    {
        [Fact]
        public async Task AddLineAsync_AddsPublishedProduct_WithServerPriceSnapshot()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var product = CreatePublishedProduct(storeId, price: 12.50m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 2,
                CurrencyCode: "usd"));

            Assert.True(result.Success);
            var line = Assert.Single(result.Payload!.Lines);
            Assert.Equal(product.Id, line.ProductId);
            Assert.Equal(2, line.Quantity);
            Assert.Equal(12.50m, line.UnitPriceSnapshot);
            Assert.Equal("USD", line.CurrencyCodeSnapshot);
        }

        [Fact]
        public async Task AddLineAsync_UsesServerDefaultCurrency_WhenClientSendsDifferentCurrency()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var product = CreatePublishedProduct(storeId, price: 12.50m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1,
                CurrencyCode: "eur"));

            Assert.True(result.Success);
            var line = Assert.Single(result.Payload!.Lines);
            Assert.Equal("USD", line.CurrencyCodeSnapshot);
        }

        [Fact]
        public async Task AddLineAsync_UsesResolvedStoreCurrency_AsLineSnapshot()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository, defaultCurrencyCode: "EUR");
            var storeId = Guid.NewGuid();
            var product = CreatePublishedProduct(storeId, price: 12.50m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1,
                CurrencyCode: "usd"));

            Assert.True(result.Success);
            var line = Assert.Single(result.Payload!.Lines);
            Assert.Equal("EUR", line.CurrencyCodeSnapshot);
        }

        [Fact]
        public async Task AddLineAsync_RejectsUnpublishedOrUnavailableProduct()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(productId))
                .ReturnsAsync((Product?)null);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                productId,
                Quantity: 1));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal(0, await context.CartLines.CountAsync());
        }

        [Fact]
        public async Task AddLineAsync_RejectsWrongStoreProduct()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var product = CreatePublishedProduct(Guid.NewGuid(), price: 20m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal(0, await context.CartLines.CountAsync());
        }

        [Fact]
        public async Task AddLineAsync_RejectsInvalidVariant()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var product = CreatePublishedProduct(storeId, price: 20m, stock: 10);
            product.ProductType = ProductTypes.VariantInventory;
            product.Variants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Stock = 5,
                Price = 22m,
                IsDefault = false,
            });
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                ProductVariantId: Guid.NewGuid(),
                Quantity: 1));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal(0, await context.CartLines.CountAsync());
        }

        [Fact]
        public async Task AddLineAsync_RejectsQuantityBelowMinimum_BeforeProductLookup()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                Guid.NewGuid(),
                "token",
                Guid.NewGuid(),
                Quantity: 0));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            productRepository.Verify(
                repository => repository.GetPublishedProductDetailsByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task AddLineAsync_CreatesDistinctLines_ForDistinctPersonalizationHash()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var product = CreateCustomVariationProduct(storeId);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var selectedAttributes = new[]
            {
                new SelectedAttributeDto(" Color ", " Red "),
            };

            var first = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                SelectedAttributes: selectedAttributes,
                PersonalizationHash: "front-art",
                Quantity: 1));
            var second = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload.Token!,
                product.Id,
                SelectedAttributes: selectedAttributes,
                PersonalizationHash: "back-art",
                Quantity: 1));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(2, second.Payload!.Lines.Count);
            Assert.All(second.Payload.Lines, line => Assert.Contains("\"color\"", line.SelectedAttributesJson!, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task AddLineAsync_RejectsTooManySelectedAttributes()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var product = CreateCustomVariationProduct(storeId);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var selectedAttributes = Enumerable.Range(1, 6)
                .Select(index => new SelectedAttributeDto($"Option {index}", "Red"))
                .ToArray();

            var result = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                SelectedAttributes: selectedAttributes,
                Quantity: 1));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal(0, await context.CartLines.CountAsync());
        }

        [Fact]
        public async Task ValidateAsync_ReturnsIssue_WhenProductBecomesUnavailable()
        {
            await using var context = CreateContext();
            var productRepository = new Mock<IProductReadRepository>();
            var service = CreateService(context, productRepository);
            var storeId = Guid.NewGuid();
            var product = CreatePublishedProduct(storeId, price: 15m, stock: 10);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);
            var cart = await service.CreateOrResumeAsync(new StorefrontCartCreateOrResumeRequest(storeId));
            var add = await service.AddLineAsync(new StorefrontCartAddLineRequest(
                storeId,
                cart.Payload!.Token!,
                product.Id,
                Quantity: 1));
            Assert.True(add.Success);
            productRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync((Product?)null);

            var validation = await service.ValidateAsync(storeId, cart.Payload.Token!);

            Assert.True(validation.Success);
            Assert.False(validation.Payload!.IsValid);
            var issue = Assert.Single(validation.Payload.Issues);
            Assert.Equal("cart.product_unavailable", issue.Code);
        }

        private static StorefrontCartService CreateService(
            CommerceNodeDbContext context,
            Mock<IProductReadRepository> productRepository,
            string defaultCurrencyCode = "USD")
        {
            return new StorefrontCartService(
                new StorefrontCartSessionService(context),
                productRepository.Object,
                new FixedStoreCurrencyResolver(defaultCurrencyCode),
                new MoneyRoundingService(new CurrencyMetadataService()));
        }

        private static Product CreatePublishedProduct(Guid storeId, decimal price, int stock)
        {
            var categoryId = Guid.NewGuid();
            return new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Name = "Published product",
                Slug = $"published-{Guid.NewGuid():N}",
                Price = price,
                Quantity = stock,
                IsPublished = true,
                PublishedOn = DateTime.UtcNow,
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

        private static Product CreateCustomVariationProduct(Guid storeId)
        {
            var product = CreatePublishedProduct(storeId, price: 25m, stock: 10);
            product.ProductType = ProductTypes.CustomVariations;
            product.VariationTemplate = new VariationTemplate
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                IsActive = true,
                Name = "Artwork options",
                Slug = "artwork-options",
                Options =
                {
                    new VariationTemplateOption
                    {
                        Id = Guid.NewGuid(),
                        Name = "Color",
                        IsActive = true,
                        Values =
                        {
                            new VariationTemplateValue
                            {
                                Id = Guid.NewGuid(),
                                Value = "Red",
                                IsActive = true,
                            },
                        },
                    },
                },
            };

            return product;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-cart-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class FixedStoreCurrencyResolver : IStoreCurrencyResolver
        {
            private readonly string defaultCurrencyCode;

            public FixedStoreCurrencyResolver(string defaultCurrencyCode)
            {
                this.defaultCurrencyCode = defaultCurrencyCode;
            }

            public Task<string> ResolveDefaultCurrencyCodeAsync(
                Guid storeId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.defaultCurrencyCode);
            }
        }
    }
}
