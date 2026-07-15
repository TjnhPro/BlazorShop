namespace BlazorShop.Tests.Application.Services
{
    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;

    using Moq;

    using Xunit;

    public class CategoryServiceTests
    {
        private readonly Mock<IGenericRepository<Category>> _mockGenericRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            this._mockGenericRepository = new Mock<IGenericRepository<Category>>();
            this._mockMapper = new Mock<IMapper>();
            this._mockCategoryRepository = new Mock<ICategoryRepository>();
            this._categoryService = new CategoryService(this._mockGenericRepository.Object, this._mockMapper.Object, this._mockCategoryRepository.Object);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnCategories()
        {
            // Arrange
            var categories = new List<Category> { new Category { Id = Guid.NewGuid(), Name = "Test Category" } };
            this._mockGenericRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(categories);
            this._mockMapper.Setup(m => m.Map<IEnumerable<GetCategory>>(It.IsAny<IEnumerable<Category>>())).Returns(new List<GetCategory> { new GetCategory { Id = categories[0].Id, Name = categories[0].Name } });

            // Act
            var result = await this._categoryService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnCategory()
        {
            // Arrange
            var category = new Category { Id = Guid.NewGuid(), Name = "Test Category" };
            this._mockGenericRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(category);
            this._mockMapper.Setup(m => m.Map<GetCategory>(It.IsAny<Category>())).Returns(new GetCategory { Id = category.Id, Name = category.Name });

            // Act
            var result = await this._categoryService.GetByIdAsync(category.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(category.Id, result.Id);
        }

        [Fact]
        public async Task AddAsync_ShouldReturnSuccessResponse()
        {
            // Arrange
            var createCategory = new CreateCategory { Name = "New Category" };
            this._mockGenericRepository.Setup(repo => repo.AddAsync(It.IsAny<Category>())).ReturnsAsync(1);
            this._mockMapper.Setup(m => m.Map<Category>(It.IsAny<CreateCategory>())).Returns(new Category { Name = createCategory.Name });

            // Act
            var result = await this._categoryService.AddAsync(createCategory);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Category added successfully", result.Message);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnSuccessResponse()
        {
            // Arrange
            var updateCategory = new UpdateCategory { Id = Guid.NewGuid(), Name = "Updated Category" };
            var existingCategory = new Category
            {
                Id = updateCategory.Id,
                Name = "Existing Category",
                Slug = "existing-category",
                RobotsIndex = true,
                RobotsFollow = true,
                IsPublished = true,
            };
            this._mockGenericRepository.Setup(repo => repo.GetByIdAsync(updateCategory.Id)).ReturnsAsync(existingCategory);
            this._mockGenericRepository.Setup(repo => repo.UpdateAsync(existingCategory)).ReturnsAsync(1);
            this._mockMapper.Setup(m => m.Map(updateCategory, existingCategory))
                .Callback<UpdateCategory, Category>((source, destination) => destination.Name = source.Name)
                .Returns(existingCategory);

            // Act
            var result = await this._categoryService.UpdateAsync(updateCategory);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Category updated successfully", result.Message);
            Assert.Equal("Updated Category", existingCategory.Name);
            Assert.Equal("existing-category", existingCategory.Slug);
            Assert.True(existingCategory.RobotsIndex);
            Assert.True(existingCategory.RobotsFollow);
            Assert.True(existingCategory.IsPublished);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnSuccessResponse()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            this._mockGenericRepository.Setup(repo => repo.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(1);

            // Act
            var result = await this._categoryService.DeleteAsync(categoryId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Category deleted successfully", result.Message);
        }

        [Fact]
        public async Task GetProductsByCategoryAsync_ShouldReturnProducts()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var products = new List<Product> { new Product { Id = Guid.NewGuid(), Name = "Test Product" } };
            this._mockCategoryRepository.Setup(repo => repo.GetProductsByCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(products);
            this._mockMapper.Setup(m => m.Map<IEnumerable<GetProduct>>(It.IsAny<IEnumerable<Product>>())).Returns(new List<GetProduct> { new GetProduct { Id = products[0].Id, Name = products[0].Name } });

            // Act
            var result = await this._categoryService.GetProductsByCategoryAsync(categoryId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryBelongsToDifferentCurrentStore_ReturnsNotFound()
        {
            var currentStoreId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            var updateCategory = new UpdateCategory { Id = Guid.NewGuid(), Name = "Updated Category" };
            var existingCategory = new Category { Id = updateCategory.Id, StoreId = otherStoreId, Name = "Existing Category" };
            var service = new CategoryService(
                this._mockGenericRepository.Object,
                this._mockMapper.Object,
                this._mockCategoryRepository.Object,
                storeContext: new FixedStoreContext(currentStoreId));

            this._mockGenericRepository
                .Setup(repo => repo.GetByIdAsync(updateCategory.Id))
                .ReturnsAsync(existingCategory);

            var result = await service.UpdateAsync(updateCategory);

            Assert.False(result.Success);
            Assert.Equal("Category not found", result.Message);
            this._mockMapper.Verify(mapper => mapper.Map(It.IsAny<UpdateCategory>(), It.IsAny<Category>()), Times.Never);
            this._mockGenericRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenCategoryBelongsToDifferentCurrentStore_ReturnsNotFound()
        {
            var currentStoreId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var existingCategory = new Category { Id = categoryId, StoreId = otherStoreId, Name = "Existing Category" };
            var service = new CategoryService(
                this._mockGenericRepository.Object,
                this._mockMapper.Object,
                this._mockCategoryRepository.Object,
                storeContext: new FixedStoreContext(currentStoreId));

            this._mockGenericRepository
                .Setup(repo => repo.GetByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var result = await service.DeleteAsync(categoryId);

            Assert.False(result.Success);
            Assert.Equal("Category not found", result.Message);
            this._mockGenericRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenParentBelongsToDifferentStore_ReturnsValidationFailure()
        {
            var storeId = Guid.NewGuid();
            var otherStoreId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var updateCategory = new UpdateCategory { Id = Guid.NewGuid(), ParentCategoryId = parentId, Name = "Updated Category" };
            var existingCategory = new Category { Id = updateCategory.Id, StoreId = storeId, Name = "Existing Category" };
            var parentCategory = new Category { Id = parentId, StoreId = otherStoreId, Name = "Other Parent" };
            var service = new CategoryService(
                this._mockGenericRepository.Object,
                this._mockMapper.Object,
                this._mockCategoryRepository.Object,
                storeContext: new FixedStoreContext(storeId));

            this._mockGenericRepository
                .Setup(repo => repo.GetByIdAsync(updateCategory.Id))
                .ReturnsAsync(existingCategory);
            this._mockMapper
                .Setup(mapper => mapper.Map(updateCategory, existingCategory))
                .Callback<UpdateCategory, Category>((source, destination) => destination.ParentCategoryId = source.ParentCategoryId)
                .Returns(existingCategory);
            this._mockGenericRepository
                .Setup(repo => repo.GetByIdAsync(parentId))
                .ReturnsAsync(parentCategory);

            var result = await service.UpdateAsync(updateCategory);

            Assert.False(result.Success);
            Assert.Equal("Parent category must belong to the same store.", result.Message);
            this._mockGenericRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        private sealed class FixedStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public FixedStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Current store resolved.", this.storeId));
            }
        }
    }
}
