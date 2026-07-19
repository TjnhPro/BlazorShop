namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlPlaneRoleConfiguration : IEntityTypeConfiguration<ControlPlaneRole>
    {
        public void Configure(EntityTypeBuilder<ControlPlaneRole> entity)
        {
            entity.ToTable(
                "control_plane_role",
                table => table.HasCheckConstraint("ck_control_plane_role_key_lower", "key = lower(key)"));

            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(role => role.Key).HasColumnName("key").HasColumnType("text").IsRequired();
            entity.Property(role => role.Name).HasColumnName("name").HasColumnType("text").IsRequired();
            entity.Property(role => role.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(role => role.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
            entity.Property(role => role.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(role => role.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(role => role.Key).IsUnique();
        }
    }
}
