namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Catalog
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class VariationTemplateConfiguration : IEntityTypeConfiguration<VariationTemplate>
    {
        public void Configure(EntityTypeBuilder<VariationTemplate> entity)
        {
            entity.ToTable("variation_templates");
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Id).HasColumnName("id");
            entity.Property(template => template.PublicId).HasColumnName("public_id");
            entity.Property(template => template.StoreId).HasColumnName("store_id");
            entity.Property(template => template.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(template => template.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
            entity.Property(template => template.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(template => template.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(template => template.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(template => template.PublicId).IsUnique();
            entity.HasIndex(template => new { template.StoreId, template.Slug }).IsUnique();
            entity.HasIndex(template => new { template.StoreId, template.IsActive });
        }
    }
}
