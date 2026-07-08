namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeGenericRepository<TEntity> : IGenericRepository<TEntity>
        where TEntity : class
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeGenericRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<int> AddAsync(TEntity entity)
        {
            this.context.Set<TEntity>().Add(entity);
            return await this.context.SaveChangesAsync();
        }

        public async Task<int> DeleteAsync(Guid id)
        {
            var entity = await this.GetByIdAsync(id);
            if (entity is null)
            {
                return 0;
            }

            this.context.Set<TEntity>().Remove(entity);
            return await this.context.SaveChangesAsync();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await this.context.Set<TEntity>().AsNoTracking().ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await this.context.Set<TEntity>().FindAsync(id);
        }

        public async Task<int> UpdateAsync(TEntity entity)
        {
            this.context.Set<TEntity>().Update(entity);
            return await this.context.SaveChangesAsync();
        }
    }
}
