namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Payments
{
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StorePaymentMethodConfiguration : IEntityTypeConfiguration<StorePaymentMethod>
    {
        public void Configure(EntityTypeBuilder<StorePaymentMethod> entity)
        {
            entity.ToTable("store_payment_methods");
            entity.HasKey(method => method.Id);
            entity.Property(method => method.Id).HasColumnName("id");
            entity.Property(method => method.StoreId).HasColumnName("store_id");
            entity.Property(method => method.PaymentMethodKey).HasColumnName("payment_method_key").HasMaxLength(64).IsRequired();
            entity.Property(method => method.Enabled).HasColumnName("enabled");
            entity.Property(method => method.DisplayName).HasColumnName("display_name").HasMaxLength(160).IsRequired();
            entity.Property(method => method.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(method => method.ShortDisplayText).HasColumnName("short_display_text").HasMaxLength(160);
            entity.Property(method => method.IconUrl).HasColumnName("icon_url").HasMaxLength(1024);
            entity.Property(method => method.DisplayOrder).HasColumnName("display_order");
            entity.Property(method => method.SupportedCurrencyCodesJson).HasColumnName("supported_currency_codes_json").HasColumnType("jsonb");
            entity.Property(method => method.SupportedCountryCodesJson).HasColumnName("supported_country_codes_json").HasColumnType("jsonb");
            entity.Property(method => method.MinOrderTotal).HasColumnName("min_order_total").HasPrecision(18, 2);
            entity.Property(method => method.MaxOrderTotal).HasColumnName("max_order_total").HasPrecision(18, 2);
            entity.Property(method => method.SettingsJson).HasColumnName("settings_json").HasColumnType("jsonb");
            entity.Property(method => method.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(method => method.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(method => new { method.StoreId, method.PaymentMethodKey }).IsUnique();
            entity.HasIndex(method => new { method.StoreId, method.Enabled, method.DisplayOrder });

            entity.HasOne(method => method.Store)
                .WithMany()
                .HasForeignKey(method => method.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                "store_payment_methods",
                table => table.HasCheckConstraint(
                    "ck_store_payment_methods_key",
                    "payment_method_key in ('cod', 'stripe', 'paypal')"));
        }
    }
}
