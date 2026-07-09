namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeCartRepository : ICart
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeCartRepository(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<int> SaveCheckoutHistory(IEnumerable<OrderItem> checkouts)
        {
            var storeId = await this.ResolveCurrentStoreIdAsync();
            var items = checkouts.ToArray();
            foreach (var item in items)
            {
                item.StoreId ??= storeId;
            }

            this.context.CheckoutOrderItems.AddRange(items);
            return await this.context.SaveChangesAsync();
        }

        public async Task<IEnumerable<OrderItem>> GetAllCheckoutHistory()
        {
            var items = await this.GetCurrentStoreCheckoutHistoryAsync();
            return await items.ToListAsync();
        }

        public async Task<IEnumerable<OrderItem>> GetCheckoutHistoryByUserId(string userId)
        {
            var items = await this.GetCurrentStoreCheckoutHistoryAsync();
            return await items
                .Where(orderItem => orderItem.UserId == userId)
                .ToListAsync();
        }

        private async Task<IQueryable<OrderItem>> GetCurrentStoreCheckoutHistoryAsync()
        {
            var storeId = await this.ResolveCurrentStoreIdAsync();
            return storeId.HasValue
                ? this.context.CheckoutOrderItems.AsNoTracking().Where(item => item.StoreId == storeId.Value)
                : this.context.CheckoutOrderItems.Where(item => false);
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }
    }
}
