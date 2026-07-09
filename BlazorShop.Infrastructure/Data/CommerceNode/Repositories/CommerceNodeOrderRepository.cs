namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeOrderRepository : IOrderRepository
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeOrderRepository(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<Guid> CreateAsync(Order order)
        {
            order.StoreId ??= await this.ResolveCurrentStoreIdAsync();
            this.context.Orders.Add(order);
            await this.context.SaveChangesAsync();
            return order.Id;
        }

        public async Task<Order?> GetByReferenceAsync(string reference)
        {
            var orders = await this.GetCurrentStoreOrdersAsync();
            return await orders
                .Include(order => order.Lines)
                .FirstOrDefaultAsync(order => order.Reference == reference);
        }

        public async Task<int> UpdateStatusAsync(Guid orderId, string status)
        {
            var orders = await this.GetCurrentStoreOrdersAsync(asTracking: true);
            var order = await orders.FirstOrDefaultAsync(item => item.Id == orderId);
            if (order is null)
            {
                return 0;
            }

            order.Status = status;
            return await this.context.SaveChangesAsync();
        }

        public async Task<List<Order>> GetByUserIdAsync(string userId)
        {
            var orders = await this.GetCurrentStoreOrdersAsync();
            return await orders
                .Include(order => order.Lines)
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllAsync()
        {
            var orders = await this.GetCurrentStoreOrdersAsync();
            return await orders
                .Include(order => order.Lines)
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc)
        {
            var orders = await this.GetCurrentStoreOrdersAsync();
            return await orders
                .Include(order => order.Lines)
                .Where(order => order.CreatedOn >= fromUtc && order.CreatedOn <= toUtc)
                .OrderBy(order => order.CreatedOn)
                .ToListAsync();
        }

        private async Task<IQueryable<Order>> GetCurrentStoreOrdersAsync(bool asTracking = false)
        {
            IQueryable<Order> orders = this.context.Orders;
            if (!asTracking)
            {
                orders = orders.AsNoTracking();
            }

            var storeId = await this.ResolveCurrentStoreIdAsync();
            return storeId.HasValue
                ? orders.Where(order => order.StoreId == storeId.Value)
                : orders.Where(order => false);
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }
    }
}
