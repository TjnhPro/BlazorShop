namespace BlazorShop.Domain.Contracts
{
    using BlazorShop.Domain.Entities;

    public interface IProductReadRepository
    {
        Task<IEnumerable<Product>> GetCatalogProductsAsync();

        Task<IEnumerable<Product>> GetCatalogProductsForCurrentStoreAsync();

        Task<PagedResult<CatalogProductReadModel>> GetCatalogPageAsync(ProductCatalogQuery query);

        Task<PagedResult<CatalogProductReadModel>> GetCatalogPageForCurrentStoreAsync(ProductCatalogQuery query);

        Task<PagedResult<CatalogProductReadModel>> GetPublishedCatalogPageAsync(ProductCatalogQuery query);

        Task<IReadOnlyList<PublishedProductSitemapEntryReadModel>> GetPublishedProductSitemapEntriesAsync();

        Task<Product?> GetProductDetailsByIdAsync(Guid id);

        Task<Product?> GetProductDetailsByIdForCurrentStoreAsync(Guid id);

        Task<Product?> GetPublishedProductDetailsByIdAsync(Guid id);

        Task<Product?> GetPublishedProductBySlugAsync(string slug);

        Task<IReadOnlyList<CatalogProductReadModel>> GetPublishedProductsByCategoryAsync(Guid categoryId);

        Task<bool> ProductSlugExistsAsync(string slug, Guid? excludedProductId = null);

        Task<bool> ProductSlugExistsInStoreAsync(string slug, Guid? storeId, Guid? excludedProductId = null);

        Task<bool> ProductSkuExistsAsync(string sku, Guid? storeId, Guid? excludedProductId = null);

        Task<IReadOnlyDictionary<Guid, Product>> GetProductsByIdsAsync(IEnumerable<Guid> productIds);
    }
}
