namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts.Newsletters;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeNewsletterSubscriberRepository : INewsletterSubscriberRepository
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;

        public CommerceNodeNewsletterSubscriberRepository(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext)
        {
            this.context = context;
            this.storeContext = storeContext;
        }

        public async Task<List<NewsletterSubscriber>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc)
        {
            var storeId = await this.ResolveCurrentStoreIdAsync();
            return await this.context.NewsletterSubscribers
                .AsNoTracking()
                .Where(subscriber => subscriber.StoreId == storeId
                    && subscriber.CreatedOn >= fromUtc
                    && subscriber.CreatedOn <= toUtc)
                .OrderBy(subscriber => subscriber.CreatedOn)
                .ToListAsync();
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }
    }
}
