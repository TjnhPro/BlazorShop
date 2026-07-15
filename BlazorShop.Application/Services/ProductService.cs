namespace BlazorShop.Application.Services
{
    using System.Text.Json;

    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;

    public class ProductService : IProductService
    {
        private readonly IProductReadRepository _productReadRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IMapper _mapper;
        private readonly IAdminAuditService? _auditService;
        private readonly ICommerceStoreContext? _storeContext;
        private readonly ICatalogQueryCache? _catalogQueryCache;
        private readonly IStorefrontNavigationCache? _navigationCache;
        private readonly IVariationTemplateLookupService? _variationTemplateLookupService;
        private readonly ICategoryRepository? _categoryRepository;

        public ProductService(
            IProductReadRepository productReadRepository,
            IGenericRepository<Product> productRepository,
            IMapper mapper,
            IAdminAuditService? auditService = null,
            ICommerceStoreContext? storeContext = null,
            ICatalogQueryCache? catalogQueryCache = null,
            IVariationTemplateLookupService? variationTemplateLookupService = null,
            ICategoryRepository? categoryRepository = null,
            IStorefrontNavigationCache? navigationCache = null)
        {
            _productReadRepository = productReadRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _auditService = auditService;
            _storeContext = storeContext;
            _catalogQueryCache = catalogQueryCache;
            _navigationCache = navigationCache;
            _variationTemplateLookupService = variationTemplateLookupService;
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<GetProduct>> GetAllAsync()
        {
            var result = _storeContext is not null
                ? await _productReadRepository.GetCatalogProductsForCurrentStoreAsync()
                : await _productReadRepository.GetCatalogProductsAsync();

            var mappedData = _mapper.Map<IEnumerable<GetProduct>>(result);

            return result.Any() ? mappedData : [];
        }

        public async Task<PagedResult<GetCatalogProduct>> GetCatalogPageAsync(ProductCatalogQuery query)
        {
            var result = _storeContext is not null
                ? await _productReadRepository.GetCatalogPageForCurrentStoreAsync(query)
                : await _productReadRepository.GetCatalogPageAsync(query);
            var mappedItems = _mapper.Map<IReadOnlyList<GetCatalogProduct>>(result.Items);

            return new PagedResult<GetCatalogProduct>
            {
                Items = mappedItems,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
            };
        }

        public async Task<GetProduct?> GetByIdAsync(Guid id)
        {
            var result = _storeContext is not null
                ? await _productReadRepository.GetProductDetailsByIdForCurrentStoreAsync(id)
                : await _productReadRepository.GetProductDetailsByIdAsync(id);
            return result != null ? MapProductDetails(result) : null;
        }

        public async Task<ServiceResponse> AddAsync(CreateProduct product)
        {
            var mappedData = _mapper.Map<Product>(product);
            mappedData.StoreId ??= await ResolveCurrentStoreIdAsync();
            NormalizeProduct(mappedData);

            var validation = await ValidateProductTypeAsync(mappedData);
            if (!validation.Success)
            {
                return validation;
            }

            validation = await ValidateProductCategoryAsync(mappedData);
            if (!validation.Success)
            {
                return validation;
            }

            if (!string.IsNullOrWhiteSpace(mappedData.Sku)
                && await _productReadRepository.ProductSkuExistsAsync(mappedData.Sku, mappedData.StoreId))
            {
                return new ServiceResponse(false, "Product SKU already exists for this store.");
            }

            int result = await _productRepository.AddAsync(mappedData);

            if (result <= 0)
            {
                return new ServiceResponse(false, "Product not added");
            }

            await LogAsync("Product.Created", mappedData.Id, $"Product {mappedData.Name} created.", new { mappedData.Name, mappedData.Price, mappedData.Quantity });
            await InvalidateCatalogAsync(mappedData.StoreId);
            return new ServiceResponse(true, "Product added successfully", mappedData.Id);
        }

        public async Task<ServiceResponse> UpdateAsync(UpdateProduct product)
        {
            var existingProduct = await _productRepository.GetByIdAsync(product.Id);

            if (existingProduct is null)
            {
                return new ServiceResponse(false, "Product not found");
            }

            if (!await ProductBelongsToCurrentStoreAsync(existingProduct))
            {
                return new ServiceResponse(false, "Product not found");
            }

            var storeId = existingProduct.StoreId;
            _mapper.Map(product, existingProduct);
            existingProduct.StoreId = storeId;
            NormalizeProduct(existingProduct);

            var validation = await ValidateProductTypeAsync(existingProduct);
            if (!validation.Success)
            {
                return validation;
            }

            validation = await ValidateProductCategoryAsync(existingProduct);
            if (!validation.Success)
            {
                return validation;
            }

            if (!string.IsNullOrWhiteSpace(existingProduct.Sku)
                && await _productReadRepository.ProductSkuExistsAsync(existingProduct.Sku, existingProduct.StoreId, existingProduct.Id))
            {
                return new ServiceResponse(false, "Product SKU already exists for this store.");
            }

            int result = await _productRepository.UpdateAsync(existingProduct);

            if (result <= 0)
            {
                return new ServiceResponse(false, "Product not found");
            }

            await LogAsync("Product.Updated", existingProduct.Id, $"Product {existingProduct.Name} updated.", new { existingProduct.Name, existingProduct.Price, existingProduct.Quantity });
            await InvalidateCatalogAsync(existingProduct.StoreId);
            return new ServiceResponse(true, "Product updated successfully");
        }

        public async Task<ServiceResponse> DeleteAsync(Guid id)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct is null)
            {
                return new ServiceResponse(false, "Product not found");
            }

            if (!await ProductBelongsToCurrentStoreAsync(existingProduct))
            {
                return new ServiceResponse(false, "Product not found");
            }

            existingProduct.ArchivedAt = DateTime.UtcNow;
            existingProduct.UpdatedAt = DateTime.UtcNow;
            existingProduct.IsPublished = false;
            var result = await _productRepository.UpdateAsync(existingProduct);

            if (result <= 0)
            {
                return new ServiceResponse(false, "Product not found");
            }

            await LogAsync("Product.Archived", id, $"Product {existingProduct.Name ?? id.ToString()} archived.", new { existingProduct.Name, existingProduct.Sku });
            await InvalidateCatalogAsync(existingProduct.StoreId);
            return new ServiceResponse(true, "Product archived successfully");
        }

        private static void NormalizeProduct(Product product)
        {
            product.Sku = string.IsNullOrWhiteSpace(product.Sku) ? null : product.Sku.Trim();
            product.Name = product.Name?.Trim();
            product.Description = product.Description?.Trim();
            product.ShortDescription = string.IsNullOrWhiteSpace(product.ShortDescription) ? null : product.ShortDescription.Trim();
            product.FullDescription = string.IsNullOrWhiteSpace(product.FullDescription)
                ? product.Description
                : product.FullDescription.Trim();
            product.Image = product.Image?.Trim();
            product.ProductType = NormalizeProductType(product.ProductType);
            if (!string.Equals(product.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase))
            {
                product.VariationTemplateId = null;
            }

            product.PublishedOn = product.IsPublished
                ? product.PublishedOn ?? DateTime.UtcNow
                : null;
            product.UpdatedAt = DateTime.UtcNow;
        }

        private async Task<ServiceResponse> ValidateProductTypeAsync(Product product)
        {
            if (!ProductTypes.All.Contains(product.ProductType))
            {
                return new ServiceResponse(false, "Product type is invalid.");
            }

            if (!string.Equals(product.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase))
            {
                return new ServiceResponse(true, string.Empty);
            }

            if (!product.VariationTemplateId.HasValue || product.VariationTemplateId.Value == Guid.Empty)
            {
                return new ServiceResponse(false, "Variation template is required for custom variation products.");
            }

            if (_variationTemplateLookupService is null)
            {
                return new ServiceResponse(true, string.Empty);
            }

            var isActive = await _variationTemplateLookupService.IsActiveTemplateInStoreAsync(
                product.VariationTemplateId.Value,
                product.StoreId);

            return isActive
                ? new ServiceResponse(true, string.Empty)
                : new ServiceResponse(false, "Variation template was not found or is inactive.");
        }

        private async Task<ServiceResponse> ValidateProductCategoryAsync(Product product)
        {
            if (!product.CategoryId.HasValue || product.CategoryId.Value == Guid.Empty || _categoryRepository is null)
            {
                return new ServiceResponse(true, string.Empty);
            }

            if (_storeContext is null)
            {
                return new ServiceResponse(true, string.Empty);
            }

            return await _categoryRepository.CategoryBelongsToCurrentStoreAsync(product.CategoryId.Value)
                ? new ServiceResponse(true, string.Empty)
                : new ServiceResponse(false, "Product category was not found for this store.");
        }

        private async Task<bool> ProductBelongsToCurrentStoreAsync(Product product)
        {
            if (_storeContext is null)
            {
                return true;
            }

            var storeResult = await _storeContext.GetCurrentStoreIdAsync();
            return storeResult.Success && product.StoreId == storeResult.Payload;
        }

        private static string NormalizeProductType(string? productType)
        {
            if (string.IsNullOrWhiteSpace(productType))
            {
                return ProductTypes.Simple;
            }

            var trimmed = productType.Trim();
            return ProductTypes.All.FirstOrDefault(type => string.Equals(type, trimmed, StringComparison.OrdinalIgnoreCase))
                   ?? trimmed;
        }

        private async Task LogAsync(string action, Guid entityId, string summary, object metadata)
        {
            if (_auditService is null)
            {
                return;
            }

            await _auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = "Product",
                EntityId = entityId.ToString(),
                Summary = summary,
                MetadataJson = JsonSerializer.Serialize(metadata),
            });
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            if (_storeContext is null)
            {
                return null;
            }

            var result = await _storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }

        private async Task InvalidateCatalogAsync(Guid? storeId)
        {
            if (!storeId.HasValue || storeId.Value == Guid.Empty)
            {
                return;
            }

            if (_catalogQueryCache is not null)
            {
                await _catalogQueryCache.InvalidateStoreCatalogAsync(storeId.Value);
            }

            _navigationCache?.Invalidate(storeId.Value);
        }

        private GetProduct MapProductDetails(Product product)
        {
            var mapped = _mapper.Map<GetProduct>(product);
            mapped.Variants = mapped.Variants
                .Select(variant =>
                {
                    variant.EffectivePrice = variant.Price ?? product.Price;
                    return variant;
                })
                .ToArray();
            mapped.VariationTemplate = MapStorefrontVariationTemplate(product);

            return mapped;
        }

        private static StorefrontVariationTemplateDto? MapStorefrontVariationTemplate(Product product)
        {
            if (!string.Equals(product.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase)
                || product.VariationTemplate is null
                || !product.VariationTemplate.IsActive)
            {
                return null;
            }

            return new StorefrontVariationTemplateDto(
                product.VariationTemplate.Name,
                product.VariationTemplate.Slug,
                product.VariationTemplate.Options
                    .Where(option => option.IsActive)
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.Name)
                    .Select(option => new StorefrontVariationOptionDto(
                        option.Name,
                        option.Values
                            .Where(value => value.IsActive)
                            .OrderBy(value => value.SortOrder)
                            .ThenBy(value => value.Value)
                            .Select(value => new StorefrontVariationValueDto(value.Value))
                            .ToArray()))
                    .Where(option => option.Values.Count > 0)
                    .ToArray());
        }
    }
}
