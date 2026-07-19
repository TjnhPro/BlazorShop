namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Content
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class StoreSeoSlugHistoryConfiguration : IEntityTypeConfiguration<StoreSeoSlugHistory>
    {
        public void Configure(EntityTypeBuilder<StoreSeoSlugHistory> entity)
        {
            entity.ToTable("store_seo_slug_history");
            entity.HasKey(history => history.Id);
            entity.Property(history => history.Id).HasColumnName("id");
            entity.Property(history => history.StoreId).HasColumnName("store_id");
            entity.Property(history => history.EntityType).HasColumnName("entity_type").HasMaxLength(64).IsRequired();
            entity.Property(history => history.EntityId).HasColumnName("entity_id");
            entity.Property(history => history.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            entity.Property(history => history.LanguageCode).HasColumnName("language_code").HasMaxLength(20);
            entity.Property(history => history.IsActive).HasColumnName("is_active");
            entity.Property(history => history.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(history => history.ReplacedAt).HasColumnName("replaced_at").HasColumnType("timestamp with time zone");
            entity.Property(history => history.ReplacedBySlug).HasColumnName("replaced_by_slug").HasMaxLength(200);

            entity.HasOne(history => history.Store)
                .WithMany()
                .HasForeignKey(history => history.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(history => new { history.StoreId, history.EntityType, history.EntityId, history.LanguageCode })
                .IsUnique()
                .HasFilter("is_active = true");
            entity.HasIndex(history => new { history.StoreId, history.EntityType, history.Slug, history.LanguageCode })
                .IsUnique()
                .HasFilter("is_active = true");
            entity.HasIndex(history => new { history.StoreId, history.EntityType, history.EntityId, history.CreatedAt });
        }
    }
}
