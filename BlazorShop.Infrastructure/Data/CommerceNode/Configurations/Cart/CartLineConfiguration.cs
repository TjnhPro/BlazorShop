namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Cart
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CartLineConfiguration : IEntityTypeConfiguration<CartLine>
    {
        public void Configure(EntityTypeBuilder<CartLine> entity)
        {
            entity.ToTable("cart_lines");
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Id).HasColumnName("id");
            entity.Property(line => line.CartSessionId).HasColumnName("cart_session_id");
            entity.Property(line => line.ProductId).HasColumnName("product_id");
            entity.Property(line => line.ProductVariantId).HasColumnName("product_variant_id");
            entity.Property(line => line.LineKey).HasColumnName("line_key").HasMaxLength(64).IsRequired();
            entity.Property(line => line.SelectedAttributesJson).HasColumnName("selected_attributes_json").HasColumnType("jsonb");
            entity.Property(line => line.PersonalizationHash).HasColumnName("personalization_hash").HasMaxLength(128);
            entity.Property(line => line.PersonalizationJson).HasColumnName("personalization_json").HasColumnType("jsonb");
            entity.Property(line => line.ArtworkAssetId).HasColumnName("artwork_asset_id");
            entity.Property(line => line.ArtworkVersion).HasColumnName("artwork_version");
            entity.Property(line => line.FulfillmentProviderKey).HasColumnName("fulfillment_provider_key").HasMaxLength(64);
            entity.Property(line => line.Quantity).HasColumnName("quantity");
            entity.Property(line => line.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasPrecision(18, 2);
            entity.Property(line => line.CurrencyCodeSnapshot).HasColumnName("currency_code_snapshot").HasMaxLength(3);
            entity.Property(line => line.BaseUnitPriceSnapshot).HasColumnName("base_unit_price_snapshot").HasPrecision(18, 2);
            entity.Property(line => line.BaseCurrencyCodeSnapshot).HasColumnName("base_currency_code_snapshot").HasMaxLength(3);
            entity.Property(line => line.ExchangeRateSnapshot).HasColumnName("exchange_rate_snapshot").HasPrecision(18, 8);
            entity.Property(line => line.ExchangeRateProviderKey).HasColumnName("exchange_rate_provider_key").HasMaxLength(64);
            entity.Property(line => line.ExchangeRateSource).HasColumnName("exchange_rate_source").HasMaxLength(256);
            entity.Property(line => line.ExchangeRateEffectiveAtUtc).HasColumnName("exchange_rate_effective_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(line => line.ExchangeRateExpiresAtUtc).HasColumnName("exchange_rate_expires_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(line => line.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(line => line.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(line => new { line.CartSessionId, line.LineKey }).IsUnique();
            entity.HasIndex(line => line.ProductId);
            entity.HasIndex(line => line.ProductVariantId);
            entity.HasIndex(line => line.ArtworkAssetId);

            entity.HasOne(line => line.CartSession)
                .WithMany(cart => cart.Lines)
                .HasForeignKey(line => line.CartSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(line => line.Product)
                .WithMany()
                .HasForeignKey(line => line.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(line => line.ProductVariant)
                .WithMany()
                .HasForeignKey(line => line.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
