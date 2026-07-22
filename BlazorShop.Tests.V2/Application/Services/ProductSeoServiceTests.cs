namespace BlazorShop.Tests.Application.Services
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Validations;
    using BlazorShop.Application.Validations.Seo;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Tests.TestUtilities;

    using Moq;

    using Xunit;

    public class ProductSeoServiceTests
    {
        private readonly Mock<IGenericRepository<Product>> _productRepository;
        private readonly Mock<IProductReadRepository> _productReadRepository;
        private readonly Mock<IApplicationTransactionManager> _transactionManager;
        private readonly Mock<ISeoRedirectAutomationService> _seoRedirectAutomationService;
        private readonly ProductSeoService _service;

        public ProductSeoServiceTests()
        {
            _productRepository = new Mock<IGenericRepository<Product>>();
            _productReadRepository = new Mock<IProductReadRepository>();
            _transactionManager = new Mock<IApplicationTransactionManager>();
            _seoRedirectAutomationService = new Mock<ISeoRedirectAutomationService>();

            _transactionManager
                .Setup(manager => manager.ExecuteInTransactionAsync(It.IsAny<Func<Task<ServiceResponse<ProductSeoDto>>>>()))
                .Returns((Func<Task<ServiceResponse<ProductSeoDto>>> action) => action());

            var slugService = new SlugService();
            _service = new ProductSeoService(
                _productRepository.Object,
                _productReadRepository.Object,
                AutoMapperTestFactory.CreateMapper(),
                slugService,
                _transactionManager.Object,
                _seoRedirectAutomationService.Object,
                new ValidationService(),
                new UpdateProductSeoDtoValidator(slugService),
                new StoreSeoSlugPolicyService(slugService, []),
                new NoopStoreSeoSlugHistoryService());
        }

        [Fact]
        public async Task GetByProductIdAsync_WhenProductExists_ReturnsMappedSeoPayload()
        {
            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Slug = "running-shoes",
                MetaTitle = "Running Shoes",
                RobotsIndex = true,
                RobotsFollow = true,
                IsPublished = true,
            };

            _productRepository
                .Setup(repository => repository.GetByIdAsync(productId))
                .ReturnsAsync(product);

            var result = await _service.GetByProductIdAsync(productId);

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.NotNull(result.Payload);
            Assert.Equal(productId, result.Payload!.ProductId);
            Assert.Equal("running-shoes", result.Payload.Slug);
        }

        [Fact]
        public async Task UpdateAsync_WhenSlugIsDuplicate_ReturnsConflict()
        {
            var productId = Guid.NewGuid();
            var existingProduct = new Product { Id = productId, Slug = "existing-slug", IsPublished = true };

            _productRepository
                .Setup(repository => repository.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);
            _productReadRepository
                .Setup(repository => repository.ProductSlugExistsAsync("summer-sale-2026", productId))
                .ReturnsAsync(true);

            var result = await _service.UpdateAsync(productId, new UpdateProductSeoDto
            {
                Slug = "Summer Sale 2026",
                IsPublished = true,
            });

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("Product slug is already in use.", result.Message);
            _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _seoRedirectAutomationService.Verify(service => service.EnsurePermanentRedirectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenProductDoesNotExist_ReturnsNotFound()
        {
            var productId = Guid.NewGuid();

            _productRepository
                .Setup(repository => repository.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            var result = await _service.UpdateAsync(productId, new UpdateProductSeoDto
            {
                Slug = "valid-product-slug",
                IsPublished = true,
            });

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal("Product not found.", result.Message);
        }

        [Fact]
        public async Task UpdateAsync_WhenPublishedSlugChanges_CreatesPermanentRedirectAndUpdatesEntity()
        {
            var productId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var existingPublishedOn = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc);
            var existingProduct = new Product
            {
                Id = productId,
                Name = "Running Shoes",
                Slug = "old-slug",
                IsPublished = true,
                PublishedOn = existingPublishedOn,
                CategoryId = categoryId,
            };

            _productRepository
                .Setup(repository => repository.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);
            _productReadRepository
                .Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product
                {
                    Id = productId,
                    Slug = "old-slug",
                    IsPublished = true,
                    PublishedOn = existingPublishedOn,
                    Category = new Category { Id = categoryId, IsPublished = true },
                });
            _productReadRepository
                .Setup(repository => repository.ProductSlugExistsAsync("summer-sale-2026", productId))
                .ReturnsAsync(false);
            _seoRedirectAutomationService
                .Setup(service => service.EnsurePermanentRedirectAsync("/product/old-slug", "/product/summer-sale-2026"))
                .ReturnsAsync(new ServiceResponse<SeoRedirectDto>(true, "Created", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                    Payload = new SeoRedirectDto
                    {
                        OldPath = "/product/old-slug",
                        NewPath = "/product/summer-sale-2026",
                        StatusCode = 301,
                        IsActive = true,
                    },
                });
            _productRepository
                .Setup(repository => repository.UpdateAsync(existingProduct))
                .ReturnsAsync(1);

            var result = await _service.UpdateAsync(productId, new UpdateProductSeoDto
            {
                Slug = " Summer Sale 2026 ",
                MetaTitle = "Summer Sale",
                IsPublished = true,
            });

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.Equal("summer-sale-2026", existingProduct.Slug);
            Assert.Equal("Summer Sale", existingProduct.MetaTitle);
            Assert.Equal(existingPublishedOn, existingProduct.PublishedOn);
            _seoRedirectAutomationService.Verify(service => service.EnsurePermanentRedirectAsync("/product/old-slug", "/product/summer-sale-2026"), Times.Once);
            _productRepository.Verify(repository => repository.UpdateAsync(existingProduct), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenSlugIsUnchanged_DoesNotCreateRedirect()
        {
            var productId = Guid.NewGuid();
            var existingProduct = new Product
            {
                Id = productId,
                Slug = "summer-sale-2026",
                IsPublished = true,
                PublishedOn = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc),
            };

            _productRepository
                .Setup(repository => repository.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);
            _productReadRepository
                .Setup(repository => repository.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync(new Product
                {
                    Id = productId,
                    Slug = "summer-sale-2026",
                    IsPublished = true,
                    PublishedOn = existingProduct.PublishedOn,
                    Category = new Category { Id = Guid.NewGuid(), IsPublished = true },
                });
            _productReadRepository
                .Setup(repository => repository.ProductSlugExistsAsync("summer-sale-2026", productId))
                .ReturnsAsync(false);
            _productRepository
                .Setup(repository => repository.UpdateAsync(existingProduct))
                .ReturnsAsync(1);

            var result = await _service.UpdateAsync(productId, new UpdateProductSeoDto
            {
                Slug = "summer-sale-2026",
                IsPublished = true,
            });

            Assert.True(result.Success);
            _seoRedirectAutomationService.Verify(service => service.EnsurePermanentRedirectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenProductIsOutsideCurrentStore_ReturnsNotFound()
        {
            var productId = Guid.NewGuid();
            var service = CreateStoreScopedService(Guid.NewGuid());

            _productReadRepository
                .Setup(repository => repository.GetProductDetailsByIdForCurrentStoreAsync(productId))
                .ReturnsAsync((Product?)null);

            var result = await service.UpdateAsync(productId, new UpdateProductSeoDto
            {
                Slug = "valid-product-slug",
                IsPublished = true,
            });

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal("Product not found.", result.Message);
            _productRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenSlugExistsOnlyInAnotherStore_AllowsUpdate()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var slugPolicy = new Mock<IStoreSeoSlugPolicyService>();
            var existingProduct = new Product
            {
                Id = productId,
                StoreId = storeId,
                Slug = "old-slug",
                IsPublished = true,
                PublishedOn = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc),
            };
            var service = CreateStoreScopedService(storeId, slugPolicy.Object);

            _productReadRepository
                .Setup(repository => repository.GetProductDetailsByIdForCurrentStoreAsync(productId))
                .ReturnsAsync(new Product
                {
                    Id = productId,
                    StoreId = storeId,
                    Slug = "old-slug",
                    IsPublished = true,
                    PublishedOn = existingProduct.PublishedOn,
                    Category = new Category { Id = Guid.NewGuid(), StoreId = storeId, IsPublished = true },
                });
            slugPolicy
                .Setup(service => service.ValidateSlugAsync(
                    SeoSlugEntityTypes.Product,
                    "shared-slug",
                    storeId,
                    null,
                    productId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(StoreSeoSlugPolicyResult.Succeeded("shared-slug"));
            _productRepository
                .Setup(repository => repository.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);
            _productRepository
                .Setup(repository => repository.UpdateAsync(existingProduct))
                .ReturnsAsync(1);
            _seoRedirectAutomationService
                .Setup(service => service.EnsurePermanentRedirectAsync("/product/old-slug", "/product/shared-slug"))
                .ReturnsAsync(new ServiceResponse<SeoRedirectDto>(true, "Created", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                    Payload = new SeoRedirectDto
                    {
                        OldPath = "/product/old-slug",
                        NewPath = "/product/shared-slug",
                        StatusCode = 301,
                        IsActive = true,
                    },
                });

            var result = await service.UpdateAsync(productId, new UpdateProductSeoDto
            {
                Slug = "shared-slug",
                IsPublished = true,
            });

            Assert.True(result.Success);
            Assert.Equal("shared-slug", existingProduct.Slug);
            slugPolicy.VerifyAll();
            _productReadRepository.Verify(
                repository => repository.ProductSlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenStoreScopedSlugChanges_UsesPolicyAndSlugHistory()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var existingPublishedOn = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc);
            var existingProduct = new Product
            {
                Id = productId,
                StoreId = storeId,
                Name = "Running Shoes",
                Slug = "old-shoes",
                IsPublished = true,
                PublishedOn = existingPublishedOn,
                CategoryId = categoryId,
            };
            var slugPolicy = new Mock<IStoreSeoSlugPolicyService>();
            var slugHistory = new Mock<IStoreSeoSlugHistoryService>();
            var service = CreateStoreScopedService(storeId, slugPolicy.Object, slugHistory.Object);

            _productReadRepository
                .Setup(repository => repository.GetProductDetailsByIdForCurrentStoreAsync(productId))
                .ReturnsAsync(new Product
                {
                    Id = productId,
                    StoreId = storeId,
                    Slug = "old-shoes",
                    IsPublished = true,
                    PublishedOn = existingPublishedOn,
                    Category = new Category { Id = categoryId, StoreId = storeId, IsPublished = true },
                });
            _productRepository
                .Setup(repository => repository.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);
            _productRepository
                .Setup(repository => repository.UpdateAsync(existingProduct))
                .ReturnsAsync(1);
            slugPolicy
                .Setup(service => service.ValidateSlugAsync(
                    SeoSlugEntityTypes.Product,
                    "new-shoes",
                    storeId,
                    null,
                    productId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(StoreSeoSlugPolicyResult.Succeeded("new-shoes"));
            slugHistory
                .Setup(service => service.GetActiveSlugAsync(
                    SeoSlugEntityTypes.Product,
                    productId,
                    storeId,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((StoreSeoSlugHistoryDto?)null);
            slugHistory
                .Setup(service => service.RecordInitialActiveSlugAsync(
                    SeoSlugEntityTypes.Product,
                    productId,
                    storeId,
                    "old-shoes",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(SlugHistorySuccess(storeId, SeoSlugEntityTypes.Product, productId, "old-shoes"));
            slugHistory
                .Setup(service => service.ReplaceActiveSlugAsync(
                    SeoSlugEntityTypes.Product,
                    productId,
                    storeId,
                    "new-shoes",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(SlugHistorySuccess(storeId, SeoSlugEntityTypes.Product, productId, "new-shoes"));
            _seoRedirectAutomationService
                .Setup(service => service.EnsurePermanentRedirectAsync("/product/old-shoes", "/product/new-shoes"))
                .ReturnsAsync(new ServiceResponse<SeoRedirectDto>(true, "Created", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                    Payload = new SeoRedirectDto
                    {
                        OldPath = "/product/old-shoes",
                        NewPath = "/product/new-shoes",
                        StatusCode = 301,
                        IsActive = true,
                    },
                });

            var result = await service.UpdateAsync(productId, new UpdateProductSeoDto
            {
                Slug = "New Shoes",
                IsPublished = true,
            });

            Assert.True(result.Success);
            Assert.Equal("new-shoes", existingProduct.Slug);
            slugPolicy.VerifyAll();
            slugHistory.VerifyAll();
        }

        private ProductSeoService CreateStoreScopedService(
            Guid storeId,
            IStoreSeoSlugPolicyService? slugPolicyService = null,
            IStoreSeoSlugHistoryService? slugHistoryService = null)
        {
            var slugService = new SlugService();
            return new ProductSeoService(
                _productRepository.Object,
                _productReadRepository.Object,
                AutoMapperTestFactory.CreateMapper(),
                slugService,
                _transactionManager.Object,
                _seoRedirectAutomationService.Object,
                new ValidationService(),
                new UpdateProductSeoDtoValidator(slugService),
                slugPolicyService ?? new StoreSeoSlugPolicyService(slugService, []),
                slugHistoryService ?? new NoopStoreSeoSlugHistoryService(),
                storeContext: new FixedStoreContext(storeId));
        }

        private static ServiceResponse<StoreSeoSlugHistoryDto> SlugHistorySuccess(
            Guid storeId,
            string entityType,
            Guid entityId,
            string slug)
        {
            return new ServiceResponse<StoreSeoSlugHistoryDto>(true, "Recorded", Guid.NewGuid())
            {
                ResponseType = ServiceResponseType.Success,
                Payload = new StoreSeoSlugHistoryDto(
                    Guid.NewGuid(),
                    storeId,
                    entityType,
                    entityId,
                    slug,
                    null,
                    true,
                    DateTimeOffset.UtcNow,
                    null,
                    null),
            };
        }

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ApplicationResult<Guid>(true, "Current store resolved.", this.storeId));
            }
        }
    }
}
