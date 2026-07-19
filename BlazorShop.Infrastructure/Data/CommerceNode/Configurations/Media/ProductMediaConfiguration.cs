namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Media
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
    {
        public void Configure(EntityTypeBuilder<ProductMedia> entity)
        {
            entity.ToTable("product_media");
            entity.HasKey(media => media.Id);
            entity.Property(media => media.Id).HasColumnName("id");
            entity.Property(media => media.PublicId).HasColumnName("public_id");
            entity.Property(media => media.StoreId).HasColumnName("store_id");
            entity.Property(media => media.ProductId).HasColumnName("product_id");
            entity.Property(media => media.OriginalSourceUrl).HasColumnName("original_source_url");
            entity.Property(media => media.OriginalStoragePath).HasColumnName("original_storage_path");
            entity.Property(media => media.ContentHash).HasColumnName("content_hash").HasMaxLength(128);
            entity.Property(media => media.FileName).HasColumnName("file_name");
            entity.Property(media => media.MimeType).HasColumnName("mime_type").HasMaxLength(128);
            entity.Property(media => media.Width).HasColumnName("width");
            entity.Property(media => media.Height).HasColumnName("height");
            entity.Property(media => media.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(media => media.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(media => media.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
            entity.Property(media => media.AltText).HasColumnName("alt_text");
            entity.Property(media => media.Status).HasColumnName("status").IsRequired();
            entity.Property(media => media.ErrorMessage).HasColumnName("error_message");
            entity.Property(media => media.Version).HasColumnName("version").HasDefaultValue(1);
            entity.Property(media => media.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(media => media.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(media => media.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamp with time zone");
            entity.Property(media => media.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(media => media.PublicId).IsUnique();
            entity.HasIndex(media => new { media.StoreId, media.ProductId, media.SortOrder });
            entity.HasIndex(media => new { media.StoreId, media.ProductId, media.Status });
            entity.HasIndex(media => new { media.StoreId, media.ProductId, media.IsPrimary })
                .IsUnique()
                .HasFilter("deleted_at IS NULL AND is_primary = TRUE");
            entity.HasIndex(media => new { media.StoreId, media.ContentHash })
                .HasFilter("content_hash IS NOT NULL");
            entity.HasIndex(media => media.Status);
            entity.HasIndex(media => media.DeletedAt);

            entity.HasOne(media => media.Product)
                .WithMany()
                .HasForeignKey(media => media.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                "product_media",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_product_media_status",
                        "status in ('pending', 'downloading', 'stored', 'failed', 'deleted')");
                    table.HasCheckConstraint("ck_product_media_sort_order", "sort_order >= 0");
                    table.HasCheckConstraint("ck_product_media_version", "version >= 1");
                    table.HasCheckConstraint("ck_product_media_width", "width IS NULL OR width > 0");
                    table.HasCheckConstraint("ck_product_media_height", "height IS NULL OR height > 0");
                    table.HasCheckConstraint("ck_product_media_file_size", "file_size_bytes IS NULL OR file_size_bytes > 0");
                });
        }
    }
}
