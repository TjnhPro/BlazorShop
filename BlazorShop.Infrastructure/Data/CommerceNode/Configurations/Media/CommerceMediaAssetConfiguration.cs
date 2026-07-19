namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Media
{
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CommerceMediaAssetConfiguration : IEntityTypeConfiguration<CommerceMediaAsset>
    {
        public void Configure(EntityTypeBuilder<CommerceMediaAsset> entity)
        {
            entity.ToTable("commerce_media_asset");
            entity.HasKey(asset => asset.Id);
            entity.Property(asset => asset.Id).HasColumnName("id");
            entity.Property(asset => asset.PublicId).HasColumnName("public_id");
            entity.Property(asset => asset.StoreId).HasColumnName("store_id");
            entity.Property(asset => asset.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260).IsRequired();
            entity.Property(asset => asset.CanonicalFileName).HasColumnName("canonical_file_name").HasMaxLength(260).IsRequired();
            entity.Property(asset => asset.DisplayName).HasColumnName("display_name").HasMaxLength(260).IsRequired();
            entity.Property(asset => asset.AltText).HasColumnName("alt_text").HasMaxLength(500).IsRequired();
            entity.Property(asset => asset.TitleText).HasColumnName("title_text").HasMaxLength(500);
            entity.Property(asset => asset.UsageType).HasColumnName("usage_type").HasMaxLength(32).HasDefaultValue(CommerceMediaAssetUsageTypes.Content).IsRequired();
            entity.Property(asset => asset.OriginalStoragePath).HasColumnName("original_storage_path").IsRequired();
            entity.Property(asset => asset.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
            entity.Property(asset => asset.MimeType).HasColumnName("mime_type").HasMaxLength(128).IsRequired();
            entity.Property(asset => asset.Extension).HasColumnName("extension").HasMaxLength(16).IsRequired();
            entity.Property(asset => asset.Width).HasColumnName("width");
            entity.Property(asset => asset.Height).HasColumnName("height");
            entity.Property(asset => asset.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(asset => asset.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(asset => asset.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(asset => asset.PublicId).IsUnique();
            entity.HasIndex(asset => new { asset.StoreId, asset.UpdatedAt });
            entity.HasIndex(asset => new { asset.StoreId, asset.UsageType, asset.UpdatedAt });
            entity.HasIndex(asset => new { asset.StoreId, asset.CanonicalFileName });
            entity.HasIndex(asset => new { asset.StoreId, asset.ContentHash });

            entity.ToTable(
                "commerce_media_asset",
                table =>
                {
                    table.HasCheckConstraint("ck_commerce_media_asset_file_size", "file_size_bytes > 0");
                    table.HasCheckConstraint("ck_commerce_media_asset_width", "width IS NULL OR width > 0");
                    table.HasCheckConstraint("ck_commerce_media_asset_height", "height IS NULL OR height > 0");
                    table.HasCheckConstraint("ck_commerce_media_asset_usage_type", "usage_type in ('content', 'branding', 'theme', 'category')");
                });
        }
    }
}
