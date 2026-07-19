namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Catalog
{
    using BlazorShop.Domain.Entities;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> entity)
        {
            entity.Property(variant => variant.AttributesJson)
                .HasColumnType("jsonb");

            entity.Property(variant => variant.AttributeSignature)
                .HasMaxLength(512);

            entity.Property(variant => variant.DisplayName)
                .HasMaxLength(256);

            entity.Property(variant => variant.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(variant => new { variant.ProductId, variant.AttributeSignature })
                .IsUnique()
                .HasFilter("\"AttributeSignature\" IS NOT NULL");

            entity.HasIndex(variant => variant.ProductId)
                .IsUnique()
                .HasFilter("\"IsDefault\" = TRUE");

            entity.HasOne(variant => variant.Product)
                .WithMany(product => product.Variants)
                .HasForeignKey(variant => variant.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
