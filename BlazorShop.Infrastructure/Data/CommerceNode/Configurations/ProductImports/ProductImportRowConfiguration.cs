namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.ProductImports
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ProductImportRowConfiguration : IEntityTypeConfiguration<ProductImportRow>
    {
        public void Configure(EntityTypeBuilder<ProductImportRow> entity)
        {
            entity.ToTable("product_import_row");
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.JobId).HasColumnName("job_id");
            entity.Property(row => row.RowNumber).HasColumnName("row_number");
            entity.Property(row => row.Sku).HasColumnName("sku").HasMaxLength(64);
            entity.Property(row => row.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            entity.Property(row => row.Action).HasColumnName("action").HasMaxLength(64).IsRequired();
            entity.Property(row => row.ProductId).HasColumnName("product_id");
            entity.Property(row => row.MediaStatus).HasColumnName("media_status").HasMaxLength(64).IsRequired();
            entity.Property(row => row.MediaTaskPublicId).HasColumnName("media_task_public_id");
            entity.Property(row => row.ErrorMessage).HasColumnName("error_message");
            entity.Property(row => row.ErrorJson).HasColumnName("error_json").HasColumnType("jsonb");
            entity.Property(row => row.RawDataJson).HasColumnName("raw_data_json").HasColumnType("jsonb");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(row => row.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(row => new { row.JobId, row.RowNumber }).IsUnique();
            entity.HasIndex(row => new { row.JobId, row.Status });
            entity.HasIndex(row => new { row.JobId, row.Sku });
            entity.HasIndex(row => row.ProductId);
            entity.HasIndex(row => row.MediaTaskPublicId);

            entity.HasOne(row => row.Job)
                .WithMany(job => job.Rows)
                .HasForeignKey(row => row.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_product_import_row_status",
                    "status in ('Pending', 'Succeeded', 'Failed', 'Skipped')"));
            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_product_import_row_action",
                    "action in ('Created', 'Updated', 'Skipped', 'Failed')"));
            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_product_import_row_media_status",
                    "media_status in ('None', 'Queued')"));
        }
    }
}
