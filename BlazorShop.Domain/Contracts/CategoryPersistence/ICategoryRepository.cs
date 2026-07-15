namespace BlazorShop.Domain.Contracts.CategoryPersistence
{
    using BlazorShop.Domain.Entities;

    public interface ICategoryRepository
    {
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId);

        Task<IReadOnlyList<Category>> GetCategoriesForCurrentStoreAsync();

        Task<IEnumerable<Category>> GetPublishedCategoriesAsync();

        Task<IReadOnlyList<Category>> GetCategoriesForTreeAsync();

        Task<IReadOnlyList<PublishedCategorySitemapEntryReadModel>> GetPublishedCategorySitemapEntriesAsync();

        Task<Category?> GetPublishedCategoryByIdAsync(Guid id);

        Task<Category?> GetPublishedCategoryBySlugAsync(string slug);

        Task<Category?> GetCategoryByIdForCurrentStoreAsync(Guid id);

        Task<bool> CategorySlugExistsAsync(string slug, Guid? excludedCategoryId = null);

        Task<bool> CategorySlugExistsInStoreAsync(string slug, Guid? storeId, Guid? excludedCategoryId = null);

        Task<bool> CategoryBelongsToCurrentStoreAsync(Guid id);

        Task<bool> HasActiveChildrenAsync(Guid id);

        Task<bool> HasActiveProductsAsync(Guid id);
    }
}
