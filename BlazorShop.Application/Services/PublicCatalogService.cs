namespace BlazorShop.Application.Services
{
    using System.Globalization;
    using System.Text;

    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;

    public class PublicCatalogService : IPublicCatalogService
    {
        private static readonly TimeSpan CategoryTreeCacheTtl = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan CatalogPageCacheTtl = TimeSpan.FromSeconds(45);

        private readonly ICategoryRepository _categoryRepository;
        private readonly ICatalogQueryCache? _catalogQueryCache;
        private readonly ICommerceStoreContext? _commerceStoreContext;
        private readonly IMapper _mapper;
        private readonly IProductReadRepository _productReadRepository;
        private readonly ISlugService _slugService;
        private readonly IStorefrontPageService? _storefrontPageService;

        public PublicCatalogService(
            ICategoryRepository categoryRepository,
            IMapper mapper,
            IProductReadRepository productReadRepository,
            ISlugService slugService,
            ICatalogQueryCache? catalogQueryCache = null,
            ICommerceStoreContext? commerceStoreContext = null,
            IStorefrontPageService? storefrontPageService = null)
        {
            _categoryRepository = categoryRepository;
            _catalogQueryCache = catalogQueryCache;
            _commerceStoreContext = commerceStoreContext;
            _mapper = mapper;
            _productReadRepository = productReadRepository;
            _slugService = slugService;
            _storefrontPageService = storefrontPageService;
        }

        public async Task<IEnumerable<GetCategory>> GetPublishedCategoriesAsync()
        {
            var categories = await _categoryRepository.GetPublishedCategoriesAsync();
            return categories.Any() ? _mapper.Map<IEnumerable<GetCategory>>(categories) : [];
        }

        public async Task<IReadOnlyList<GetCategoryTreeNode>> GetPublishedCategoryTreeAsync()
        {
            var storeId = await ResolveCurrentStoreIdAsync();
            var cacheKey = storeId.HasValue ? BuildCategoryTreeCacheKey(storeId.Value) : null;
            if (_catalogQueryCache is not null && cacheKey is not null)
            {
                var cached = await _catalogQueryCache.GetAsync<IReadOnlyList<GetCategoryTreeNode>>(cacheKey);
                if (cached is not null)
                {
                    return cached;
                }
            }

            var categories = await _categoryRepository.GetCategoriesForTreeAsync() ?? [];
            var publishedCategories = categories
                .Where(category => category.IsPublished
                    && category.ArchivedAt == null
                    && !string.IsNullOrWhiteSpace(category.Slug))
                .ToArray();

            var tree = BuildTree(publishedCategories);
            if (_catalogQueryCache is not null && cacheKey is not null)
            {
                await _catalogQueryCache.SetAsync(cacheKey, tree, CategoryTreeCacheTtl);
            }

            return tree;
        }

        public async Task<GetPublicCatalogSitemap> GetPublishedSitemapAsync()
        {
            var categories = await _categoryRepository.GetPublishedCategorySitemapEntriesAsync();
            var products = await _productReadRepository.GetPublishedProductSitemapEntriesAsync();
            var pages = await GetPublishedPageSitemapEntriesAsync(_storefrontPageService);

            return new GetPublicCatalogSitemap
            {
                Categories = categories
                    .Select(category => new GetCategorySitemapEntry
                    {
                        Slug = category.Slug,
                        LastModifiedUtc = category.LastModifiedUtc,
                    })
                    .ToArray(),
                Products = products
                    .Select(product => new GetProductSitemapEntry
                    {
                        Slug = product.Slug,
                        LastModifiedUtc = product.LastModifiedUtc,
                    })
                    .ToArray(),
                Pages = pages
                    .Select(page => new GetPageSitemapEntry
                    {
                        Slug = page.Slug,
                        LastModifiedUtc = page.UpdatedAt.UtcDateTime,
                    })
                    .ToArray(),
            };
        }

        private static async Task<IReadOnlyList<StorefrontPageSitemapEntryDto>> GetPublishedPageSitemapEntriesAsync(
            IStorefrontPageService? storefrontPageService)
        {
            if (storefrontPageService is null)
            {
                return [];
            }

            var result = await storefrontPageService.ListSitemapEntriesAsync();
            return result.Success && result.Payload is not null ? result.Payload : [];
        }

        public async Task<PagedResult<GetCatalogProduct>> GetPublishedCatalogPageAsync(ProductCatalogQuery query)
        {
            var storeId = await ResolveCurrentStoreIdAsync();
            var cacheKey = storeId.HasValue ? BuildCatalogPageCacheKey(storeId.Value, query) : null;
            if (_catalogQueryCache is not null && cacheKey is not null)
            {
                var cached = await _catalogQueryCache.GetAsync<PagedResult<GetCatalogProduct>>(cacheKey);
                if (cached is not null)
                {
                    return cached;
                }
            }

            var result = await _productReadRepository.GetPublishedCatalogPageAsync(query);
            var mappedItems = _mapper.Map<IReadOnlyList<GetCatalogProduct>>(result.Items);

            var page = new PagedResult<GetCatalogProduct>
            {
                Items = mappedItems,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
            };

            if (_catalogQueryCache is not null && cacheKey is not null)
            {
                await _catalogQueryCache.SetAsync(cacheKey, page, CatalogPageCacheTtl);
            }

            return page;
        }

        public async Task<ProductFilterMetadataReadModel> GetPublishedProductFilterMetadataAsync(ProductCatalogQuery query)
        {
            return await _productReadRepository.GetPublishedProductFilterMetadataAsync(query);
        }

        public async Task<IReadOnlyList<GetCatalogProduct>> GetPublishedSearchSuggestionsAsync(ProductCatalogQuery query, int limit)
        {
            var page = await this.GetPublishedCatalogPageAsync(new ProductCatalogQuery
            {
                PageNumber = 1,
                PageSize = Math.Clamp(limit, 1, CatalogSearchPolicy.SuggestionMaxLimit),
                CategoryId = query.CategoryId,
                CategorySlug = query.CategorySlug,
                IncludeSubcategories = query.IncludeSubcategories,
                SearchTerm = query.SearchTerm,
                InStock = query.InStock,
                SortBy = ProductCatalogSortBy.DisplayOrder,
            });

            return page.Items;
        }

        public async Task<GetProduct?> GetPublishedProductByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var product = await _productReadRepository.GetPublishedProductDetailsByIdAsync(id);
            return product is null ? null : MapProductDetails(product);
        }

        public async Task<GetProduct?> GetPublishedProductBySlugAsync(string slug)
        {
            var normalizedSlug = NormalizeSlug(slug);
            if (normalizedSlug is null)
            {
                return null;
            }

            var product = await _productReadRepository.GetPublishedProductBySlugAsync(normalizedSlug);
            return product is null ? null : MapProductDetails(product);
        }

        public async Task<GetCategory?> GetPublishedCategoryByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var category = await _categoryRepository.GetPublishedCategoryByIdAsync(id);
            return category is null ? null : _mapper.Map<GetCategory>(category);
        }

        public async Task<IReadOnlyList<GetCatalogProduct>> GetPublishedProductsByCategoryAsync(Guid categoryId)
        {
            if (categoryId == Guid.Empty)
            {
                return [];
            }

            var products = await _productReadRepository.GetPublishedProductsByCategoryAsync(categoryId);
            return products.Count > 0 ? _mapper.Map<IReadOnlyList<GetCatalogProduct>>(products) : [];
        }

        public async Task<GetCategoryPage?> GetPublishedCategoryPageBySlugAsync(string slug)
        {
            var normalizedSlug = NormalizeSlug(slug);
            if (normalizedSlug is null)
            {
                return null;
            }

            var category = await _categoryRepository.GetPublishedCategoryBySlugAsync(normalizedSlug);
            if (category is null)
            {
                return null;
            }

            var products = await _productReadRepository.GetPublishedProductsByCategoryAsync(category.Id);
            var categoriesForTree = await _categoryRepository.GetCategoriesForTreeAsync() ?? [];
            var publishedCategories = categoriesForTree
                .Where(candidate => candidate.IsPublished
                    && candidate.ArchivedAt == null
                    && !string.IsNullOrWhiteSpace(candidate.Slug))
                .ToArray();
            var descendantCategoryIds = CollectDescendantCategoryIds(category.Id, publishedCategories);
            var directProductCount = await _productReadRepository.CountPublishedProductsByCategoryIdsAsync([category.Id]);
            var descendantProductCount = await _productReadRepository.CountPublishedProductsByCategoryIdsAsync(descendantCategoryIds);

            return new GetCategoryPage
            {
                Category = _mapper.Map<GetCategory>(category),
                Breadcrumbs = BuildBreadcrumbs(category.Id, publishedCategories),
                Products = _mapper.Map<IReadOnlyList<GetCatalogProduct>>(products),
                DirectProductCount = directProductCount,
                DescendantProductCount = descendantProductCount,
            };
        }

        private string? NormalizeSlug(string slug)
        {
            var normalizedSlug = _slugService.NormalizeSlug(slug);
            return string.IsNullOrWhiteSpace(normalizedSlug) ? null : normalizedSlug;
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            if (_commerceStoreContext is null)
            {
                return null;
            }

            var result = await _commerceStoreContext.GetCurrentStoreIdAsync();
            return result.Success && result.Payload != Guid.Empty ? result.Payload : null;
        }

        private static string BuildCategoryTreeCacheKey(Guid storeId)
        {
            return $"store:{storeId:D}:catalog:categories:tree:v1";
        }

        private static string BuildCatalogPageCacheKey(Guid storeId, ProductCatalogQuery query)
        {
            var builder = new StringBuilder();
            builder.Append(CultureInfo.InvariantCulture, $"store:{storeId:D}:catalog:products:v2");
            builder.Append(CultureInfo.InvariantCulture, $":page:{Math.Max(1, query.PageNumber)}");
            builder.Append(CultureInfo.InvariantCulture, $":size:{Math.Max(1, query.PageSize)}");
            builder.Append(CultureInfo.InvariantCulture, $":sort:{query.SortBy}");
            builder.Append(CultureInfo.InvariantCulture, $":category-id:{query.CategoryId?.ToString("D") ?? "none"}");
            builder.Append(CultureInfo.InvariantCulture, $":category-slug:{NormalizeCachePart(query.GetNormalizedCategorySlug())}");
            builder.Append(CultureInfo.InvariantCulture, $":search:{NormalizeCachePart(CatalogSearchPolicy.NormalizeSearchTerm(query.SearchTerm))}");
            builder.Append(CultureInfo.InvariantCulture, $":min:{query.MinPrice?.ToString(CultureInfo.InvariantCulture) ?? "none"}");
            builder.Append(CultureInfo.InvariantCulture, $":max:{query.MaxPrice?.ToString(CultureInfo.InvariantCulture) ?? "none"}");
            builder.Append(CultureInfo.InvariantCulture, $":stock:{query.InStock?.ToString() ?? "none"}");
            builder.Append(CultureInfo.InvariantCulture, $":created:{query.CreatedAfterUtc?.ToString("O", CultureInfo.InvariantCulture) ?? "none"}");
            return builder.ToString();
        }

        private static string NormalizeCachePart(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "empty"
                : Uri.EscapeDataString(value.Trim().ToLowerInvariant());
        }

        private GetProduct MapProductDetails(Product product)
        {
            var mapped = _mapper.Map<GetProduct>(product);
            mapped.Variants = mapped.Variants
                .Where(variant => variant.IsActive)
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
                        option.ControlType,
                        option.IsRequired,
                        option.Values
                            .Where(value => value.IsActive)
                            .OrderBy(value => value.SortOrder)
                            .ThenBy(value => value.Value)
                            .Select(value => new StorefrontVariationValueDto(value.Value, value.ColorHex))
                            .ToArray()))
                    .Where(option => option.Values.Count > 0)
                    .ToArray());
        }

        private static IReadOnlyList<GetCategoryTreeNode> BuildTree(IReadOnlyList<Category> categories)
        {
            var nodes = categories
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .Select(category => new GetCategoryTreeNode
                {
                    Id = category.Id,
                    ParentCategoryId = category.ParentCategoryId,
                    Name = category.Name,
                    Slug = category.Slug,
                    Image = category.Image,
                    DisplayOrder = category.DisplayOrder,
                    IsPublished = category.IsPublished,
                })
                .ToDictionary(category => category.Id);

            var childrenByParent = nodes.Values
                .Where(category => category.ParentCategoryId.HasValue)
                .GroupBy(category => category.ParentCategoryId!.Value)
                .ToDictionary(group => group.Key, group => group.ToArray());

            foreach (var node in nodes.Values)
            {
                if (childrenByParent.TryGetValue(node.Id, out var children))
                {
                    node.Children = children;
                }
            }

            return nodes.Values
                .Where(category => !category.ParentCategoryId.HasValue || !nodes.ContainsKey(category.ParentCategoryId.Value))
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToArray();
        }

        private static IReadOnlyList<GetCategoryBreadcrumbItem> BuildBreadcrumbs(Guid categoryId, IReadOnlyList<Category> categories)
        {
            var byId = categories.ToDictionary(category => category.Id);
            var breadcrumbs = new List<GetCategoryBreadcrumbItem>();
            var visited = new HashSet<Guid>();
            var currentId = categoryId;

            while (byId.TryGetValue(currentId, out var category) && visited.Add(currentId))
            {
                breadcrumbs.Add(new GetCategoryBreadcrumbItem
                {
                    Id = category.Id,
                    Name = category.Name,
                    Slug = category.Slug,
                });

                if (!category.ParentCategoryId.HasValue)
                {
                    break;
                }

                currentId = category.ParentCategoryId.Value;
            }

            breadcrumbs.Reverse();
            return breadcrumbs;
        }

        private static IReadOnlyCollection<Guid> CollectDescendantCategoryIds(Guid rootCategoryId, IReadOnlyList<Category> categories)
        {
            var categoryIds = new HashSet<Guid> { rootCategoryId };
            var pending = new Queue<Guid>();
            pending.Enqueue(rootCategoryId);

            while (pending.Count > 0)
            {
                var currentId = pending.Dequeue();
                foreach (var child in categories.Where(category => category.ParentCategoryId == currentId))
                {
                    if (categoryIds.Add(child.Id))
                    {
                        pending.Enqueue(child.Id);
                    }
                }
            }

            return categoryIds;
        }
    }
}
