namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Catalog
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ProductCommerceNodeConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> entity)
        {
            entity.Property(product => product.StoreId).IsRequired();
            entity.Property(product => product.MinOrderQuantity).HasDefaultValue(1);
            entity.Property(product => product.QuantityStep).HasDefaultValue(1);
            entity.Property(product => product.PurchasingDisabled).HasDefaultValue(false);
            entity.Property(product => product.PurchasingDisabledReason)
                .HasMaxLength(ProductPurchaseConstraints.PurchasingDisabledReasonMaxLength);
            entity.Property(product => product.ManageStock).HasDefaultValue(true);
            entity.Property(product => product.HideWhenOutOfStock).HasDefaultValue(false);
            entity.Property(product => product.ShippingRequired).HasDefaultValue(true);
            entity.Property(product => product.FreeShipping).HasDefaultValue(false);
            entity.Property(product => product.ShippingSurcharge).HasPrecision(18, 2);
            entity.Property(product => product.DeliveryEstimateText)
                .HasMaxLength(ProductPurchaseConstraints.DeliveryEstimateTextMaxLength);
            entity.HasOne<CommerceStore>()
                .WithMany()
                .HasForeignKey(product => product.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(product => product.ProductType)
                .HasMaxLength(64)
                .HasDefaultValue(ProductTypes.Simple);

            entity.HasIndex(product => new { product.StoreId, product.ProductType });
            entity.HasIndex(product => product.VariationTemplateId);

            entity.HasOne(product => product.VariationTemplate)
                .WithMany(template => template.Products)
                .HasForeignKey(product => product.VariationTemplateId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
