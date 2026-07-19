namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Stores
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreCurrencyExchangeRateConfiguration : IEntityTypeConfiguration<StoreCurrencyExchangeRate>
    {
        public void Configure(EntityTypeBuilder<StoreCurrencyExchangeRate> entity)
        {
            entity.ToTable("store_currency_exchange_rates");
            entity.HasKey(rate => rate.Id);
            entity.Property(rate => rate.Id).HasColumnName("id");
            entity.Property(rate => rate.StoreId).HasColumnName("store_id");
            entity.Property(rate => rate.BaseCurrencyCode).HasColumnName("base_currency_code").HasMaxLength(3).IsRequired();
            entity.Property(rate => rate.TargetCurrencyCode).HasColumnName("target_currency_code").HasMaxLength(3).IsRequired();
            entity.Property(rate => rate.Rate).HasColumnName("rate").HasPrecision(28, 12);
            entity.Property(rate => rate.ProviderKey).HasColumnName("provider_key").HasMaxLength(64).IsRequired();
            entity.Property(rate => rate.Source).HasColumnName("source").HasMaxLength(256);
            entity.Property(rate => rate.EffectiveAt).HasColumnName("effective_at").HasColumnType("timestamp with time zone");
            entity.Property(rate => rate.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
            entity.Property(rate => rate.IsManual).HasColumnName("is_manual");
            entity.Property(rate => rate.IsEnabled).HasColumnName("is_enabled");
            entity.Property(rate => rate.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(rate => rate.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(rate => new { rate.StoreId, rate.BaseCurrencyCode, rate.TargetCurrencyCode, rate.ProviderKey }).IsUnique();
            entity.HasIndex(rate => new { rate.StoreId, rate.TargetCurrencyCode, rate.IsEnabled });

            entity.HasOne(rate => rate.Store)
                .WithMany()
                .HasForeignKey(rate => rate.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                "store_currency_exchange_rates",
                table =>
                {
                    table.HasCheckConstraint("ck_store_currency_exchange_rates_base_currency_code", "char_length(base_currency_code) = 3");
                    table.HasCheckConstraint("ck_store_currency_exchange_rates_target_currency_code", "char_length(target_currency_code) = 3");
                    table.HasCheckConstraint("ck_store_currency_exchange_rates_distinct_currency", "base_currency_code <> target_currency_code");
                    table.HasCheckConstraint("ck_store_currency_exchange_rates_rate", "rate > 0");
                    table.HasCheckConstraint("ck_store_currency_exchange_rates_expires_after_effective", "expires_at is null or expires_at > effective_at");
                });
        }
    }
}
