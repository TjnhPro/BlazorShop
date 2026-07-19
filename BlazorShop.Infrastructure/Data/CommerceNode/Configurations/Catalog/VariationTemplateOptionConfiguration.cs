namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Catalog
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class VariationTemplateOptionConfiguration : IEntityTypeConfiguration<VariationTemplateOption>
    {
        public void Configure(EntityTypeBuilder<VariationTemplateOption> entity)
        {
            entity.ToTable("variation_template_options");
            entity.HasKey(option => option.Id);
            entity.Property(option => option.Id).HasColumnName("id");
            entity.Property(option => option.PublicId).HasColumnName("public_id");
            entity.Property(option => option.TemplateId).HasColumnName("template_id");
            entity.Property(option => option.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(option => option.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(option => option.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(option => option.ControlType)
                .HasColumnName("control_type")
                .HasMaxLength(32)
                .HasDefaultValue(VariationControlTypes.Dropdown)
                .IsRequired();
            entity.Property(option => option.IsRequired).HasColumnName("is_required").HasDefaultValue(true);
            entity.Property(option => option.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(option => option.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(option => option.PublicId).IsUnique();
            entity.HasIndex(option => new { option.TemplateId, option.Name }).IsUnique();
            entity.HasIndex(option => new { option.TemplateId, option.SortOrder });

            entity.HasOne(option => option.Template)
                .WithMany(template => template.Options)
                .HasForeignKey(option => option.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                table => table.HasCheckConstraint(
                    "ck_variation_template_option_control_type",
                    $"control_type in ({CommerceNodeSql.In(VariationControlTypes.All)})"));
        }
    }
}
