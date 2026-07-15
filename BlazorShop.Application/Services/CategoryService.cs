namespace BlazorShop.Application.Services
{
    using System.Text.Json;

    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;

    public class CategoryService : ICategoryService
    {
        private readonly IGenericRepository<Category> _genericRepository;
        private readonly IMapper _mapper;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAdminAuditService? _auditService;
        private readonly ICommerceStoreContext? _storeContext;
        private readonly ICatalogQueryCache? _catalogQueryCache;

        public CategoryService(
            IGenericRepository<Category> genericRepository,
            IMapper mapper,
            ICategoryRepository categoryRepository,
            IAdminAuditService? auditService = null,
            ICommerceStoreContext? storeContext = null,
            ICatalogQueryCache? catalogQueryCache = null)
        {
            _genericRepository = genericRepository;
            _mapper = mapper;
            _categoryRepository = categoryRepository;
            _auditService = auditService;
            _storeContext = storeContext;
            _catalogQueryCache = catalogQueryCache;
        }

        public async Task<IEnumerable<GetCategory>> GetAllAsync()
        {
            var result = _storeContext is not null
                ? await _categoryRepository.GetCategoriesForCurrentStoreAsync()
                : await _genericRepository.GetAllAsync();
            return result.Any() ? _mapper.Map<IEnumerable<GetCategory>>(result) : new List<GetCategory>();
        }

        public async Task<PagedResult<GetCategory>> QueryAsync(int pageNumber = 1, int pageSize = 25)
        {
            var normalizedPageNumber = Math.Max(1, pageNumber);
            var normalizedPageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 100);
            var all = (await GetAllAsync()).OrderBy(category => category.Name).ToArray();

            return new PagedResult<GetCategory>
            {
                Items = all.Skip((normalizedPageNumber - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray(),
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalCount = all.Length
            };
        }

        public async Task<IReadOnlyList<GetCategoryTreeNode>> GetTreeAsync()
        {
            var categories = await _categoryRepository.GetCategoriesForTreeAsync();
            return BuildTree(categories);
        }

        public async Task<GetCategory> GetByIdAsync(Guid id)
        {
            var result = _storeContext is not null
                ? await _categoryRepository.GetCategoryByIdForCurrentStoreAsync(id)
                : await _genericRepository.GetByIdAsync(id);
            return result != null ? _mapper.Map<GetCategory>(result) : new GetCategory();
        }

        public async Task<ServiceResponse> AddAsync(CreateCategory category)
        {
            var entity = _mapper.Map<Category>(category);
            entity.StoreId ??= await ResolveCurrentStoreIdAsync();

            var validation = await ValidateParentAsync(entity.Id, entity.ParentCategoryId, entity.StoreId);
            if (!validation.Success)
            {
                return validation;
            }

            NormalizeCategory(entity);
            int result = await _genericRepository.AddAsync(entity);

            if (result <= 0)
            {
                return new ServiceResponse(false, "Category not added");
            }

            await LogAsync("Category.Created", entity.Id, $"Category {entity.Name} created.", new { entity.Name, entity.ParentCategoryId, entity.DisplayOrder });
            await InvalidateCatalogAsync(entity.StoreId);
            return new ServiceResponse(true, "Category added successfully");
        }

        public async Task<ServiceResponse> UpdateAsync(UpdateCategory category)
        {
            var existingCategory = await _genericRepository.GetByIdAsync(category.Id);

            if (existingCategory is null)
            {
                return new ServiceResponse(false, "Category not found");
            }

            if (!await CategoryBelongsToCurrentStoreAsync(existingCategory))
            {
                return new ServiceResponse(false, "Category not found");
            }

            var storeId = existingCategory.StoreId;
            _mapper.Map(category, existingCategory);
            existingCategory.StoreId = storeId;

            var validation = await ValidateParentAsync(existingCategory.Id, existingCategory.ParentCategoryId, existingCategory.StoreId);
            if (!validation.Success)
            {
                return validation;
            }

            NormalizeCategory(existingCategory);
            int result = await _genericRepository.UpdateAsync(existingCategory);

            if (result <= 0)
            {
                return new ServiceResponse(false, "Category not found");
            }

            await LogAsync("Category.Updated", existingCategory.Id, $"Category {existingCategory.Name} updated.", new { existingCategory.Name, existingCategory.ParentCategoryId, existingCategory.DisplayOrder });
            await InvalidateCatalogAsync(existingCategory.StoreId);
            return new ServiceResponse(true, "Category updated successfully");
        }

        public async Task<ServiceResponse> DeleteAsync(Guid id)
        {
            var existingCategory = await _genericRepository.GetByIdAsync(id);
            if (existingCategory is null)
            {
                return new ServiceResponse(false, "Category not found");
            }

            if (!await CategoryBelongsToCurrentStoreAsync(existingCategory))
            {
                return new ServiceResponse(false, "Category not found");
            }

            if (await _categoryRepository.HasActiveChildrenAsync(id))
            {
                return new ServiceResponse(false, "Category has active child categories.");
            }

            if (await _categoryRepository.HasActiveProductsAsync(id))
            {
                return new ServiceResponse(false, "Category has active products.");
            }

            existingCategory.ArchivedAt = DateTime.UtcNow;
            existingCategory.UpdatedAt = DateTime.UtcNow;
            existingCategory.IsPublished = false;
            var result = await _genericRepository.UpdateAsync(existingCategory);

            if (result <= 0)
            {
                return new ServiceResponse(false, "Category not found");
            }

            await LogAsync("Category.Archived", id, $"Category {existingCategory.Name ?? id.ToString()} archived.", new { existingCategory.Name });
            await InvalidateCatalogAsync(existingCategory.StoreId);
            return new ServiceResponse(true, "Category archived successfully");
        }

        public async Task<IEnumerable<GetProduct>> GetProductsByCategoryAsync(Guid id)
        {
            var result = await _categoryRepository.GetProductsByCategoryAsync(id);
            return result.Any() ? _mapper.Map<IEnumerable<GetProduct>>(result) : [];
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
                EntityType = "Category",
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

        private async Task<bool> CategoryBelongsToCurrentStoreAsync(Category category)
        {
            if (_storeContext is null)
            {
                return true;
            }

            var storeResult = await _storeContext.GetCurrentStoreIdAsync();
            return storeResult.Success && category.StoreId == storeResult.Payload;
        }

        private async Task<ServiceResponse> ValidateParentAsync(Guid categoryId, Guid? parentCategoryId, Guid? storeId)
        {
            if (!parentCategoryId.HasValue)
            {
                return new ServiceResponse(true, string.Empty);
            }

            if (parentCategoryId.Value == categoryId)
            {
                return new ServiceResponse(false, "Category cannot be its own parent.");
            }

            var parent = await _genericRepository.GetByIdAsync(parentCategoryId.Value);
            if (parent is null || parent.ArchivedAt != null)
            {
                return new ServiceResponse(false, "Parent category not found.");
            }

            if (parent.StoreId != storeId)
            {
                return new ServiceResponse(false, "Parent category must belong to the same store.");
            }

            var categories = (await _genericRepository.GetAllAsync())
                .Where(category => category.StoreId == storeId && category.ArchivedAt == null)
                .ToDictionary(category => category.Id);
            var currentParentId = parent.ParentCategoryId;

            while (currentParentId.HasValue)
            {
                if (currentParentId.Value == categoryId)
                {
                    return new ServiceResponse(false, "Category parent chain cannot be circular.");
                }

                if (!categories.TryGetValue(currentParentId.Value, out var currentParent))
                {
                    break;
                }

                currentParentId = currentParent.ParentCategoryId;
            }

            return new ServiceResponse(true, string.Empty);
        }

        private static void NormalizeCategory(Category category)
        {
            category.Name = category.Name?.Trim();
            category.Image = string.IsNullOrWhiteSpace(category.Image) ? null : category.Image.Trim();
            category.UpdatedAt = DateTime.UtcNow;
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
                    Slug = category.IsPublished ? category.Slug : null,
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
    }
}
