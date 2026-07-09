namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts.Seo;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeSeoSettingsRepository : ISeoSettingsRepository
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeSeoSettingsRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<SeoSettings?> GetCurrentAsync()
        {
            return await this.context.SeoSettings
                .OrderBy(settings => settings.Id)
                .FirstOrDefaultAsync();
        }
    }
}
