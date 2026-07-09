namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeOrderRepository : IOrderRepository
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeOrderRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<Guid> CreateAsync(Order order)
        {
            this.context.Orders.Add(order);
            await this.context.SaveChangesAsync();
            return order.Id;
        }

        public async Task<Order?> GetByReferenceAsync(string reference)
        {
            return await this.context.Orders
                .Include(order => order.Lines)
                .FirstOrDefaultAsync(order => order.Reference == reference);
        }

        public async Task<int> UpdateStatusAsync(Guid orderId, string status)
        {
            var order = await this.context.Orders.FirstOrDefaultAsync(item => item.Id == orderId);
            if (order is null)
            {
                return 0;
            }

            order.Status = status;
            return await this.context.SaveChangesAsync();
        }

        public async Task<List<Order>> GetByUserIdAsync(string userId)
        {
            return await this.context.Orders
                .Include(order => order.Lines)
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await this.context.Orders
                .Include(order => order.Lines)
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc)
        {
            return await this.context.Orders
                .Include(order => order.Lines)
                .Where(order => order.CreatedOn >= fromUtc && order.CreatedOn <= toUtc)
                .OrderBy(order => order.CreatedOn)
                .ToListAsync();
        }
    }
}
