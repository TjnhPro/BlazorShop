namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlPlanePermissionConfiguration : IEntityTypeConfiguration<ControlPlanePermission>
    {
        public void Configure(EntityTypeBuilder<ControlPlanePermission> entity)
        {
            entity.ToTable(
                "control_plane_permission",
                table => table.HasCheckConstraint("ck_control_plane_permission_key_lower", "key = lower(key)"));

            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(permission => permission.Key).HasColumnName("key").HasColumnType("text").IsRequired();
            entity.Property(permission => permission.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(permission => permission.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(permission => permission.Key).IsUnique();
        }
    }
}
