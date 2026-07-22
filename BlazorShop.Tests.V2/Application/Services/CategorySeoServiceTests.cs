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
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Tests.TestUtilities;

    using Moq;

    using Xunit;

    public class CategorySeoServiceTests
    {
        private readonly Mock<IGenericRepository<Category>> _categoryRepository;
        private readonly Mock<ICategoryRepository> _categoryReadRepository;
        private readonly Mock<IApplicationTransactionManager> _transactionManager;
        private readonly Mock<ISeoRedirectAutomationService> _seoRedirectAutomationService;
        private readonly CategorySeoService _service;

        public CategorySeoServiceTests()
        {
            _categoryRepository = new Mock<IGenericRepository<Category>>();
            _categoryReadRepository = new Mock<ICategoryRepository>();
            _transactionManager = new Mock<IApplicationTransactionManager>();
            _seoRedirectAutomationService = new Mock<ISeoRedirectAutomationService>();

            _transactionManager
                .Setup(manager => manager.ExecuteInTransactionAsync(It.IsAny<Func<Task<ServiceResponse<CategorySeoDto>>>>()))
                .Returns((Func<Task<ServiceResponse<CategorySeoDto>>> action) => action());

            var slugService = new SlugService();
            _service = new CategorySeoService(
                _categoryRepository.Object,
                _categoryReadRepository.Object,
                AutoMapperTestFactory.CreateMapper(),
                slugService,
                _transactionManager.Object,
                _seoRedirectAutomationService.Object,
                new ValidationService(),
                new UpdateCategorySeoDtoValidator(slugService),
                new StoreSeoSlugPolicyService(slugService, []),
                new NoopStoreSeoSlugHistoryService());
        }

        [Fact]
        public async Task GetByCategoryIdAsync_WhenCategoryExists_ReturnsMappedSeoPayload()
        {
            var categoryId = Guid.NewGuid();
            var category = new Category
            {
                Id = categoryId,
                Slug = "mens-shoes",
                MetaTitle = "Men's Shoes",
                IsPublished = true,
            };

            _categoryRepository
                .Setup(repository => repository.GetByIdAsync(categoryId))
                .ReturnsAsync(category);

            var result = await _service.GetByCategoryIdAsync(categoryId);

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.Equal(categoryId, result.Payload!.CategoryId);
            Assert.Equal("mens-shoes", result.Payload.Slug);
        }

        [Fact]
        public async Task UpdateAsync_WhenPayloadIsValid_NormalizesSlugAndUpdatesEntityAndCreatesRedirect()
        {
            var categoryId = Guid.NewGuid();
            var existingCategory = new Category { Id = categoryId, Name = "Men", Slug = "old-slug", IsPublished = true };

            _categoryRepository
                .Setup(repository => repository.GetByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);
            _categoryReadRepository
                .Setup(repository => repository.CategorySlugExistsAsync("mens-sale", categoryId))
                .ReturnsAsync(false);
            _seoRedirectAutomationService
                .Setup(service => service.EnsurePermanentRedirectAsync("/category/old-slug", "/category/mens-sale"))
                .ReturnsAsync(new ServiceResponse<SeoRedirectDto>(true, "Created", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                    Payload = new SeoRedirectDto
                    {
                        OldPath = "/category/old-slug",
                        NewPath = "/category/mens-sale",
                        StatusCode = 301,
                        IsActive = true,
                    },
                });
            _categoryRepository
                .Setup(repository => repository.UpdateAsync(existingCategory))
                .ReturnsAsync(1);

            var result = await _service.UpdateAsync(categoryId, new UpdateCategorySeoDto
            {
                Slug = "Mens Sale",
                MetaTitle = "Men's Sale",
                IsPublished = true,
            });

            Assert.True(result.Success);
            Assert.Equal(ServiceResponseType.Success, result.ResponseType);
            Assert.Equal("mens-sale", existingCategory.Slug);
            Assert.Equal("Men's Sale", existingCategory.MetaTitle);
            _seoRedirectAutomationService.Verify(service => service.EnsurePermanentRedirectAsync("/category/old-slug", "/category/mens-sale"), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenSlugIsDuplicate_ReturnsConflict()
        {
            var categoryId = Guid.NewGuid();
            var existingCategory = new Category { Id = categoryId, Slug = "old-slug", IsPublished = true };

            _categoryRepository
                .Setup(repository => repository.GetByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);
            _categoryReadRepository
                .Setup(repository => repository.CategorySlugExistsAsync("mens-sale", categoryId))
                .ReturnsAsync(true);

            var result = await _service.UpdateAsync(categoryId, new UpdateCategorySeoDto
            {
                Slug = "Mens Sale",
                IsPublished = true,
            });

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("Category slug is already in use.", result.Message);
            _seoRedirectAutomationService.Verify(service => service.EnsurePermanentRedirectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenSlugIsUnchanged_DoesNotCreateRedirect()
        {
            var categoryId = Guid.NewGuid();
            var existingCategory = new Category { Id = categoryId, Slug = "mens-sale", IsPublished = true };

            _categoryRepository
                .Setup(repository => repository.GetByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);
            _categoryReadRepository
                .Setup(repository => repository.CategorySlugExistsAsync("mens-sale", categoryId))
                .ReturnsAsync(false);
            _categoryRepository
                .Setup(repository => repository.UpdateAsync(existingCategory))
                .ReturnsAsync(1);

            var result = await _service.UpdateAsync(categoryId, new UpdateCategorySeoDto
            {
                Slug = "mens-sale",
                IsPublished = true,
            });

            Assert.True(result.Success);
            _seoRedirectAutomationService.Verify(service => service.EnsurePermanentRedirectAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
        {
            var categoryId = Guid.NewGuid();

            _categoryRepository
                .Setup(repository => repository.GetByIdAsync(categoryId))
                .ReturnsAsync((Category?)null);

            var result = await _service.UpdateAsync(categoryId, new UpdateCategorySeoDto
            {
                Slug = "mens-sale",
                IsPublished = true,
            });

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal("Category not found.", result.Message);
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryIsOutsideCurrentStore_ReturnsNotFound()
        {
            var categoryId = Guid.NewGuid();
            var service = CreateStoreScopedService(Guid.NewGuid());

            _categoryReadRepository
                .Setup(repository => repository.GetCategoryByIdForCurrentStoreAsync(categoryId))
                .ReturnsAsync((Category?)null);

            var result = await service.UpdateAsync(categoryId, new UpdateCategorySeoDto
            {
                Slug = "valid-category-slug",
                IsPublished = true,
            });

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
            Assert.Equal("Category not found.", result.Message);
            _categoryRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenSlugExistsOnlyInAnotherStore_AllowsUpdate()
        {
            var storeId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var slugPolicy = new Mock<IStoreSeoSlugPolicyService>();
            var existingCategory = new Category
            {
                Id = categoryId,
                StoreId = storeId,
                Slug = "old-slug",
                IsPublished = true,
            };
            var service = CreateStoreScopedService(storeId, slugPolicy.Object);

            _categoryReadRepository
                .Setup(repository => repository.GetCategoryByIdForCurrentStoreAsync(categoryId))
                .ReturnsAsync(new Category
                {
                    Id = categoryId,
                    StoreId = storeId,
                    Slug = "old-slug",
                    IsPublished = true,
                });
            slugPolicy
                .Setup(service => service.ValidateSlugAsync(
                    SeoSlugEntityTypes.Category,
                    "shared-category",
                    storeId,
                    null,
                    categoryId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(StoreSeoSlugPolicyResult.Succeeded("shared-category"));
            _categoryRepository
                .Setup(repository => repository.GetByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);
            _categoryRepository
                .Setup(repository => repository.UpdateAsync(existingCategory))
                .ReturnsAsync(1);
            _seoRedirectAutomationService
                .Setup(service => service.EnsurePermanentRedirectAsync("/category/old-slug", "/category/shared-category"))
                .ReturnsAsync(new ServiceResponse<SeoRedirectDto>(true, "Created", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                    Payload = new SeoRedirectDto
                    {
                        OldPath = "/category/old-slug",
                        NewPath = "/category/shared-category",
                        StatusCode = 301,
                        IsActive = true,
                    },
                });

            var result = await service.UpdateAsync(categoryId, new UpdateCategorySeoDto
            {
                Slug = "shared-category",
                IsPublished = true,
            });

            Assert.True(result.Success);
            Assert.Equal("shared-category", existingCategory.Slug);
            slugPolicy.VerifyAll();
            _categoryReadRepository.Verify(
                repository => repository.CategorySlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenStoreScopedSlugChanges_UsesPolicyAndSlugHistory()
        {
            var storeId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var existingCategory = new Category
            {
                Id = categoryId,
                StoreId = storeId,
                Name = "Men",
                Slug = "old-category",
                IsPublished = true,
            };
            var slugPolicy = new Mock<IStoreSeoSlugPolicyService>();
            var slugHistory = new Mock<IStoreSeoSlugHistoryService>();
            var service = CreateStoreScopedService(storeId, slugPolicy.Object, slugHistory.Object);

            _categoryReadRepository
                .Setup(repository => repository.GetCategoryByIdForCurrentStoreAsync(categoryId))
                .ReturnsAsync(new Category
                {
                    Id = categoryId,
                    StoreId = storeId,
                    Slug = "old-category",
                    IsPublished = true,
                });
            _categoryRepository
                .Setup(repository => repository.GetByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);
            _categoryRepository
                .Setup(repository => repository.UpdateAsync(existingCategory))
                .ReturnsAsync(1);
            slugPolicy
                .Setup(service => service.ValidateSlugAsync(
                    SeoSlugEntityTypes.Category,
                    "new-category",
                    storeId,
                    null,
                    categoryId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(StoreSeoSlugPolicyResult.Succeeded("new-category"));
            slugHistory
                .Setup(service => service.GetActiveSlugAsync(
                    SeoSlugEntityTypes.Category,
                    categoryId,
                    storeId,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((StoreSeoSlugHistoryDto?)null);
            slugHistory
                .Setup(service => service.RecordInitialActiveSlugAsync(
                    SeoSlugEntityTypes.Category,
                    categoryId,
                    storeId,
                    "old-category",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(SlugHistorySuccess(storeId, SeoSlugEntityTypes.Category, categoryId, "old-category"));
            slugHistory
                .Setup(service => service.ReplaceActiveSlugAsync(
                    SeoSlugEntityTypes.Category,
                    categoryId,
                    storeId,
                    "new-category",
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(SlugHistorySuccess(storeId, SeoSlugEntityTypes.Category, categoryId, "new-category"));
            _seoRedirectAutomationService
                .Setup(service => service.EnsurePermanentRedirectAsync("/category/old-category", "/category/new-category"))
                .ReturnsAsync(new ServiceResponse<SeoRedirectDto>(true, "Created", Guid.NewGuid())
                {
                    ResponseType = ServiceResponseType.Success,
                    Payload = new SeoRedirectDto
                    {
                        OldPath = "/category/old-category",
                        NewPath = "/category/new-category",
                        StatusCode = 301,
                        IsActive = true,
                    },
                });

            var result = await service.UpdateAsync(categoryId, new UpdateCategorySeoDto
            {
                Slug = "New Category",
                IsPublished = true,
            });

            Assert.True(result.Success);
            Assert.Equal("new-category", existingCategory.Slug);
            slugPolicy.VerifyAll();
            slugHistory.VerifyAll();
        }

        private CategorySeoService CreateStoreScopedService(
            Guid storeId,
            IStoreSeoSlugPolicyService? slugPolicyService = null,
            IStoreSeoSlugHistoryService? slugHistoryService = null)
        {
            var slugService = new SlugService();
            return new CategorySeoService(
                _categoryRepository.Object,
                _categoryReadRepository.Object,
                AutoMapperTestFactory.CreateMapper(),
                slugService,
                _transactionManager.Object,
                _seoRedirectAutomationService.Object,
                new ValidationService(),
                new UpdateCategorySeoDtoValidator(slugService),
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
