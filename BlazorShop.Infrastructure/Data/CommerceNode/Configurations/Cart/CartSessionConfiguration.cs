namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Cart
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CartSessionConfiguration : IEntityTypeConfiguration<CartSession>
    {
        public void Configure(EntityTypeBuilder<CartSession> entity)
        {
            entity.ToTable("cart_sessions");
            entity.HasKey(cart => cart.Id);
            entity.Property(cart => cart.Id).HasColumnName("id");
            entity.Property(cart => cart.PublicId).HasColumnName("public_id");
            entity.Property(cart => cart.StoreId).HasColumnName("store_id");
            entity.Property(cart => cart.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            entity.Property(cart => cart.CustomerId).HasColumnName("customer_id");
            entity.Property(cart => cart.AppUserId).HasColumnName("app_user_id").HasMaxLength(450);
            entity.Property(cart => cart.State).HasColumnName("state").HasMaxLength(32).IsRequired();
            entity.Property(cart => cart.Version).HasColumnName("version").HasDefaultValue(1);
            entity.Property(cart => cart.LastActivityAtUtc).HasColumnName("last_activity_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(cart => cart.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(cart => cart.ConvertedOrderId).HasColumnName("converted_order_id");
            entity.Property(cart => cart.MergedIntoCartId).HasColumnName("merged_into_cart_id");
            entity.Property(cart => cart.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(cart => cart.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(cart => cart.PublicId).IsUnique();
            entity.HasIndex(cart => cart.TokenHash).IsUnique();
            entity.HasIndex(cart => new { cart.StoreId, cart.State });
            entity.HasIndex(cart => cart.CustomerId);
            entity.HasIndex(cart => cart.AppUserId).HasFilter("app_user_id IS NOT NULL");
            entity.HasIndex(cart => cart.ExpiresAtUtc);

            entity.HasOne(cart => cart.Store)
                .WithMany()
                .HasForeignKey(cart => cart.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cart => cart.Customer)
                .WithMany()
                .HasForeignKey(cart => cart.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cart => cart.AppUser)
                .WithMany()
                .HasForeignKey(cart => cart.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cart => cart.ConvertedOrder)
                .WithMany()
                .HasForeignKey(cart => cart.ConvertedOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(cart => cart.MergedIntoCart)
                .WithMany()
                .HasForeignKey(cart => cart.MergedIntoCartId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
