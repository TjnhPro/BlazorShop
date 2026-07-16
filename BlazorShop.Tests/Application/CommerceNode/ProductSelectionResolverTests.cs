namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Moq;

    using Xunit;

    public sealed class ProductSelectionResolverTests
    {
        [Fact]
        public async Task ResolveAsync_ReturnsResolvedSelection_ForValidAttributes()
        {
            var storeId = Guid.NewGuid();
            var product = CreateCustomVariationProduct(storeId);
            var resolver = CreateResolver(product);

            var result = await resolver.ResolveAsync(new ProductSelectionRequest(
                storeId,
                product.Id,
                SelectedAttributes: [new SelectedAttributeDto("Color", "Red")],
                Quantity: 2));

            Assert.True(result.Success);
            Assert.True(result.CanAddToCart);
            Assert.Equal(product.Id, result.ProductId);
            Assert.Equal(25m, result.UnitPrice);
            Assert.Equal("USD", result.CurrencyCode);
            var attribute = Assert.Single(result.SelectedAttributes);
            Assert.Equal("Color", attribute.Name);
            Assert.Equal("Red", attribute.Value);
        }

        [Fact]
        public async Task ResolveAsync_RejectsInvalidAttributeValue()
        {
            var storeId = Guid.NewGuid();
            var product = CreateCustomVariationProduct(storeId);
            var resolver = CreateResolver(product);

            var result = await resolver.ResolveAsync(new ProductSelectionRequest(
                storeId,
                product.Id,
                SelectedAttributes: [new SelectedAttributeDto("Color", "Blue")]));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Selected attribute value is not available for this product.", result.Message);
        }

        [Fact]
        public async Task ResolveAsync_RejectsMissingRequiredOption()
        {
            var storeId = Guid.NewGuid();
            var product = CreateCustomVariationProduct(storeId);
            var resolver = CreateResolver(product);

            var result = await resolver.ResolveAsync(new ProductSelectionRequest(storeId, product.Id));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Required selected attribute 'Color' is missing.", result.Message);
        }

        [Fact]
        public async Task ResolveAsync_RejectsInactiveVariant()
        {
            var storeId = Guid.NewGuid();
            var product = CreateSimpleVariantProduct(storeId, isActive: false, stock: 5);
            var variant = product.Variants.Single();
            var resolver = CreateResolver(product);

            var result = await resolver.ResolveAsync(new ProductSelectionRequest(
                storeId,
                product.Id,
                ProductVariantId: variant.Id));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Selected product variant is not available.", result.Message);
        }

        [Fact]
        public async Task ResolveAsync_ResolvesVariantFromSelectedTemplateAttributes()
        {
            var storeId = Guid.NewGuid();
            var product = CreateTemplatedVariantProduct(storeId);
            var resolver = CreateResolver(product);

            var result = await resolver.ResolveAsync(new ProductSelectionRequest(
                storeId,
                product.Id,
                SelectedAttributes: [new SelectedAttributeDto("Color", "Red")],
                Quantity: 2));

            Assert.True(result.Success);
            var variant = Assert.Single(product.Variants);
            Assert.Equal(variant.Id, result.ProductVariantId);
            Assert.Equal("Color=Red".ToLowerInvariant(), result.AttributeSignature);
            Assert.Equal(22m, result.UnitPrice);
            Assert.True(result.CanAddToCart);
        }

        [Fact]
        public async Task ResolveAsync_RejectsSelectedAttributesThatDoNotMatchExplicitVariant()
        {
            var storeId = Guid.NewGuid();
            var product = CreateTemplatedVariantProduct(storeId);
            var variant = product.Variants.Single();
            var resolver = CreateResolver(product);

            var result = await resolver.ResolveAsync(new ProductSelectionRequest(
                storeId,
                product.Id,
                ProductVariantId: variant.Id,
                SelectedAttributes: [new SelectedAttributeDto("Color", "Blue")]));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Selected attributes do not match the selected product variant.", result.Message);
        }

        [Fact]
        public async Task ResolveAsync_RejectsStockLimitedSelection()
        {
            var storeId = Guid.NewGuid();
            var product = CreateSimpleVariantProduct(storeId, isActive: true, stock: 1);
            var variant = product.Variants.Single();
            var resolver = CreateResolver(product);

            var result = await resolver.ResolveAsync(new ProductSelectionRequest(
                storeId,
                product.Id,
                ProductVariantId: variant.Id,
                Quantity: 2));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("One or more cart items are out of stock.", result.Message);
        }

        private static ProductSelectionResolver CreateResolver(Product product)
        {
            var productReadRepository = new Mock<IProductReadRepository>();
            productReadRepository
                .Setup(repository => repository.GetPublishedProductDetailsByIdAsync(product.Id))
                .ReturnsAsync(product);

            return new ProductSelectionResolver(
                productReadRepository.Object,
                new FixedWorkingCurrencyResolver("USD"),
                new NoopMoneyConversionService(),
                new MoneyRoundingService(new CurrencyMetadataService()));
        }

        private static Product CreateCustomVariationProduct(Guid storeId)
        {
            var product = CreatePublishedProduct(storeId, 25m, 10);
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
                        IsRequired = true,
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

        private static Product CreateSimpleVariantProduct(Guid storeId, bool isActive, int stock)
        {
            var product = CreatePublishedProduct(storeId, 20m, 10);
            product.ProductType = ProductTypes.VariantInventory;
            product.Variants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Price = 22m,
                Stock = stock,
                IsActive = isActive,
            });

            return product;
        }

        private static Product CreateTemplatedVariantProduct(Guid storeId)
        {
            var product = CreatePublishedProduct(storeId, 20m, 10);
            product.ProductType = ProductTypes.VariantInventory;
            product.VariationTemplate = new VariationTemplate
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                IsActive = true,
                Name = "Color options",
                Slug = "color-options",
                Options =
                {
                    new VariationTemplateOption
                    {
                        Id = Guid.NewGuid(),
                        Name = "Color",
                        IsActive = true,
                        IsRequired = true,
                        Values =
                        {
                            new VariationTemplateValue
                            {
                                Id = Guid.NewGuid(),
                                Value = "Red",
                                IsActive = true,
                            },
                            new VariationTemplateValue
                            {
                                Id = Guid.NewGuid(),
                                Value = "Blue",
                                IsActive = true,
                            },
                        },
                    },
                },
            };

            var normalization = ProductVariantAttributeNormalizer.Normalize(
                [
                    new ProductVariantAttributeDto
                    {
                        Name = "Color",
                        Value = "Red",
                    },
                ]);
            product.Variants.Add(new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Price = 22m,
                Stock = 7,
                IsActive = true,
                AttributeSignature = normalization.AttributeSignature,
                AttributesJson = normalization.AttributesJson,
                DisplayName = normalization.DisplayName,
            });

            return product;
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

        private sealed class FixedWorkingCurrencyResolver : IStorefrontWorkingCurrencyResolver
        {
            private readonly string currencyCode;

            public FixedWorkingCurrencyResolver(string currencyCode)
            {
                this.currencyCode = currencyCode;
            }

            public Task<StorefrontWorkingCurrencyResolution> ResolveAsync(
                Guid storeId,
                string? requestedCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new StorefrontWorkingCurrencyResolution(
                    this.currencyCode,
                    this.currencyCode,
                    requestedCurrencyCode,
                    true,
                    true,
                    "ok"));
            }
        }

        private sealed class NoopMoneyConversionService : IMoneyConversionService
        {
            public Task<ServiceResponse<MoneyConversionResult>> ConvertFromBaseAsync(
                Guid storeId,
                decimal amount,
                string targetCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
