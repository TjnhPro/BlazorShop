namespace BlazorShop.Infrastructure.Repositories.CategoryPersistence
{
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Infrastructure.Data;

    using Microsoft.EntityFrameworkCore;

    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext dbContext)
        {
            _context = dbContext;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
        {
            var products = await _context
                               .Products
                               .Include(x => x.Category)
                               .Where(p => p.CategoryId == categoryId)
                               .AsNoTracking()
                               .ToListAsync();

            return products.Count > 0 ? products : [];
        }

        public async Task<IReadOnlyList<Category>> GetCategoriesForCurrentStoreAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(category => category.ArchivedAt == null)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetPublishedCategoriesAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(category => category.ArchivedAt == null
                    && category.IsPublished
                    && category.Slug != null
                    && category.Slug != string.Empty)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToListAsync();

            return categories.Count > 0 ? categories : [];
        }

        public async Task<IReadOnlyList<Category>> GetCategoriesForTreeAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(category => category.ArchivedAt == null)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<PublishedCategorySitemapEntryReadModel>> GetPublishedCategorySitemapEntriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(category => category.ArchivedAt == null
                    && category.IsPublished
                    && category.Slug != null
                    && category.Slug != string.Empty)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .Select(category => new PublishedCategorySitemapEntryReadModel
                {
                    Slug = category.Slug!,
                    LastModifiedUtc = category.UpdatedAt,
                })
                .ToListAsync();
        }

        public async Task<Category?> GetPublishedCategoryByIdAsync(Guid id)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.Id == id
                    && category.ArchivedAt == null
                    && category.IsPublished
                    && category.Slug != null
                    && category.Slug != string.Empty);
        }

        public async Task<Category?> GetPublishedCategoryBySlugAsync(string slug)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.IsPublished
                    && category.ArchivedAt == null
                    && category.Slug == slug);
        }

        public async Task<Category?> GetCategoryByIdForCurrentStoreAsync(Guid id)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.Id == id && category.ArchivedAt == null);
        }

        public async Task<bool> CategorySlugExistsAsync(string slug, Guid? excludedCategoryId = null)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(category => category.Slug == slug
                    && category.ArchivedAt == null
                    && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value));
        }

        public async Task<bool> CategorySlugExistsInStoreAsync(string slug, Guid? storeId, Guid? excludedCategoryId = null)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(category => category.Slug == slug
                    && category.StoreId == storeId
                    && category.ArchivedAt == null
                    && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value));
        }

        public async Task<bool> CategoryBelongsToCurrentStoreAsync(Guid id)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(category => category.Id == id && category.ArchivedAt == null);
        }

        public async Task<bool> HasActiveChildrenAsync(Guid id)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(category => category.ParentCategoryId == id && category.ArchivedAt == null);
        }

        public async Task<bool> HasActiveProductsAsync(Guid id)
        {
            return await _context.Products
                .AsNoTracking()
                .AnyAsync(product => product.CategoryId == id && product.ArchivedAt == null);
        }
    }
}
