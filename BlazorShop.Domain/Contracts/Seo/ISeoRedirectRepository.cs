namespace BlazorShop.Domain.Contracts.Seo
{
    using BlazorShop.Domain.Entities;

    public interface ISeoRedirectRepository
    {
        Task<IReadOnlyList<SeoRedirect>> ListForStoreAsync(Guid storeId);

        Task<SeoRedirect?> GetByIdForStoreAsync(Guid storeId, Guid id);

        Task<bool> OldPathExistsAsync(string oldPath, Guid? excludedRedirectId = null);

        Task<bool> OldPathExistsInStoreAsync(Guid storeId, string oldPath, Guid? excludedRedirectId = null);

        Task<SeoRedirect?> GetByOldPathAsync(string oldPath);

        Task<SeoRedirect?> GetByOldPathInStoreAsync(Guid storeId, string oldPath);

        Task<SeoRedirect?> GetActiveByOldPathAsync(string oldPath);

        Task<SeoRedirect?> GetActiveByOldPathInStoreAsync(Guid storeId, string oldPath);
    }
}
