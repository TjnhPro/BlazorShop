namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using BlazorShop.Domain.Contracts.Newsletters;
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeNewsletterSubscriberRepository : INewsletterSubscriberRepository
    {
        private readonly CommerceNodeDbContext context;

        public CommerceNodeNewsletterSubscriberRepository(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<List<NewsletterSubscriber>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc)
        {
            return await this.context.NewsletterSubscribers
                .AsNoTracking()
                .Where(subscriber => subscriber.CreatedOn >= fromUtc && subscriber.CreatedOn <= toUtc)
                .OrderBy(subscriber => subscriber.CreatedOn)
                .ToListAsync();
        }
    }
}
