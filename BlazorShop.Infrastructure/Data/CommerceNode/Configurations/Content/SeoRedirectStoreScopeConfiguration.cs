namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Content
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class SeoRedirectStoreScopeConfiguration : IEntityTypeConfiguration<SeoRedirect>
    {
        public void Configure(EntityTypeBuilder<SeoRedirect> entity)
        {
            entity.HasOne<CommerceStore>()
                .WithMany()
                .HasForeignKey(redirect => redirect.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
