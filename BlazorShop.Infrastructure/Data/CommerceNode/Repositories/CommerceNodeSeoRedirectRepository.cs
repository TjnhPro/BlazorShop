namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts.Seo;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeSeoRedirectRepository : ISeoRedirectRepository
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeSeoRedirectRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<IReadOnlyList<SeoRedirect>> ListForStoreAsync(Guid storeId)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .Where(redirect => redirect.StoreId == storeId)
                .OrderByDescending(redirect => redirect.CreatedOn)
                .ThenBy(redirect => redirect.OldPath)
                .ToListAsync();
        }

        public async Task<SeoRedirect?> GetByIdForStoreAsync(Guid storeId, Guid id)
        {
            return await this.context.SeoRedirects
                .FirstOrDefaultAsync(redirect => redirect.StoreId == storeId && redirect.Id == id);
        }

        public async Task<bool> OldPathExistsAsync(string oldPath, Guid? excludedRedirectId = null)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .AnyAsync(redirect => redirect.OldPath == oldPath
                    && (!excludedRedirectId.HasValue || redirect.Id != excludedRedirectId.Value));
        }

        public async Task<bool> OldPathExistsInStoreAsync(Guid storeId, string oldPath, Guid? excludedRedirectId = null)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .AnyAsync(redirect => redirect.StoreId == storeId
                    && redirect.OldPath == oldPath
                    && (!excludedRedirectId.HasValue || redirect.Id != excludedRedirectId.Value));
        }

        public async Task<SeoRedirect?> GetByOldPathAsync(string oldPath)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .OrderByDescending(redirect => redirect.IsActive)
                .ThenByDescending(redirect => redirect.CreatedOn)
                .FirstOrDefaultAsync(redirect => redirect.OldPath == oldPath);
        }

        public async Task<SeoRedirect?> GetByOldPathInStoreAsync(Guid storeId, string oldPath)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .OrderByDescending(redirect => redirect.IsActive)
                .ThenByDescending(redirect => redirect.CreatedOn)
                .FirstOrDefaultAsync(redirect => redirect.StoreId == storeId && redirect.OldPath == oldPath);
        }

        public async Task<SeoRedirect?> GetActiveByOldPathAsync(string oldPath)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .FirstOrDefaultAsync(redirect => redirect.IsActive && redirect.OldPath == oldPath);
        }

        public async Task<SeoRedirect?> GetActiveByOldPathInStoreAsync(Guid storeId, string oldPath)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .FirstOrDefaultAsync(redirect => redirect.StoreId == storeId
                    && redirect.IsActive
                    && redirect.OldPath == oldPath);
        }
    }
}
