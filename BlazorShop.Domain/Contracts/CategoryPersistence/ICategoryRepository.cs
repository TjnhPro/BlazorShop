namespace BlazorShop.Domain.Contracts.CategoryPersistence
{
    using BlazorShop.Domain.Entities;

    public interface ICategoryRepository
    {
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId);

        Task<IEnumerable<Category>> GetPublishedCategoriesAsync();

        Task<IReadOnlyList<Category>> GetCategoriesForTreeAsync();

        Task<IReadOnlyList<PublishedCategorySitemapEntryReadModel>> GetPublishedCategorySitemapEntriesAsync();

        Task<Category?> GetPublishedCategoryByIdAsync(Guid id);

        Task<Category?> GetPublishedCategoryBySlugAsync(string slug);

        Task<bool> CategorySlugExistsAsync(string slug, Guid? excludedCategoryId = null);

        Task<bool> HasActiveChildrenAsync(Guid id);

        Task<bool> HasActiveProductsAsync(Guid id);
    }
}
