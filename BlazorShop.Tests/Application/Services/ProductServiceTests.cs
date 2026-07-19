namespace BlazorShop.Tests.Application.Services
{
    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;

    using Moq;

    using Xunit;

    public class ProductServiceTests
    {
        private readonly Mock<IProductReadRepository> _mockProductReadRepository;
        private readonly Mock<IGenericRepository<Product>> _mockProductRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            // Moq init
            this._mockProductReadRepository = new Mock<IProductReadRepository>();
            this._mockProductRepository = new Mock<IGenericRepository<Product>>();
            this._mockMapper = new Mock<IMapper>();

            // Create service
            this._productService = new ProductService(
                this._mockProductReadRepository.Object,
                this._mockProductRepository.Object,
                this._mockMapper.Object);
        }

        [Fact]
        public async Task GetAllAsync_WhenProductsExist_ShouldReturnMappedProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product1" },
                new Product { Id = Guid.NewGuid(), Name = "Product2" }
            };

            var mappedProducts = new List<GetProduct>
            {
                new GetProduct { Id = products[0].Id, Name = "Product1" },
                new GetProduct { Id = products[1].Id, Name = "Product2" }
            };

            // Moq config
            this._mockProductReadRepository.Setup(repo => repo.GetCatalogProductsAsync())
                                       .ReturnsAsync(products);

            this._mockMapper.Setup(mapper => mapper.Map<IEnumerable<GetProduct>>(products))
                       .Returns(mappedProducts);

            // Act
            var result = await this._productService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mappedProducts, result);
            this._mockProductReadRepository.Verify(repo => repo.GetCatalogProductsAsync(), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map<IEnumerable<GetProduct>>(products), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoProductsExist_ShouldReturnEmptyList()
        {
            // Arrange
            var products = new List<Product>();

            this._mockProductReadRepository.Setup(repo => repo.GetCatalogProductsAsync())
                                       .ReturnsAsync(products);

            this._mockMapper.Setup(mapper => mapper.Map<IEnumerable<GetProduct>>(products))
                       .Returns(new List<GetProduct>());

            // Act
            var result = await this._productService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            this._mockProductReadRepository.Verify(repo => repo.GetCatalogProductsAsync(), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map<IEnumerable<GetProduct>>(products), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProductExists_ShouldReturnMappedProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Product1" };
            var mappedProduct = new GetProduct { Id = productId, Name = "Product1" };
            this._mockProductReadRepository.Setup(repo => repo.GetProductDetailsByIdAsync(productId))
                                       .ReturnsAsync(product);
            this._mockMapper.Setup(mapper => mapper.Map<GetProduct>(product))
                       .Returns(mappedProduct);

            // Act
            var result = await this._productService.GetByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mappedProduct, result);
            this._mockProductReadRepository.Verify(repo => repo.GetProductDetailsByIdAsync(productId), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map<GetProduct>(product), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProductDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var productId = Guid.NewGuid();
            this._mockProductReadRepository.Setup(repo => repo.GetProductDetailsByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await this._productService.GetByIdAsync(productId);

            // Assert
            Assert.Null(result);
            this._mockProductReadRepository.Verify(repo => repo.GetProductDetailsByIdAsync(productId), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map<GetProduct>(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task GetCatalogPageAsync_WhenCatalogItemsExist_ShouldReturnMappedPage()
        {
            var query = new ProductCatalogQuery { PageNumber = 1, PageSize = 2, SortBy = ProductCatalogSortBy.Newest };
            var catalogItems = new List<CatalogProductReadModel>
            {
                new CatalogProductReadModel
                {
                    Id = Guid.NewGuid(),
                    Name = "Catalog Product 1",
                    Description = "Description 1",
                    Price = 20m,
                    Image = "/img/1.png",
                    CategoryId = Guid.NewGuid(),
                    CreatedOn = DateTime.UtcNow,
                    HasVariants = true,
                },
                new CatalogProductReadModel
                {
                    Id = Guid.NewGuid(),
                    Name = "Catalog Product 2",
                    Description = "Description 2",
                    Price = 30m,
                    Image = "/img/2.png",
                    CategoryId = Guid.NewGuid(),
                    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    HasVariants = false,
                }
            };
            var pagedCatalog = new PagedResult<CatalogProductReadModel>
            {
                Items = catalogItems,
                PageNumber = 1,
                PageSize = 2,
                TotalCount = 5,
            };
            var mappedItems = new List<GetCatalogProduct>
            {
                new GetCatalogProduct { Id = catalogItems[0].Id, Name = "Catalog Product 1", HasVariants = true },
                new GetCatalogProduct { Id = catalogItems[1].Id, Name = "Catalog Product 2", HasVariants = false },
            };

            this._mockProductReadRepository
                .Setup(repo => repo.GetCatalogPageAsync(query))
                .ReturnsAsync(pagedCatalog);
            this._mockMapper
                .Setup(mapper => mapper.Map<IReadOnlyList<GetCatalogProduct>>(catalogItems))
                .Returns(mappedItems);

            var result = await this._productService.GetCatalogPageAsync(query);

            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(mappedItems, result.Items);
            this._mockProductReadRepository.Verify(repo => repo.GetCatalogPageAsync(query), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map<IReadOnlyList<GetCatalogProduct>>(catalogItems), Times.Once);
        }


        [Fact]
        public async Task AddAsync_WhenProductIsAdded_ShouldReturnSuccessResponse()
        {
            // Arrange
            var product = new CreateProduct { Name = "Product1" };
            var mappedProduct = new Product { Name = "Product1" };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                       .Returns(mappedProduct);
            this._mockProductRepository.Setup(repo => repo.AddAsync(mappedProduct))
                                  .ReturnsAsync(1);

            // Act
            var result = await this._productService.AddAsync(product);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Product added successfully", result.Message);
            this._mockMapper.Verify(mapper => mapper.Map<Product>(product), Times.Once);
            this._mockProductRepository.Verify(repo => repo.AddAsync(mappedProduct), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenProductIsNotAdded_ShouldReturnFailureResponse()
        {
            // Arrange
            var product = new CreateProduct { Name = "Product1" };
            var mappedProduct = new Product { Name = "Product1" };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                       .Returns(mappedProduct);
            this._mockProductRepository.Setup(repo => repo.AddAsync(mappedProduct))
                                  .ReturnsAsync(0);

            // Act
            var result = await this._productService.AddAsync(product);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Product not added", result.Message);
            this._mockMapper.Verify(mapper => mapper.Map<Product>(product), Times.Once);
            this._mockProductRepository.Verify(repo => repo.AddAsync(mappedProduct), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenAvailabilityEndIsBeforeStart_ShouldReturnFailureResponse()
        {
            var product = new CreateProduct
            {
                Name = "Scheduled product",
                AvailableStartUtc = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                AvailableEndUtc = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc),
            };
            var mappedProduct = new Product
            {
                Name = product.Name,
                AvailableStartUtc = product.AvailableStartUtc,
                AvailableEndUtc = product.AvailableEndUtc,
            };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);

            var result = await this._productService.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal("Product availability end must be after availability start.", result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Theory]
        [InlineData(0, 1, null, "Minimum order quantity must be at least 1.")]
        [InlineData(1, 0, null, "Quantity step must be at least 1.")]
        [InlineData(5, 1, 4, "Maximum order quantity must be greater than or equal to minimum order quantity.")]
        public async Task AddAsync_WhenPurchaseQuantityRulesAreInvalid_ShouldReturnFailureResponse(
            int minOrderQuantity,
            int quantityStep,
            int? maxOrderQuantity,
            string expectedMessage)
        {
            var product = new CreateProduct
            {
                Name = "Product",
                MinOrderQuantity = minOrderQuantity,
                QuantityStep = quantityStep,
                MaxOrderQuantity = maxOrderQuantity,
            };
            var mappedProduct = new Product
            {
                Name = product.Name,
                MinOrderQuantity = product.MinOrderQuantity,
                QuantityStep = product.QuantityStep,
                MaxOrderQuantity = product.MaxOrderQuantity,
            };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);

            var result = await this._productService.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal(expectedMessage, result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenPurchasingDisabledReasonIsTooLong_ShouldReturnFailureResponse()
        {
            var product = new CreateProduct
            {
                Name = "Product",
                PurchasingDisabledReason = new string('x', ProductPurchaseConstraints.PurchasingDisabledReasonMaxLength + 1),
            };
            var mappedProduct = new Product
            {
                Name = product.Name,
                PurchasingDisabledReason = product.PurchasingDisabledReason,
            };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);

            var result = await this._productService.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal($"Purchasing disabled reason must be {ProductPurchaseConstraints.PurchasingDisabledReasonMaxLength} characters or fewer.", result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenConditionIsInvalid_ShouldReturnFailureResponse()
        {
            var product = new CreateProduct { Name = "Product", Condition = "damaged" };
            var mappedProduct = new Product { Name = product.Name, Condition = product.Condition };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);

            var result = await this._productService.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal("Product condition is invalid.", result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenProductTypeIsUnsupported_ShouldReturnFailureResponse()
        {
            var product = new CreateProduct { Name = "Product", ProductType = "Bundle" };
            var mappedProduct = new Product { Name = product.Name, ProductType = product.ProductType };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);

            var result = await this._productService.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal("Product type is invalid.", result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenDimensionIsNegative_ShouldReturnFailureResponse()
        {
            var product = new CreateProduct { Name = "Product", Width = -1m };
            var mappedProduct = new Product { Name = product.Name, Width = product.Width };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);

            var result = await this._productService.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal("Product dimensions cannot be negative.", result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenIdentityFieldsAreValid_NormalizesAndPersistsThem()
        {
            var product = new CreateProduct
            {
                Name = "Product",
                Gtin = " 0123456789012 ",
                Barcode = " BAR-1 ",
                ManufacturerPartNumber = " MPN-1 ",
                Condition = " NEW ",
                Weight = 1.25m,
                Length = 2.5m,
                Width = 3.5m,
                Height = 4.5m,
            };
            var mappedProduct = new Product
            {
                Name = product.Name,
                Gtin = product.Gtin,
                Barcode = product.Barcode,
                ManufacturerPartNumber = product.ManufacturerPartNumber,
                Condition = product.Condition,
                Weight = product.Weight,
                Length = product.Length,
                Width = product.Width,
                Height = product.Height,
            };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);
            this._mockProductRepository.Setup(repo => repo.AddAsync(mappedProduct))
                .ReturnsAsync(1);

            var result = await this._productService.AddAsync(product);

            Assert.True(result.Success);
            Assert.Equal("0123456789012", mappedProduct.Gtin);
            Assert.Equal("BAR-1", mappedProduct.Barcode);
            Assert.Equal("MPN-1", mappedProduct.ManufacturerPartNumber);
            Assert.Equal("new", mappedProduct.Condition);
            Assert.Equal(1.25m, mappedProduct.Weight);
            Assert.Equal(2.5m, mappedProduct.Length);
            Assert.Equal(3.5m, mappedProduct.Width);
            Assert.Equal(4.5m, mappedProduct.Height);
            this._mockProductRepository.Verify(repo => repo.AddAsync(mappedProduct), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenPurchaseMetadataIsValid_NormalizesAndPersistsIt()
        {
            var product = new CreateProduct
            {
                Name = "Product",
                MinOrderQuantity = 2,
                MaxOrderQuantity = 10,
                QuantityStep = 2,
                PurchasingDisabled = true,
                PurchasingDisabledReason = " Temporarily paused ",
                ManageStock = false,
                HideWhenOutOfStock = true,
                ShippingRequired = false,
                FreeShipping = true,
                ShippingSurcharge = 3.5m,
                DeliveryEstimateText = " Ships next week ",
            };
            var mappedProduct = new Product
            {
                Name = product.Name,
                MinOrderQuantity = product.MinOrderQuantity,
                MaxOrderQuantity = product.MaxOrderQuantity,
                QuantityStep = product.QuantityStep,
                PurchasingDisabled = product.PurchasingDisabled,
                PurchasingDisabledReason = product.PurchasingDisabledReason,
                ManageStock = product.ManageStock,
                HideWhenOutOfStock = product.HideWhenOutOfStock,
                ShippingRequired = product.ShippingRequired,
                FreeShipping = product.FreeShipping,
                ShippingSurcharge = product.ShippingSurcharge,
                DeliveryEstimateText = product.DeliveryEstimateText,
            };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);
            this._mockProductRepository.Setup(repo => repo.AddAsync(mappedProduct))
                .ReturnsAsync(1);

            var result = await this._productService.AddAsync(product);

            Assert.True(result.Success);
            Assert.Equal(2, mappedProduct.MinOrderQuantity);
            Assert.Equal(10, mappedProduct.MaxOrderQuantity);
            Assert.Equal(2, mappedProduct.QuantityStep);
            Assert.True(mappedProduct.PurchasingDisabled);
            Assert.Equal("Temporarily paused", mappedProduct.PurchasingDisabledReason);
            Assert.False(mappedProduct.ManageStock);
            Assert.True(mappedProduct.HideWhenOutOfStock);
            Assert.False(mappedProduct.ShippingRequired);
            Assert.True(mappedProduct.FreeShipping);
            Assert.Equal(3.5m, mappedProduct.ShippingSurcharge);
            Assert.Equal("Ships next week", mappedProduct.DeliveryEstimateText);
            this._mockProductRepository.Verify(repo => repo.AddAsync(mappedProduct), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenShippingSurchargeIsNegative_ReturnsFailure()
        {
            var product = new CreateProduct
            {
                Name = "Product",
                ShippingSurcharge = -0.01m,
            };
            var mappedProduct = new Product
            {
                Name = product.Name,
                ShippingSurcharge = product.ShippingSurcharge,
            };
            this._mockMapper.Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);

            var result = await this._productService.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal("Shipping surcharge cannot be negative.", result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenProductIsUpdated_ShouldReturnSuccessResponse()
        {
            // Arrange
            var storeId = Guid.NewGuid();
            var product = new UpdateProduct { Id = Guid.NewGuid(), Name = "Product1" };
            var existingProduct = new Product
            {
                Id = product.Id,
                StoreId = storeId,
                Name = "Existing Product",
                Slug = "existing-product",
                RobotsIndex = true,
                RobotsFollow = true,
                IsPublished = true,
            };
            var navigationCache = new Mock<IStorefrontNavigationCache>();
            var service = new ProductService(
                this._mockProductReadRepository.Object,
                this._mockProductRepository.Object,
                this._mockMapper.Object,
                navigationCache: navigationCache.Object);
            this._mockProductRepository.Setup(repo => repo.GetByIdAsync(product.Id))
                .ReturnsAsync(existingProduct);
            this._mockMapper.Setup(mapper => mapper.Map(product, existingProduct))
                .Callback<UpdateProduct, Product>((source, destination) => destination.Name = source.Name)
                .Returns(existingProduct);
            this._mockProductRepository.Setup(repo => repo.UpdateAsync(existingProduct))
                                  .ReturnsAsync(1);

            // Act
            var result = await service.UpdateAsync(product);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Product updated successfully", result.Message);
            Assert.Equal("Product1", existingProduct.Name);
            Assert.Equal("existing-product", existingProduct.Slug);
            Assert.True(existingProduct.RobotsIndex);
            Assert.True(existingProduct.RobotsFollow);
            Assert.True(existingProduct.IsPublished);
            this._mockProductRepository.Verify(repo => repo.GetByIdAsync(product.Id), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map(product, existingProduct), Times.Once);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(existingProduct), Times.Once);
            navigationCache.Verify(cache => cache.Invalidate(storeId), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenProductIsNotUpdated_ShouldReturnFailureResponse()
        {
            // Arrange
            var product = new UpdateProduct { Id = Guid.NewGuid(), Name = "Product1" };
            var existingProduct = new Product { Id = product.Id, Name = "Existing Product" };
            this._mockProductRepository.Setup(repo => repo.GetByIdAsync(product.Id))
                .ReturnsAsync(existingProduct);
            this._mockMapper.Setup(mapper => mapper.Map(product, existingProduct))
                .Returns(existingProduct);
            this._mockProductRepository.Setup(repo => repo.UpdateAsync(existingProduct))
                                  .ReturnsAsync(0);

            // Act
            var result = await this._productService.UpdateAsync(product);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Product not found", result.Message);
            this._mockProductRepository.Verify(repo => repo.GetByIdAsync(product.Id), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map(product, existingProduct), Times.Once);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(existingProduct), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenProductDoesNotExist_ShouldReturnFailureResponse()
        {
            // Arrange
            var product = new UpdateProduct { Id = Guid.NewGuid(), Name = "Product1" };
            this._mockProductRepository.Setup(repo => repo.GetByIdAsync(product.Id))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await this._productService.UpdateAsync(product);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Product not found", result.Message);
            this._mockProductRepository.Verify(repo => repo.GetByIdAsync(product.Id), Times.Once);
            this._mockMapper.Verify(mapper => mapper.Map(It.IsAny<UpdateProduct>(), It.IsAny<Product>()), Times.Never);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenProductIsDeleted_ShouldReturnSuccessResponse()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var existingProduct = new Product { Id = productId, Name = "Product1", IsPublished = true };
            this._mockProductRepository.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);
            this._mockProductRepository.Setup(repo => repo.UpdateAsync(existingProduct))
                .ReturnsAsync(1);

            // Act
            var result = await this._productService.DeleteAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Product archived successfully", result.Message);
            Assert.False(existingProduct.IsPublished);
            Assert.NotNull(existingProduct.ArchivedAt);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(existingProduct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenProductIsNotDeleted_ShouldReturnFailureResponse()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var existingProduct = new Product { Id = productId, Name = "Product1" };
            this._mockProductRepository.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);
            this._mockProductRepository.Setup(repo => repo.UpdateAsync(existingProduct))
                .ReturnsAsync(0);

            // Act
            var result = await this._productService.DeleteAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Product not found", result.Message);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(existingProduct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenProductDoesNotExist_ShouldReturnFailureResponse()
        {
            // Arrange
            var productId = Guid.NewGuid();
            this._mockProductRepository.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await this._productService.DeleteAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Product not found", result.Message);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenProductBelongsToDifferentCurrentStore_ReturnsNotFound()
        {
            var currentStoreId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            var product = new UpdateProduct { Id = Guid.NewGuid(), Name = "Cross Store" };
            var existingProduct = new Product { Id = product.Id, StoreId = otherStoreId, Name = "Existing" };
            var service = new ProductService(
                this._mockProductReadRepository.Object,
                this._mockProductRepository.Object,
                this._mockMapper.Object,
                storeContext: new FixedStoreContext(currentStoreId));

            this._mockProductRepository
                .Setup(repo => repo.GetByIdAsync(product.Id))
                .ReturnsAsync(existingProduct);

            var result = await service.UpdateAsync(product);

            Assert.False(result.Success);
            Assert.Equal("Product not found", result.Message);
            this._mockMapper.Verify(mapper => mapper.Map(It.IsAny<UpdateProduct>(), It.IsAny<Product>()), Times.Never);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenProductBelongsToDifferentCurrentStore_ReturnsNotFound()
        {
            var currentStoreId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var existingProduct = new Product { Id = productId, StoreId = otherStoreId, Name = "Existing" };
            var service = new ProductService(
                this._mockProductReadRepository.Object,
                this._mockProductRepository.Object,
                this._mockMapper.Object,
                storeContext: new FixedStoreContext(currentStoreId));

            this._mockProductRepository
                .Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);

            var result = await service.DeleteAsync(productId);

            Assert.False(result.Success);
            Assert.Equal("Product not found", result.Message);
            this._mockProductRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenCategoryBelongsToDifferentCurrentStore_ReturnsValidationFailure()
        {
            var currentStoreId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var categoryRepository = new Mock<ICategoryRepository>();
            var product = new CreateProduct { Name = "Product1", CategoryId = categoryId };
            var mappedProduct = new Product { Name = "Product1", CategoryId = categoryId };
            var service = new ProductService(
                this._mockProductReadRepository.Object,
                this._mockProductRepository.Object,
                this._mockMapper.Object,
                storeContext: new FixedStoreContext(currentStoreId),
                categoryRepository: categoryRepository.Object);

            this._mockMapper
                .Setup(mapper => mapper.Map<Product>(product))
                .Returns(mappedProduct);
            categoryRepository
                .Setup(repository => repository.CategoryBelongsToCurrentStoreAsync(categoryId))
                .ReturnsAsync(false);

            var result = await service.AddAsync(product);

            Assert.False(result.Success);
            Assert.Equal("Product category was not found for this store.", result.Message);
            this._mockProductRepository.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Never);
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
