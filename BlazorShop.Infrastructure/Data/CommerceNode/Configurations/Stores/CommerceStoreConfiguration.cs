namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Stores
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CommerceStoreConfiguration : IEntityTypeConfiguration<CommerceStore>
    {
        public void Configure(EntityTypeBuilder<CommerceStore> entity)
        {
            entity.ToTable("commerce_store");
            entity.HasKey(store => store.Id);
            entity.Property(store => store.Id).HasColumnName("id");
            entity.Property(store => store.PublicId).HasColumnName("public_id");
            entity.Property(store => store.ControlPlaneStorePublicId).HasColumnName("control_plane_store_public_id");
            entity.Property(store => store.StoreKey).HasColumnName("store_key").IsRequired();
            entity.Property(store => store.Name).HasColumnName("name").HasMaxLength(400).IsRequired();
            entity.Property(store => store.Status).HasColumnName("status").IsRequired();
            entity.Property(store => store.BaseUrl).HasColumnName("base_url");
            entity.Property(store => store.ForceHttps).HasColumnName("force_https").HasDefaultValue(true);
            entity.Property(store => store.SslEnabled).HasColumnName("ssl_enabled").HasDefaultValue(true);
            entity.Property(store => store.SslPort).HasColumnName("ssl_port");
            entity.Property(store => store.DisplayOrder).HasColumnName("display_order");
            entity.Property(store => store.HtmlBodyId).HasColumnName("html_body_id").HasMaxLength(128);
            entity.Property(store => store.CdnHost).HasColumnName("cdn_host");
            entity.Property(store => store.LogoUrl).HasColumnName("logo_url");
            entity.Property(store => store.CompanyName).HasColumnName("company_name").HasMaxLength(200);
            entity.Property(store => store.CompanyEmail).HasColumnName("company_email").HasMaxLength(254);
            entity.Property(store => store.CompanyPhone).HasColumnName("company_phone").HasMaxLength(50);
            entity.Property(store => store.CompanyAddress).HasColumnName("company_address").HasMaxLength(500);
            entity.Property(store => store.FaviconUrl).HasColumnName("favicon_url");
            entity.Property(store => store.PngIconUrl).HasColumnName("png_icon_url");
            entity.Property(store => store.AppleTouchIconUrl).HasColumnName("apple_touch_icon_url");
            entity.Property(store => store.MsTileImageUrl).HasColumnName("ms_tile_image_url");
            entity.Property(store => store.MsTileColor).HasColumnName("ms_tile_color").HasMaxLength(32);
            entity.Property(store => store.DefaultCurrencyCode).HasColumnName("default_currency_code").HasMaxLength(3).IsRequired();
            entity.Property(store => store.DefaultCulture).HasColumnName("default_culture").HasMaxLength(20).IsRequired();
            entity.Property(store => store.SupportEmail).HasColumnName("support_email").HasMaxLength(256);
            entity.Property(store => store.SupportPhone).HasColumnName("support_phone").HasMaxLength(64);
            entity.Property(store => store.MaintenanceModeEnabled).HasColumnName("maintenance_mode_enabled");
            entity.Property(store => store.MaintenanceMessage).HasColumnName("maintenance_message");
            entity.Property(store => store.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(store => store.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(store => store.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(store => store.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(store => store.PublicId).IsUnique();
            entity.HasIndex(store => store.ControlPlaneStorePublicId);
            entity.HasIndex(store => store.Status);
            entity.HasIndex(store => store.DisplayOrder);
            entity.HasIndex(store => store.StoreKey)
                .IsUnique()
                .HasFilter("archived_at IS NULL");

            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_commerce_store_status",
                    "status in ('active', 'provisioning', 'disabled', 'archived')"));
            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_commerce_store_default_currency_code",
                    "char_length(default_currency_code) = 3"));
        }
    }
}
