namespace BlazorShop.Tests.Application.Services
{
    using AutoMapper;

    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Moq;

    using Xunit;

    public sealed class ProductVariantServiceTests
    {
        private readonly Mock<IGenericRepository<ProductVariant>> variantRepository = new();
        private readonly Mock<IProductReadRepository> productReadRepository = new();
        private readonly Mock<IMapper> mapper = new();
        private readonly ProductVariantService service;

        public ProductVariantServiceTests()
        {
            this.service = new ProductVariantService(
                this.variantRepository.Object,
                this.mapper.Object,
                this.productReadRepository.Object);
        }

        [Fact]
        public async Task GetByProductIdAsync_WhenProductHasNoVariants_ReturnsEmpty()
        {
            var productId = Guid.NewGuid();
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product { Id = productId, Price = 10m });
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync([]);

            var result = await this.service.GetByProductIdAsync(productId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task AddAsync_WhenSkuAlreadyExistsForProduct_ReturnsFailure()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId, Sku = " variant-1 " };
            var mapped = new ProductVariant { ProductId = productId, Sku = request.Sku };
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product { Id = productId, Price = 10m });
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync(
                [
                    new ProductVariant { ProductId = productId, Sku = "VARIANT-1" },
                ]);

            var result = await this.service.AddAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Variant SKU already exists for this product.", result.Message);
            this.variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenDefaultVariantAlreadyExists_ReturnsFailure()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId, Sku = "variant-2", IsDefault = true };
            var mapped = new ProductVariant { ProductId = productId, Sku = request.Sku, IsDefault = true };
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product { Id = productId, Price = 10m });
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync(
                [
                    new ProductVariant { ProductId = productId, Sku = "variant-1", IsDefault = true },
                ]);

            var result = await this.service.AddAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Product already has a default variant.", result.Message);
            this.variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenAttributeSignatureAlreadyExistsForProduct_ReturnsFailure()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant
            {
                ProductId = productId,
                Attributes =
                [
                    new ProductVariantAttributeDto { Name = "Color", Value = "Red" },
                ],
            };
            var mapped = new ProductVariant { ProductId = productId, AttributeSignature = "color=red" };
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product { Id = productId, Price = 10m });
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync(
                [
                    new ProductVariant { ProductId = productId, AttributeSignature = "color=red" },
                ]);

            var result = await this.service.AddAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Variant attribute combination already exists for this product.", result.Message);
            this.variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenDefaultVariantIsInactive_ReturnsFailure()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId, Sku = "variant-1", IsDefault = true, IsActive = false };
            var mapped = new ProductVariant { ProductId = productId, Sku = request.Sku, IsDefault = true, IsActive = false };
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product { Id = productId, Price = 10m });
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync([]);

            var result = await this.service.AddAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Inactive variant cannot be the default variant.", result.Message);
            this.variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenTemplateOptionIsUnknown_ReturnsFailure()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId };
            var mapped = CreateMappedVariant(
                productId,
                [
                    new ProductVariantAttributeDto { Name = "Color", Value = "Red" },
                    new ProductVariantAttributeDto { Name = "Material", Value = "Cotton" },
                ]);
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(CreateProductWithTemplate(productId));
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync([]);

            var result = await this.service.AddAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Variation option 'Material' is not available for this product.", result.Message);
            this.variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenTemplateValueIsUnknown_ReturnsFailure()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId };
            var mapped = CreateMappedVariant(productId, [new ProductVariantAttributeDto { Name = "Color", Value = "Blue" }]);
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(CreateProductWithTemplate(productId));
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync([]);

            var result = await this.service.AddAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Variation value 'Blue' is not available for option 'Color'.", result.Message);
            this.variantRepository.Verify(repository => repository.AddAsync(It.IsAny<ProductVariant>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenTemplateAttributesAreValid_AddsVariant()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId };
            var mapped = CreateMappedVariant(productId, [new ProductVariantAttributeDto { Name = "Color", Value = "Red" }]);
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(CreateProductWithTemplate(productId));
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync([]);
            this.variantRepository.Setup(repository => repository.AddAsync(mapped))
                .ReturnsAsync(1);

            var result = await this.service.AddAsync(request);

            Assert.True(result.Success);
            this.variantRepository.Verify(repository => repository.AddAsync(mapped), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenProductHasNoTemplate_DoesNotValidateAttributesAgainstTemplate()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId };
            var mapped = CreateMappedVariant(productId, [new ProductVariantAttributeDto { Name = "Material", Value = "Cotton" }]);
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product { Id = productId, Price = 10m });
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync([]);
            this.variantRepository.Setup(repository => repository.AddAsync(mapped))
                .ReturnsAsync(1);

            var result = await this.service.AddAsync(request);

            Assert.True(result.Success);
            this.variantRepository.Verify(repository => repository.AddAsync(mapped), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenSkuIsUnique_TrimsBeforePersisting()
        {
            var productId = Guid.NewGuid();
            var request = new CreateProductVariant { ProductId = productId, Sku = " variant-1 " };
            var mapped = new ProductVariant { ProductId = productId, Sku = request.Sku };
            this.productReadRepository.Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product { Id = productId, Price = 10m });
            this.mapper.Setup(mapper => mapper.Map<ProductVariant>(request))
                .Returns(mapped);
            this.variantRepository.Setup(repository => repository.GetAllAsync())
                .ReturnsAsync([]);
            this.variantRepository.Setup(repository => repository.AddAsync(mapped))
                .ReturnsAsync(1);

            var result = await this.service.AddAsync(request);

            Assert.True(result.Success);
            Assert.Equal("variant-1", mapped.Sku);
            this.variantRepository.Verify(repository => repository.AddAsync(mapped), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenVariantDoesNotBelongToRequestedProduct_ReturnsFailure()
        {
            var existing = new ProductVariant { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), Sku = "variant-1" };
            var request = new UpdateProductVariant
            {
                Id = existing.Id,
                ProductId = Guid.NewGuid(),
                Sku = "variant-2",
            };
            this.variantRepository.Setup(repository => repository.GetByIdAsync(existing.Id))
                .ReturnsAsync(existing);

            var result = await this.service.UpdateAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Variant does not belong to the product.", result.Message);
            this.variantRepository.Verify(repository => repository.UpdateAsync(It.IsAny<ProductVariant>()), Times.Never);
        }

        private static ProductVariant CreateMappedVariant(Guid productId, IReadOnlyList<ProductVariantAttributeDto> attributes)
        {
            var normalized = ProductVariantAttributeNormalizer.Normalize(attributes);
            return new ProductVariant
            {
                ProductId = productId,
                AttributesJson = normalized.AttributesJson,
                AttributeSignature = normalized.AttributeSignature,
                DisplayName = normalized.DisplayName,
                IsActive = true,
            };
        }

        private static Product CreateProductWithTemplate(Guid productId)
        {
            var templateId = Guid.NewGuid();
            return new Product
            {
                Id = productId,
                Price = 10m,
                VariationTemplateId = templateId,
                VariationTemplate = new VariationTemplate
                {
                    Id = templateId,
                    Name = "Shirt options",
                    Slug = "shirt-options",
                    IsActive = true,
                    Options =
                    {
                        new VariationTemplateOption
                        {
                            Name = "Color",
                            ControlType = VariationControlTypes.Color,
                            IsActive = true,
                            IsRequired = true,
                            Values =
                            {
                                new VariationTemplateValue { Value = "Red", IsActive = true },
                                new VariationTemplateValue { Value = "Blue", IsActive = false },
                            },
                        },
                    },
                },
            };
        }
    }
}
