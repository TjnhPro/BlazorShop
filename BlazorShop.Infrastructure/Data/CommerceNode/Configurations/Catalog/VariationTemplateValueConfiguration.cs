namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Catalog
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class VariationTemplateValueConfiguration : IEntityTypeConfiguration<VariationTemplateValue>
    {
        public void Configure(EntityTypeBuilder<VariationTemplateValue> entity)
        {
            entity.ToTable("variation_template_values");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.PublicId).HasColumnName("public_id");
            entity.Property(value => value.OptionId).HasColumnName("option_id");
            entity.Property(value => value.Value).HasColumnName("value").HasMaxLength(200).IsRequired();
            entity.Property(value => value.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(value => value.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(value => value.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
            entity.Property(value => value.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(value => value.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(value => value.PublicId).IsUnique();
            entity.HasIndex(value => new { value.OptionId, value.Value }).IsUnique();
            entity.HasIndex(value => new { value.OptionId, value.SortOrder });

            entity.HasOne(value => value.Option)
                .WithMany(option => option.Values)
                .HasForeignKey(value => value.OptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
