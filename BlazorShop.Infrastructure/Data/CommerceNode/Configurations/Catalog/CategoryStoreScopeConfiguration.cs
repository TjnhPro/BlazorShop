namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Catalog
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CategoryStoreScopeConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> entity)
        {
            entity.Property(category => category.StoreId).IsRequired();
            entity.HasOne<CommerceStore>()
                .WithMany()
                .HasForeignKey(category => category.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
