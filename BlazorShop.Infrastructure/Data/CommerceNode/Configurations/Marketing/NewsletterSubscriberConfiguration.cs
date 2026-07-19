namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Marketing
{
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
    {
        public void Configure(EntityTypeBuilder<NewsletterSubscriber> entity)
        {
            entity.HasIndex(subscriber => new { subscriber.StoreId, subscriber.Email })
                .IsUnique()
                .HasFilter("\"StoreId\" IS NOT NULL");

            entity.HasIndex(subscriber => subscriber.StoreId);
        }
    }
}
