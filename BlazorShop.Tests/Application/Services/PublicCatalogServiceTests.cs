namespace BlazorShop.Tests.Application.Services
{
    using AutoMapper;

    using BlazorShop.Domain.Constants;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Moq;

    using Xunit;

    public class PublicCatalogServiceTests
    {
        private readonly Mock<ICategoryRepository> _categoryRepository = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IProductReadRepository> _productReadRepository = new();
        private readonly Mock<ISlugService> _slugService = new();

        [Fact]
        public async Task GetPublishedProductBySlugAsync_NormalizesSlugBeforeLookup()
        {
            var product = new Product { Id = Guid.NewGuid(), Name = "Running Shoes", Slug = "running-shoes", IsPublished = true };
            var mappedProduct = new GetProduct { Id = product.Id, Name = product.Name, Slug = product.Slug };

            _slugService.Setup(service => service.NormalizeSlug("Running Shoes")).Returns("running-shoes");
            _productReadRepository.Setup(repository => repository.GetPublishedProductBySlugAsync("running-shoes")).ReturnsAsync(product);
            _mapper.Setup(mapper => mapper.Map<GetProduct>(product)).Returns(mappedProduct);

            var service = CreateService();

            var result = await service.GetPublishedProductBySlugAsync("Running Shoes");

            Assert.NotNull(result);
            Assert.Equal("running-shoes", result!.Slug);
            _productReadRepository.Verify(repository => repository.GetPublishedProductBySlugAsync("running-shoes"), Times.Once);
        }

        [Fact]
        public async Task GetPublishedProductBySlugAsync_MapsActiveVariationTemplateOptionsAndValues()
        {
            var storeId = Guid.NewGuid();
            var product = new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Name = "Custom Tee",
                Slug = "custom-tee",
                IsPublished = true,
                ProductType = ProductTypes.CustomVariations,
                VariationTemplate = new VariationTemplate
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    Name = "Tee options",
                    Slug = "tee-options",
                    IsActive = true,
                    Options =
                    {
                        new VariationTemplateOption
                        {
                            Name = "Color",
                            SortOrder = 1,
                            IsActive = true,
                            ControlType = VariationControlTypes.Color,
                            IsRequired = true,
                            Values =
                            {
                                new VariationTemplateValue { Value = "Red", SortOrder = 1, IsActive = true, ColorHex = "#FF0000" },
                                new VariationTemplateValue { Value = "Blue", SortOrder = 2, IsActive = false },
                            },
                        },
                        new VariationTemplateOption
                        {
                            Name = "Hidden",
                            SortOrder = 2,
                            IsActive = false,
                            Values =
                            {
                                new VariationTemplateValue { Value = "Secret", SortOrder = 1, IsActive = true },
                            },
                        },
                    },
                },
            };
            var mappedProduct = new GetProduct { Id = product.Id, Name = product.Name, Slug = product.Slug };

            _slugService.Setup(service => service.NormalizeSlug("Custom Tee")).Returns("custom-tee");
            _productReadRepository.Setup(repository => repository.GetPublishedProductBySlugAsync("custom-tee")).ReturnsAsync(product);
            _mapper.Setup(mapper => mapper.Map<GetProduct>(product)).Returns(mappedProduct);

            var service = CreateService();

            var result = await service.GetPublishedProductBySlugAsync("Custom Tee");

            Assert.NotNull(result?.VariationTemplate);
            Assert.Equal("Tee options", result!.VariationTemplate!.Name);
            var option = Assert.Single(result.VariationTemplate.Options);
            Assert.Equal("Color", option.Name);
            Assert.Equal(VariationControlTypes.Color, option.ControlType);
            Assert.True(option.IsRequired);
            var value = Assert.Single(option.Values);
            Assert.Equal("Red", value.Value);
            Assert.Equal("#FF0000", value.ColorHex);
        }

        [Fact]
        public async Task GetPublishedProductBySlugAsync_ExposesOnlyActiveVariants()
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Variant product",
                Slug = "variant-product",
                IsPublished = true,
                ProductType = ProductTypes.VariantInventory,
                Variants =
                {
                    new ProductVariant { Id = Guid.NewGuid(), Sku = "ACTIVE", IsActive = true, Stock = 5 },
                    new ProductVariant { Id = Guid.NewGuid(), Sku = "INACTIVE", IsActive = false, Stock = 5 },
                },
            };
            var mappedProduct = new GetProduct
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Variants =
                [
                    new GetProductVariant { Id = product.Variants.ElementAt(0).Id, ProductId = product.Id, Sku = "ACTIVE", IsActive = true },
                    new GetProductVariant { Id = product.Variants.ElementAt(1).Id, ProductId = product.Id, Sku = "INACTIVE", IsActive = false },
                ],
            };

            _slugService.Setup(service => service.NormalizeSlug("Variant Product")).Returns("variant-product");
            _productReadRepository.Setup(repository => repository.GetPublishedProductBySlugAsync("variant-product")).ReturnsAsync(product);
            _mapper.Setup(mapper => mapper.Map<GetProduct>(product)).Returns(mappedProduct);

            var service = CreateService();

            var result = await service.GetPublishedProductBySlugAsync("Variant Product");

            Assert.NotNull(result);
            var variant = Assert.Single(result!.Variants);
            Assert.Equal("ACTIVE", variant.Sku);
        }

        [Fact]
        public async Task GetPublishedCategoryPageBySlugAsync_ReturnsCategoryAndProducts()
        {
            var category = new Category { Id = Guid.NewGuid(), Name = "Shoes", Slug = "shoes", IsPublished = true };
            var products = new List<CatalogProductReadModel>
            {
                new() { Id = Guid.NewGuid(), Name = "Running Shoes", Slug = "running-shoes", CategoryId = category.Id },
            };
            var mappedCategory = new GetCategory { Id = category.Id, Name = category.Name, Slug = category.Slug };
            var mappedProducts = new List<GetCatalogProduct>
            {
                new() { Id = products[0].Id, Name = products[0].Name, Slug = products[0].Slug, CategoryId = category.Id },
            };

            _slugService.Setup(service => service.NormalizeSlug("Shoes")).Returns("shoes");
            _categoryRepository.Setup(repository => repository.GetPublishedCategoryBySlugAsync("shoes")).ReturnsAsync(category);
            _productReadRepository.Setup(repository => repository.GetPublishedProductsByCategoryAsync(category.Id)).ReturnsAsync(products);
            _mapper.Setup(mapper => mapper.Map<GetCategory>(category)).Returns(mappedCategory);
            _mapper.Setup(mapper => mapper.Map<IReadOnlyList<GetCatalogProduct>>(products)).Returns(mappedProducts);

            var service = CreateService();

            var result = await service.GetPublishedCategoryPageBySlugAsync("Shoes");

            Assert.NotNull(result);
            Assert.Equal("shoes", result!.Category.Slug);
            Assert.Single(result.Products);
            Assert.Equal("running-shoes", result.Products[0].Slug);
        }

        [Fact]
        public async Task GetPublishedCategoryPageBySlugAsync_AddsBreadcrumbsAndProductCounts()
        {
            var rootCategory = new Category { Id = Guid.NewGuid(), Name = "Apparel", Slug = "apparel", IsPublished = true };
            var childCategory = new Category { Id = Guid.NewGuid(), ParentCategoryId = rootCategory.Id, Name = "Shoes", Slug = "shoes", IsPublished = true };
            var grandChildCategory = new Category { Id = Guid.NewGuid(), ParentCategoryId = childCategory.Id, Name = "Running", Slug = "running", IsPublished = true };
            var hiddenChild = new Category { Id = Guid.NewGuid(), ParentCategoryId = childCategory.Id, Name = "Hidden", Slug = "hidden", IsPublished = false };
            var mappedCategory = new GetCategory { Id = childCategory.Id, Name = childCategory.Name, Slug = childCategory.Slug };

            _slugService.Setup(service => service.NormalizeSlug("Shoes")).Returns("shoes");
            _categoryRepository.Setup(repository => repository.GetPublishedCategoryBySlugAsync("shoes")).ReturnsAsync(childCategory);
            _categoryRepository.Setup(repository => repository.GetCategoriesForTreeAsync()).ReturnsAsync(
            [
                rootCategory,
                childCategory,
                grandChildCategory,
                hiddenChild,
            ]);
            _productReadRepository.Setup(repository => repository.GetPublishedProductsByCategoryAsync(childCategory.Id)).ReturnsAsync([]);
            _productReadRepository
                .Setup(repository => repository.CountPublishedProductsByCategoryIdsAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(childCategory.Id))))
                .ReturnsAsync(2);
            _productReadRepository
                .Setup(repository => repository.CountPublishedProductsByCategoryIdsAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2
                        && ids.Contains(childCategory.Id)
                        && ids.Contains(grandChildCategory.Id))))
                .ReturnsAsync(5);
            _mapper.Setup(mapper => mapper.Map<GetCategory>(childCategory)).Returns(mappedCategory);
            _mapper.Setup(mapper => mapper.Map<IReadOnlyList<GetCatalogProduct>>(It.IsAny<IReadOnlyList<CatalogProductReadModel>>())).Returns([]);

            var service = CreateService();

            var result = await service.GetPublishedCategoryPageBySlugAsync("Shoes");

            Assert.NotNull(result);
            Assert.Equal(2, result!.DirectProductCount);
            Assert.Equal(5, result.DescendantProductCount);
            Assert.Equal(["Apparel", "Shoes"], result.Breadcrumbs.Select(crumb => crumb.Name ?? string.Empty).ToArray());
            Assert.DoesNotContain(result.Breadcrumbs, crumb => crumb.Name == "Hidden");
        }

        [Fact]
        public async Task GetPublishedCategoryPageBySlugAsync_ReturnsNullWhenCategoryIsMissing()
        {
            _slugService.Setup(service => service.NormalizeSlug("Missing Category")).Returns("missing-category");
            _categoryRepository.Setup(repository => repository.GetPublishedCategoryBySlugAsync("missing-category")).ReturnsAsync((Category?)null);

            var service = CreateService();

            var result = await service.GetPublishedCategoryPageBySlugAsync("Missing Category");

            Assert.Null(result);
            _productReadRepository.Verify(repository => repository.GetPublishedProductsByCategoryAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetPublishedSitemapAsync_LoadsRepositoriesSequentially()
        {
            var categoryFinished = false;
            var productStartedBeforeCategoryFinished = false;
            var categoryLastModified = new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc);
            var productLastModified = new DateTime(2026, 7, 9, 1, 0, 0, DateTimeKind.Utc);

            _categoryRepository
                .Setup(repository => repository.GetPublishedCategorySitemapEntriesAsync())
                .Returns(async () =>
                {
                    await Task.Yield();
                    categoryFinished = true;

                    return
                    [
                        new PublishedCategorySitemapEntryReadModel
                        {
                            Slug = "qa-category",
                            LastModifiedUtc = categoryLastModified,
                        },
                    ];
                });

            _productReadRepository
                .Setup(repository => repository.GetPublishedProductSitemapEntriesAsync())
                .ReturnsAsync(() =>
                {
                    productStartedBeforeCategoryFinished = !categoryFinished;

                    return
                    [
                        new PublishedProductSitemapEntryReadModel
                        {
                            Slug = "qa-product",
                            LastModifiedUtc = productLastModified,
                        },
                    ];
                });

            var service = CreateService();

            var result = await service.GetPublishedSitemapAsync();

            Assert.False(productStartedBeforeCategoryFinished);
            Assert.Single(result.Categories);
            Assert.Equal("qa-category", result.Categories[0].Slug);
            Assert.Single(result.Products);
            Assert.Equal("qa-product", result.Products[0].Slug);
        }

        private PublicCatalogService CreateService()
        {
            return new PublicCatalogService(
                _categoryRepository.Object,
                _mapper.Object,
                _productReadRepository.Object,
                _slugService.Object);
        }
    }
}
