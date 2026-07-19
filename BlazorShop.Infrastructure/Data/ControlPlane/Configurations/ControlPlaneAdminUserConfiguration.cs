namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlPlaneAdminUserConfiguration : IEntityTypeConfiguration<ControlPlaneAdminUser>
    {
        public void Configure(EntityTypeBuilder<ControlPlaneAdminUser> entity)
        {
            entity.ToTable(
                "control_plane_admin_user",
                table => table.HasCheckConstraint(
                    "ck_control_plane_admin_user_status",
                    "status in ('active', 'disabled', 'invited')"));

            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(user => user.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(user => user.IdentityUserId).HasColumnName("identity_user_id").HasColumnType("text").IsRequired();
            entity.Property(user => user.Email).HasColumnName("email").HasColumnType("text").IsRequired();
            entity.Property(user => user.DisplayName).HasColumnName("display_name").HasColumnType("text").IsRequired();
            entity.Property(user => user.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(user => user.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamp with time zone");
            entity.Property(user => user.StatusChangedAt).HasColumnName("status_changed_at").HasColumnType("timestamp with time zone");
            entity.Property(user => user.StatusChangedByAdminUserId).HasColumnName("status_changed_by_admin_user_id");
            entity.Property(user => user.StatusReason).HasColumnName("status_reason").HasColumnType("text");
            entity.Property(user => user.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(user => user.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(user => user.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(user => user.PublicId)
                .IsUnique()
                .HasDatabaseName("control_plane_admin_user_public_id_uq");

            entity.HasIndex(user => user.IdentityUserId).IsUnique();
            entity.HasIndex(user => user.Status)
                .HasDatabaseName("ix_control_plane_admin_user_status")
                .HasFilter("deleted_at is null");
            entity.HasIndex(user => user.StatusChangedByAdminUserId)
                .HasDatabaseName("ix_control_plane_admin_user_status_changed_by");
            entity.HasIndex(user => user.Email)
                .IsUnique()
                .HasDatabaseName("control_plane_admin_user_active_email_uq")
                .HasFilter("deleted_at is null");

            entity.HasOne(user => user.StatusChangedByAdminUser)
                .WithMany()
                .HasForeignKey(user => user.StatusChangedByAdminUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
