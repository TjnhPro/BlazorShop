namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlPlaneRolePermissionConfiguration : IEntityTypeConfiguration<ControlPlaneRolePermission>
    {
        public void Configure(EntityTypeBuilder<ControlPlaneRolePermission> entity)
        {
            entity.ToTable("control_plane_role_permission");
            entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
            entity.Property(rolePermission => rolePermission.RoleId).HasColumnName("role_id");
            entity.Property(rolePermission => rolePermission.PermissionId).HasColumnName("permission_id");
            entity.Property(rolePermission => rolePermission.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(rolePermission => rolePermission.PermissionId);
            entity.HasOne(rolePermission => rolePermission.Role)
                .WithMany(role => role.Permissions)
                .HasForeignKey(rolePermission => rolePermission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rolePermission => rolePermission.Permission)
                .WithMany(permission => permission.Roles)
                .HasForeignKey(rolePermission => rolePermission.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
