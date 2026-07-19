namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Stores
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreCurrencyConfiguration : IEntityTypeConfiguration<StoreCurrency>
    {
        public void Configure(EntityTypeBuilder<StoreCurrency> entity)
        {
            entity.ToTable("store_currencies");
            entity.HasKey(currency => currency.Id);
            entity.Property(currency => currency.Id).HasColumnName("id");
            entity.Property(currency => currency.StoreId).HasColumnName("store_id");
            entity.Property(currency => currency.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            entity.Property(currency => currency.IsEnabled).HasColumnName("is_enabled");
            entity.Property(currency => currency.IsDefaultDisplayCurrency).HasColumnName("is_default_display_currency");
            entity.Property(currency => currency.DisplayOrder).HasColumnName("display_order");
            entity.Property(currency => currency.CultureName).HasColumnName("culture_name").HasMaxLength(32);
            entity.Property(currency => currency.Symbol).HasColumnName("symbol").HasMaxLength(16);
            entity.Property(currency => currency.DecimalDigits).HasColumnName("decimal_digits");
            entity.Property(currency => currency.UnitPriceRoundingMode).HasColumnName("unit_price_rounding_mode").HasMaxLength(32).IsRequired();
            entity.Property(currency => currency.UnitPriceRoundingIncrement).HasColumnName("unit_price_rounding_increment").HasPrecision(18, 4);
            entity.Property(currency => currency.LineTotalRoundingMode).HasColumnName("line_total_rounding_mode").HasMaxLength(32).IsRequired();
            entity.Property(currency => currency.LineTotalRoundingIncrement).HasColumnName("line_total_rounding_increment").HasPrecision(18, 4);
            entity.Property(currency => currency.OrderTotalRoundingMode).HasColumnName("order_total_rounding_mode").HasMaxLength(32).IsRequired();
            entity.Property(currency => currency.OrderTotalRoundingIncrement).HasColumnName("order_total_rounding_increment").HasPrecision(18, 4);
            entity.Property(currency => currency.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(currency => currency.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(currency => new { currency.StoreId, currency.CurrencyCode }).IsUnique();
            entity.HasIndex(currency => new { currency.StoreId, currency.IsEnabled, currency.DisplayOrder });

            entity.HasOne(currency => currency.Store)
                .WithMany()
                .HasForeignKey(currency => currency.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                "store_currencies",
                table =>
                {
                    table.HasCheckConstraint("ck_store_currencies_currency_code", "char_length(currency_code) = 3");
                    table.HasCheckConstraint("ck_store_currencies_decimal_digits", "decimal_digits >= 0 and decimal_digits <= 4");
                    table.HasCheckConstraint("ck_store_currencies_unit_price_rounding_increment", "unit_price_rounding_increment > 0");
                    table.HasCheckConstraint("ck_store_currencies_line_total_rounding_increment", "line_total_rounding_increment > 0");
                    table.HasCheckConstraint("ck_store_currencies_order_total_rounding_increment", "order_total_rounding_increment > 0");
                });
        }
    }
}
