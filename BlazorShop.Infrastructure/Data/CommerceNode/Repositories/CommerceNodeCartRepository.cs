namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeCartRepository : ICart
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeCartRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<int> SaveCheckoutHistory(IEnumerable<OrderItem> checkouts)
        {
            this.context.CheckoutOrderItems.AddRange(checkouts);
            return await this.context.SaveChangesAsync();
        }

        public async Task<IEnumerable<OrderItem>> GetAllCheckoutHistory()
        {
            return await this.context.CheckoutOrderItems.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<OrderItem>> GetCheckoutHistoryByUserId(string userId)
        {
            return await this.context.CheckoutOrderItems
                .AsNoTracking()
                .Where(orderItem => orderItem.UserId == userId)
                .ToListAsync();
        }
    }
}
