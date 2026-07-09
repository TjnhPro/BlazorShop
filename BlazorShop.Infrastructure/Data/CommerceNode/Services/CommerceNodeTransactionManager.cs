namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.Services.Contracts;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeTransactionManager : IApplicationTransactionManager
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeTransactionManager(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            var executionStrategy = this.context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await this.context.Database.BeginTransactionAsync();
                try
                {
                    var result = await action();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
