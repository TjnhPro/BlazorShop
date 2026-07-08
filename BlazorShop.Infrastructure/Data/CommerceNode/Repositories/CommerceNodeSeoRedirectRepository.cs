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

        public async Task<bool> OldPathExistsAsync(string oldPath, Guid? excludedRedirectId = null)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .AnyAsync(redirect => redirect.OldPath == oldPath
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

        public async Task<SeoRedirect?> GetActiveByOldPathAsync(string oldPath)
        {
            return await this.context.SeoRedirects
                .AsNoTracking()
                .FirstOrDefaultAsync(redirect => redirect.IsActive && redirect.OldPath == oldPath);
        }
    }
}
