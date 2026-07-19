namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlPlaneAdminUserRoleConfiguration : IEntityTypeConfiguration<ControlPlaneAdminUserRole>
    {
        public void Configure(EntityTypeBuilder<ControlPlaneAdminUserRole> entity)
        {
            entity.ToTable("control_plane_admin_user_role");
            entity.HasKey(userRole => new { userRole.AdminUserId, userRole.RoleId });
            entity.Property(userRole => userRole.AdminUserId).HasColumnName("admin_user_id");
            entity.Property(userRole => userRole.RoleId).HasColumnName("role_id");
            entity.Property(userRole => userRole.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(userRole => userRole.RoleId);
            entity.HasOne(userRole => userRole.AdminUser)
                .WithMany(user => user.Roles)
                .HasForeignKey(userRole => userRole.AdminUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
