namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Media
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CategoryMediaAssignmentConfiguration : IEntityTypeConfiguration<CategoryMediaAssignment>
    {
        public void Configure(EntityTypeBuilder<CategoryMediaAssignment> entity)
        {
            entity.ToTable("category_media_assignment");
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.Id).HasColumnName("id");
            entity.Property(assignment => assignment.StoreId).HasColumnName("store_id");
            entity.Property(assignment => assignment.CategoryId).HasColumnName("category_id");
            entity.Property(assignment => assignment.MediaAssetId).HasColumnName("media_asset_id");
            entity.Property(assignment => assignment.AltText).HasColumnName("alt_text").HasMaxLength(500);
            entity.Property(assignment => assignment.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(assignment => assignment.IsPrimary).HasColumnName("is_primary").HasDefaultValue(true);
            entity.Property(assignment => assignment.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(assignment => assignment.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(assignment => new { assignment.StoreId, assignment.CategoryId, assignment.IsPrimary })
                .IsUnique()
                .HasFilter("is_primary = TRUE");
            entity.HasIndex(assignment => new { assignment.StoreId, assignment.MediaAssetId });
            entity.HasIndex(assignment => new { assignment.StoreId, assignment.CategoryId, assignment.SortOrder });

            entity.HasOne(assignment => assignment.Category)
                .WithMany()
                .HasForeignKey(assignment => assignment.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(assignment => assignment.MediaAsset)
                .WithMany()
                .HasForeignKey(assignment => assignment.MediaAssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(
                "category_media_assignment",
                table =>
                {
                    table.HasCheckConstraint("ck_category_media_assignment_sort_order", "sort_order >= 0");
                });
        }
    }
}
