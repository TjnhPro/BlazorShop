namespace BlazorShop.Tests.Application.Services
{
    using AutoMapper;

    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

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
    }
}
