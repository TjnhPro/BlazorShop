namespace BlazorShop.Application.Services
{
    using System.Text.Json;

    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    public class ProductService : IProductService
    {
        private readonly IProductReadRepository _productReadRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IMapper _mapper;
        private readonly IAdminAuditService? _auditService;
        private readonly ICommerceStoreContext? _storeContext;
        private readonly ICatalogQueryCache? _catalogQueryCache;

        public ProductService(
            IProductReadRepository productReadRepository,
            IGenericRepository<Product> productRepository,
            IMapper mapper,
            IAdminAuditService? auditService = null,
            ICommerceStoreContext? storeContext = null,
            ICatalogQueryCache? catalogQueryCache = null)
        {
            _productReadRepository = productReadRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _auditService = auditService;
            _storeContext = storeContext;
            _catalogQueryCache = catalogQueryCache;
        }

        public async Task<IEnumerable<GetProduct>> GetAllAsync()
        {
            var result = await _productReadRepository.GetCatalogProductsAsync();

            var mappedData = _mapper.Map<IEnumerable<GetProduct>>(result);

            return result.Any() ? mappedData : [];
        }

        public async Task<PagedResult<GetCatalogProduct>> GetCatalogPageAsync(ProductCatalogQuery query)
        {
            var result = await _productReadRepository.GetCatalogPageAsync(query);
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
            var result = await _productReadRepository.GetProductDetailsByIdAsync(id);
            return result != null ? MapProductDetails(result) : null;
        }

        public async Task<ServiceResponse> AddAsync(CreateProduct product)
        {
            var mappedData = _mapper.Map<Product>(product);
            mappedData.StoreId ??= await ResolveCurrentStoreIdAsync();
            NormalizeProduct(mappedData);

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

            var storeId = existingProduct.StoreId;
            _mapper.Map(product, existingProduct);
            existingProduct.StoreId = storeId;
            NormalizeProduct(existingProduct);

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
            product.UpdatedAt = DateTime.UtcNow;
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
            if (_catalogQueryCache is null || !storeId.HasValue || storeId.Value == Guid.Empty)
            {
                return;
            }

            await _catalogQueryCache.InvalidateStoreCatalogAsync(storeId.Value);
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

            return mapped;
        }
    }
}
