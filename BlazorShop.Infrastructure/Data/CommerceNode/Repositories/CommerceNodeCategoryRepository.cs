namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeCategoryRepository : ICategoryRepository
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeCategoryRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
        {
            var products = await this.context.Products
                .Include(product => product.Category)
                .Where(product => product.CategoryId == categoryId)
                .AsNoTracking()
                .ToListAsync();

            return products.Count > 0 ? products : [];
        }

        public async Task<IEnumerable<Category>> GetPublishedCategoriesAsync()
        {
            var categories = await this.context.Categories
                .AsNoTracking()
                .Where(category => category.IsPublished && category.Slug != null && category.Slug != string.Empty)
                .OrderBy(category => category.Name)
                .ToListAsync();

            return categories.Count > 0 ? categories : [];
        }

        public async Task<IReadOnlyList<PublishedCategorySitemapEntryReadModel>> GetPublishedCategorySitemapEntriesAsync()
        {
            return await this.context.Categories
                .AsNoTracking()
                .Where(category => category.IsPublished && category.Slug != null && category.Slug != string.Empty)
                .OrderBy(category => category.Name)
                .Select(category => new PublishedCategorySitemapEntryReadModel
                {
                    Slug = category.Slug!,
                    LastModifiedUtc = category.Products!
                        .Where(product => product.IsPublished
                            && product.PublishedOn != null
                            && product.Slug != null
                            && product.Slug != string.Empty)
                        .Max(product => product.PublishedOn),
                })
                .ToListAsync();
        }

        public async Task<Category?> GetPublishedCategoryByIdAsync(Guid id)
        {
            return await this.context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.Id == id
                    && category.IsPublished
                    && category.Slug != null
                    && category.Slug != string.Empty);
        }

        public async Task<Category?> GetPublishedCategoryBySlugAsync(string slug)
        {
            return await this.context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.IsPublished
                    && category.Slug == slug);
        }

        public async Task<bool> CategorySlugExistsAsync(string slug, Guid? excludedCategoryId = null)
        {
            return await this.context.Categories
                .AsNoTracking()
                .AnyAsync(category => category.Slug == slug
                    && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value));
        }
    }
}
