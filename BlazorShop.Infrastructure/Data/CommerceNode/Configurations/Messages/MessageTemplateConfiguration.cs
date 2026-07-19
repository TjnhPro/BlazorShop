namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Messages
{
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Seed;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
    {
        public void Configure(EntityTypeBuilder<MessageTemplate> entity)
        {
            entity.ToTable("message_templates");
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Id).HasColumnName("id");
            entity.Property(template => template.PublicId).HasColumnName("public_id");
            entity.Property(template => template.SystemName).HasColumnName("system_name").HasMaxLength(128).IsRequired();
            entity.Property(template => template.StoreId).HasColumnName("store_id");
            entity.Property(template => template.LanguageCode).HasColumnName("language_code").HasMaxLength(16);
            entity.Property(template => template.SubjectTemplate).HasColumnName("subject_template").HasMaxLength(512).IsRequired();
            entity.Property(template => template.BodyHtmlTemplate).HasColumnName("body_html_template").HasColumnType("text").IsRequired();
            entity.Property(template => template.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(template => template.Description).HasColumnName("description").HasMaxLength(512);
            entity.Property(template => template.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(template => template.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(template => template.Store)
                .WithMany()
                .HasForeignKey(template => template.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(template => template.PublicId).IsUnique();
            entity.HasIndex(template => new { template.StoreId, template.SystemName });
            entity.HasIndex(template => new { template.SystemName, template.IsActive });
            entity.HasIndex(template => new { template.SystemName, template.StoreId, template.LanguageCode })
                .IsUnique()
                .HasFilter("store_id IS NOT NULL AND language_code IS NOT NULL");
            entity.HasIndex(template => new { template.SystemName, template.StoreId })
                .IsUnique()
                .HasFilter("store_id IS NOT NULL AND language_code IS NULL")
                .HasDatabaseName("ix_message_templates_unique_store_default_language");
            entity.HasIndex(template => new { template.SystemName, template.LanguageCode })
                .IsUnique()
                .HasFilter("store_id IS NULL AND language_code IS NOT NULL")
                .HasDatabaseName("ix_message_templates_unique_global_language");
            entity.HasIndex(template => template.SystemName)
                .IsUnique()
                .HasFilter("store_id IS NULL AND language_code IS NULL")
                .HasDatabaseName("ix_message_templates_unique_global_default_language");

            entity.HasData(CommerceNodeSeedData.CreateDefaultMessageTemplates());
        }
    }
}
