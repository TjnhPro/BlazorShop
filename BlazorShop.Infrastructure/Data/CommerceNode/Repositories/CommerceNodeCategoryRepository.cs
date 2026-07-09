namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeCategoryRepository : ICategoryRepository
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeCategoryRepository(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
        {
            var scopedProducts = await this.GetCurrentStoreProductsAsync();
            var products = await scopedProducts
                .Include(product => product.Category)
                .Where(product => product.CategoryId == categoryId
                    && product.Category != null
                    && product.Category.StoreId == product.StoreId)
                .AsNoTracking()
                .ToListAsync();

            return products.Count > 0 ? products : [];
        }

        public async Task<IEnumerable<Category>> GetPublishedCategoriesAsync()
        {
            var scopedCategories = await this.GetCurrentStoreCategoriesAsync();
            var categories = await scopedCategories
                .AsNoTracking()
                .Where(category => category.IsPublished && category.Slug != null && category.Slug != string.Empty)
                .OrderBy(category => category.Name)
                .ToListAsync();

            return categories.Count > 0 ? categories : [];
        }

        public async Task<IReadOnlyList<PublishedCategorySitemapEntryReadModel>> GetPublishedCategorySitemapEntriesAsync()
        {
            var scopedCategories = await this.GetCurrentStoreCategoriesAsync();
            return await scopedCategories
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
            var scopedCategories = await this.GetCurrentStoreCategoriesAsync();
            return await scopedCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(category => category.Id == id
                    && category.IsPublished
                    && category.Slug != null
                    && category.Slug != string.Empty);
        }

        public async Task<Category?> GetPublishedCategoryBySlugAsync(string slug)
        {
            var scopedCategories = await this.GetCurrentStoreCategoriesAsync();
            return await scopedCategories
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

        private async Task<IQueryable<Category>> GetCurrentStoreCategoriesAsync()
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync();
            if (!storeResult.Success)
            {
                return this.context.Categories.Where(category => false);
            }

            var storeId = storeResult.Payload;
            return this.context.Categories
                .AsNoTracking()
                .Where(category => category.StoreId == storeId);
        }

        private async Task<IQueryable<Product>> GetCurrentStoreProductsAsync()
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync();
            if (!storeResult.Success)
            {
                return this.context.Products.Where(product => false);
            }

            var storeId = storeResult.Payload;
            return this.context.Products
                .AsNoTracking()
                .Where(product => product.StoreId == storeId);
        }
    }
}
