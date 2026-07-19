namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlPlaneAdminUserPermissionConfiguration : IEntityTypeConfiguration<ControlPlaneAdminUserPermission>
    {
        public void Configure(EntityTypeBuilder<ControlPlaneAdminUserPermission> entity)
        {
            entity.ToTable("control_plane_admin_user_permission");
            entity.HasKey(userPermission => new { userPermission.AdminUserId, userPermission.PermissionId });
            entity.Property(userPermission => userPermission.AdminUserId).HasColumnName("admin_user_id");
            entity.Property(userPermission => userPermission.PermissionId).HasColumnName("permission_id");
            entity.Property(userPermission => userPermission.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(userPermission => userPermission.CreatedByAdminUserId).HasColumnName("created_by_admin_user_id");
            entity.HasIndex(userPermission => userPermission.PermissionId)
                .HasDatabaseName("ix_control_plane_admin_user_permission_permission_id");
            entity.HasIndex(userPermission => userPermission.CreatedByAdminUserId)
                .HasDatabaseName("ix_control_plane_admin_user_permission_created_by");
            entity.HasOne(userPermission => userPermission.AdminUser)
                .WithMany(user => user.DirectPermissions)
                .HasForeignKey(userPermission => userPermission.AdminUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(userPermission => userPermission.Permission)
                .WithMany(permission => permission.DirectUsers)
                .HasForeignKey(userPermission => userPermission.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(userPermission => userPermission.CreatedByAdminUser)
                .WithMany(user => user.CreatedDirectPermissionGrants)
                .HasForeignKey(userPermission => userPermission.CreatedByAdminUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
