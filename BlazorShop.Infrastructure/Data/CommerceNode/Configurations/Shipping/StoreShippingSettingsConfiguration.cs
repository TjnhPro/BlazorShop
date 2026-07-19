namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Shipping
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreShippingSettingsConfiguration : IEntityTypeConfiguration<StoreShippingSettings>
    {
        public void Configure(EntityTypeBuilder<StoreShippingSettings> entity)
        {
            entity.ToTable("store_shipping_settings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).HasColumnName("id");
            entity.Property(settings => settings.PublicId).HasColumnName("public_id");
            entity.Property(settings => settings.StoreId).HasColumnName("store_id");
            entity.Property(settings => settings.OriginFullName).HasColumnName("origin_full_name").HasMaxLength(128);
            entity.Property(settings => settings.OriginCompany).HasColumnName("origin_company").HasMaxLength(128);
            entity.Property(settings => settings.OriginAddress1).HasColumnName("origin_address1").HasMaxLength(256);
            entity.Property(settings => settings.OriginAddress2).HasColumnName("origin_address2").HasMaxLength(256);
            entity.Property(settings => settings.OriginCity).HasColumnName("origin_city").HasMaxLength(128);
            entity.Property(settings => settings.OriginStateProvinceCode).HasColumnName("origin_state_province_code").HasMaxLength(32);
            entity.Property(settings => settings.OriginPostalCode).HasColumnName("origin_postal_code").HasMaxLength(32);
            entity.Property(settings => settings.OriginCountryCode).HasColumnName("origin_country_code").HasMaxLength(2);
            entity.Property(settings => settings.EnabledCountryCodesJson).HasColumnName("enabled_country_codes_json").HasColumnType("jsonb");
            entity.Property(settings => settings.DefaultFlatRate).HasColumnName("default_flat_rate").HasPrecision(18, 2);
            entity.Property(settings => settings.FreeShippingThreshold).HasColumnName("free_shipping_threshold").HasPrecision(18, 2);
            entity.Property(settings => settings.SurchargePolicy).HasColumnName("surcharge_policy").HasMaxLength(16).HasDefaultValue("sum").IsRequired();
            entity.Property(settings => settings.DefaultDeliveryEstimateText).HasColumnName("default_delivery_estimate_text").HasMaxLength(128);
            entity.Property(settings => settings.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(settings => settings.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(settings => settings.UpdatedByUserId).HasColumnName("updated_by_user_id").HasMaxLength(128);

            entity.HasOne(settings => settings.Store)
                .WithMany()
                .HasForeignKey(settings => settings.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(settings => settings.PublicId).IsUnique();
            entity.HasIndex(settings => settings.StoreId).IsUnique();
        }
    }
}
