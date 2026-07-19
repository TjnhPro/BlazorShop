namespace BlazorShop.Application.Services
{
    using AutoMapper;
    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    public class ProductVariantService : IProductVariantService
    {
        private readonly IGenericRepository<ProductVariant> _variantRepository;
        private readonly IMapper _mapper;
        private readonly IProductReadRepository? _productReadRepository;
        private readonly ICommerceStoreContext? _storeContext;
        private readonly ICatalogQueryCache? _catalogQueryCache;

        public ProductVariantService(
            IGenericRepository<ProductVariant> variantRepository,
            IMapper mapper,
            IProductReadRepository? productReadRepository = null,
            ICommerceStoreContext? storeContext = null,
            ICatalogQueryCache? catalogQueryCache = null)
        {
            _variantRepository = variantRepository;
            _mapper = mapper;
            _productReadRepository = productReadRepository;
            _storeContext = storeContext;
            _catalogQueryCache = catalogQueryCache;
        }

        public async Task<IEnumerable<GetProductVariant>> GetByProductIdAsync(Guid productId)
        {
            var product = await GetProductForCurrentStoreAsync(productId);
            if (product is null)
            {
                return [];
            }

            var all = await _variantRepository.GetAllAsync();
            var data = all.Where(v => v.ProductId == productId).ToArray();
            return data.Length > 0
                ? data.Select(variant => MapVariant(variant, product.Price)).ToArray()
                : [];
        }

        public async Task<PagedResult<GetProductVariant>> QueryByProductIdAsync(Guid productId, int pageNumber = 1, int pageSize = 25)
        {
            var normalizedPageNumber = Math.Max(1, pageNumber);
            var normalizedPageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 100);
            var all = (await GetByProductIdAsync(productId))
                .OrderBy(variant => variant.DisplayName)
                .ThenBy(variant => variant.Sku)
                .ToArray();

            return new PagedResult<GetProductVariant>
            {
                Items = all.Skip((normalizedPageNumber - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray(),
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalCount = all.Length
            };
        }

        public async Task<ServiceResponse> AddAsync(CreateProductVariant variant)
        {
            var product = await GetProductForCurrentStoreAsync(variant.ProductId);
            if (product is null)
            {
                return new ServiceResponse(false, "Product not found");
            }

            var mapped = _mapper.Map<ProductVariant>(variant);
            NormalizeVariant(mapped);
            var validation = await ValidateVariantAsync(mapped, product);
            if (!validation.Success)
            {
                return validation;
            }

            var result = await _variantRepository.AddAsync(mapped);
            if (result <= 0)
            {
                return new ServiceResponse(false, "Variant not added");
            }

            await InvalidateCatalogAsync(product.StoreId);
            return new ServiceResponse(true, "Variant added successfully");
        }

        public async Task<ServiceResponse> UpdateAsync(UpdateProductVariant variant)
        {
            var existing = await _variantRepository.GetByIdAsync(variant.Id);
            if (existing is null)
            {
                return new ServiceResponse(false, "Variant not found");
            }

            if (existing.ProductId != variant.ProductId)
            {
                return new ServiceResponse(false, "Variant does not belong to the product.");
            }

            var product = await GetProductForCurrentStoreAsync(variant.ProductId);
            if (product is null)
            {
                return new ServiceResponse(false, "Product not found");
            }

            _mapper.Map(variant, existing);
            NormalizeVariant(existing);
            var validation = await ValidateVariantAsync(existing, product, existing.Id);
            if (!validation.Success)
            {
                return validation;
            }

            var result = await _variantRepository.UpdateAsync(existing);
            if (result <= 0)
            {
                return new ServiceResponse(false, "Variant not found");
            }

            await InvalidateCatalogAsync(product.StoreId);
            return new ServiceResponse(true, "Variant updated successfully");
        }

        public async Task<ServiceResponse> DeleteAsync(Guid variantId)
        {
            var existing = await _variantRepository.GetByIdAsync(variantId);
            if (existing is null)
            {
                return new ServiceResponse(false, "Variant not found");
            }

            var product = await GetProductForCurrentStoreAsync(existing.ProductId);
            if (product is null)
            {
                return new ServiceResponse(false, "Product not found");
            }

            var result = await _variantRepository.DeleteAsync(variantId);
            if (result <= 0)
            {
                return new ServiceResponse(false, "Variant not found");
            }

            await InvalidateCatalogAsync(product.StoreId);
            return new ServiceResponse(true, "Variant deleted successfully");
        }

        private async Task<ServiceResponse> ValidateVariantAsync(ProductVariant variant, Product product, Guid? excludedVariantId = null)
        {
            var all = await _variantRepository.GetAllAsync();
            var variants = all
                .Where(item => item.ProductId == variant.ProductId
                    && (!excludedVariantId.HasValue || item.Id != excludedVariantId.Value))
                .ToArray();

            if (!string.IsNullOrWhiteSpace(variant.AttributeSignature)
                && variants.Any(item => item.AttributeSignature == variant.AttributeSignature))
            {
                return new ServiceResponse(false, "Variant attribute combination already exists for this product.");
            }

            if (!string.IsNullOrWhiteSpace(variant.Sku)
                && variants.Any(item => string.Equals(item.Sku, variant.Sku, StringComparison.OrdinalIgnoreCase)))
            {
                return new ServiceResponse(false, "Variant SKU already exists for this product.");
            }

            if (variant.IsDefault && !variant.IsActive)
            {
                return new ServiceResponse(false, "Inactive variant cannot be the default variant.");
            }

            if (variant.IsDefault && variants.Any(item => item.IsDefault))
            {
                return new ServiceResponse(false, "Product already has a default variant.");
            }

            var templateValidation = ValidateTemplateAttributes(variant, product);
            if (!templateValidation.Success)
            {
                return templateValidation;
            }

            return new ServiceResponse(true, string.Empty);
        }

        private static ServiceResponse ValidateTemplateAttributes(ProductVariant variant, Product product)
        {
            if (!product.VariationTemplateId.HasValue && product.VariationTemplate is null)
            {
                return new ServiceResponse(true, string.Empty);
            }

            var template = product.VariationTemplate;
            if (template is null || !template.IsActive)
            {
                return new ServiceResponse(false, "Variation template is not available for this product.");
            }

            var options = template.Options
                .Where(option => option.IsActive)
                .ToDictionary(option => NormalizeAttributePart(option.Name), StringComparer.Ordinal);
            var attributes = ProductVariantAttributeNormalizer.Deserialize(variant.AttributesJson)
                .ToDictionary(attribute => NormalizeAttributePart(attribute.Name), StringComparer.Ordinal);

            foreach (var option in template.Options.Where(option => option.IsActive && option.IsRequired))
            {
                if (!attributes.ContainsKey(NormalizeAttributePart(option.Name)))
                {
                    return new ServiceResponse(false, $"Required variation option '{option.Name}' is missing.");
                }
            }

            foreach (var attribute in attributes.Values)
            {
                var optionKey = NormalizeAttributePart(attribute.Name);
                if (!options.TryGetValue(optionKey, out var option))
                {
                    return new ServiceResponse(false, $"Variation option '{attribute.Name}' is not available for this product.");
                }

                var valueKey = NormalizeAttributePart(attribute.Value);
                var valueAllowed = option.Values.Any(value =>
                    value.IsActive && string.Equals(NormalizeAttributePart(value.Value), valueKey, StringComparison.Ordinal));
                if (!valueAllowed)
                {
                    return new ServiceResponse(false, $"Variation value '{attribute.Value}' is not available for option '{option.Name}'.");
                }
            }

            return new ServiceResponse(true, string.Empty);
        }

        private static void NormalizeVariant(ProductVariant variant)
        {
            variant.Sku = string.IsNullOrWhiteSpace(variant.Sku) ? null : variant.Sku.Trim();
        }

        private static string NormalizeAttributePart(string value)
        {
            return value.Trim().ToLowerInvariant();
        }

        private async Task<Product?> GetProductForCurrentStoreAsync(Guid productId)
        {
            if (_productReadRepository is null)
            {
                return new Product { Id = productId };
            }

            var product = await _productReadRepository.GetProductDetailsByIdAsync(productId);
            if (product is null)
            {
                return null;
            }

            var storeId = await ResolveCurrentStoreIdAsync();
            return storeId.HasValue && product.StoreId != storeId.Value ? null : product;
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            if (_storeContext is null)
            {
                return null;
            }

            var result = await _storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Value : null;
        }

        private async Task InvalidateCatalogAsync(Guid? storeId)
        {
            if (_catalogQueryCache is null || !storeId.HasValue || storeId.Value == Guid.Empty)
            {
                return;
            }

            await _catalogQueryCache.InvalidateStoreCatalogAsync(storeId.Value);
        }

        private GetProductVariant MapVariant(ProductVariant variant, decimal productPrice)
        {
            var mapped = _mapper.Map<GetProductVariant>(variant);
            mapped.EffectivePrice = variant.Price ?? productPrice;
            return mapped;
        }
    }
}
