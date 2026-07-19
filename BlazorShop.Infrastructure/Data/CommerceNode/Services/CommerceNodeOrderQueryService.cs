namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeOrderQueryService : IOrderQueryService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly OrderReadModelAssembler orderReadModelAssembler;

        public CommerceNodeOrderQueryService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            OrderReadModelAssembler orderReadModelAssembler)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.orderReadModelAssembler = orderReadModelAssembler;
        }

        public async Task<IEnumerable<GetOrder>> GetOrdersForUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return [];
            }

            var orderQuery = await this.GetCurrentStoreOrdersAsync();
            var orders = await orderQuery
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();

            return await this.MapOrdersAsync(orders);
        }

        public async Task<IEnumerable<GetOrder>> GetAllAsync()
        {
            var orderQuery = await this.GetCurrentStoreOrdersAsync();
            var orders = await orderQuery
                .OrderByDescending(order => order.CreatedOn)
                .ToListAsync();

            return await this.MapOrdersAsync(orders);
        }

        private async Task<IQueryable<Order>> GetCurrentStoreOrdersAsync()
        {
            var storeId = await this.ResolveCurrentStoreIdAsync();
            return storeId.HasValue
                ? this.context.Orders
                    .AsNoTracking()
                    .Include(order => order.Lines)
                    .Where(order => order.StoreId == storeId.Value)
                : this.context.Orders.AsNoTracking().Where(order => false);
        }

        private async Task<IReadOnlyList<GetOrder>> MapOrdersAsync(IReadOnlyCollection<Order> orders)
        {
            return await this.orderReadModelAssembler.BuildAsync(orders, OrderReadModelOptions.Internal());
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }

    }
}
