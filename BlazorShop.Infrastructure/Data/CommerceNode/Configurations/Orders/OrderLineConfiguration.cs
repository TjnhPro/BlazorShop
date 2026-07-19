namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Orders
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
    {
        public void Configure(EntityTypeBuilder<OrderLine> entity)
        {
            entity.Property(line => line.Sku)
                .HasMaxLength(64);

            entity.Property(line => line.VariantAttributesJson)
                .HasColumnType("jsonb");

            entity.Property(line => line.PersonalizationHash)
                .HasMaxLength(128);

            entity.Property(line => line.PersonalizationJson)
                .HasColumnType("jsonb");

            entity.Property(line => line.FulfillmentProviderKey)
                .HasMaxLength(64);

            entity.Property(line => line.CurrencyCode)
                .HasMaxLength(3);

            entity.Property(line => line.BaseUnitPrice)
                .HasPrecision(18, 2);

            entity.Property(line => line.ConvertedUnitPrice)
                .HasPrecision(18, 2);

            entity.Property(line => line.LineTotal)
                .HasPrecision(18, 2);

            entity.Property(line => line.BaseLineTotal)
                .HasPrecision(18, 2);

            entity.HasIndex(line => line.ProductVariantId);
            entity.HasIndex(line => line.ArtworkAssetId);

            entity.HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(line => line.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
