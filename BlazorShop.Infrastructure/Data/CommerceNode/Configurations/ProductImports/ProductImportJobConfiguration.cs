namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.ProductImports
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class ProductImportJobConfiguration : IEntityTypeConfiguration<ProductImportJob>
    {
        public void Configure(EntityTypeBuilder<ProductImportJob> entity)
        {
            entity.ToTable("product_import_job");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Id).HasColumnName("id");
            entity.Property(job => job.PublicId).HasColumnName("public_id");
            entity.Property(job => job.StoreId).HasColumnName("store_id");
            entity.Property(job => job.TaskPublicId).HasColumnName("task_public_id");
            entity.Property(job => job.Mode).HasColumnName("mode").HasMaxLength(32).IsRequired();
            entity.Property(job => job.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
            entity.Property(job => job.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
            entity.Property(job => job.StoredFilePath).HasColumnName("stored_file_path").IsRequired();
            entity.Property(job => job.FileHash).HasColumnName("file_hash").HasMaxLength(128).IsRequired();
            entity.Property(job => job.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(job => job.TotalRows).HasColumnName("total_rows");
            entity.Property(job => job.CreatedCount).HasColumnName("created_count");
            entity.Property(job => job.UpdatedCount).HasColumnName("updated_count");
            entity.Property(job => job.FailedCount).HasColumnName("failed_count");
            entity.Property(job => job.SkippedCount).HasColumnName("skipped_count");
            entity.Property(job => job.MediaQueuedCount).HasColumnName("media_queued_count");
            entity.Property(job => job.ErrorMessage).HasColumnName("error_message");
            entity.Property(job => job.ErrorJson).HasColumnName("error_json").HasColumnType("jsonb");
            entity.Property(job => job.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
            entity.Property(job => job.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(job => job.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
            entity.Property(job => job.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
            entity.Property(job => job.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(job => job.PublicId).IsUnique();
            entity.HasIndex(job => new { job.StoreId, job.Mode, job.FileHash }).IsUnique();
            entity.HasIndex(job => new { job.StoreId, job.Status, job.CreatedAt });
            entity.HasIndex(job => job.TaskPublicId);

            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_product_import_job_status",
                    "status in ('Queued', 'Running', 'Completed', 'CompletedWithErrors', 'Failed')"));
            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_product_import_job_mode",
                    "mode in ('create_only', 'upsert')"));
        }
    }
}
