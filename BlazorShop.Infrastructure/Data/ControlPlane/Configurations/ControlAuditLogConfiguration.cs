namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlAuditLogConfiguration : IEntityTypeConfiguration<ControlAuditLog>
    {
        public void Configure(EntityTypeBuilder<ControlAuditLog> entity)
        {
            entity.ToTable(
                "control_audit_log",
                table => table.HasCheckConstraint(
                    "ck_control_audit_log_result",
                    "result in ('success', 'failure', 'denied')"));

            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(log => log.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(log => log.ActorAdminUserId).HasColumnName("actor_admin_user_id");
            entity.Property(log => log.ActorIdentityUserId).HasColumnName("actor_identity_user_id").HasColumnType("text");
            entity.Property(log => log.ActorEmail).HasColumnName("actor_email").HasColumnType("text");
            entity.Property(log => log.Action).HasColumnName("action").HasColumnType("text").IsRequired();
            entity.Property(log => log.EntityType).HasColumnName("entity_type").HasColumnType("text").IsRequired();
            entity.Property(log => log.EntityPublicId).HasColumnName("entity_public_id").HasColumnType("text");
            entity.Property(log => log.NodeId).HasColumnName("node_id");
            entity.Property(log => log.StoreId).HasColumnName("store_id");
            entity.Property(log => log.ControlActionId).HasColumnName("control_action_id");
            entity.Property(log => log.Result).HasColumnName("result").HasColumnType("text").IsRequired();
            entity.Property(log => log.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(log => log.IpAddress).HasColumnName("ip_address").HasColumnType("text");
            entity.Property(log => log.UserAgent).HasColumnName("user_agent").HasColumnType("text");
            entity.Property(log => log.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(log => log.PublicId).IsUnique();
            entity.HasIndex(log => log.ActorAdminUserId);
            entity.HasIndex(log => log.NodeId);
            entity.HasIndex(log => log.StoreId);
            entity.HasIndex(log => log.ControlActionId);
            entity.HasIndex(log => new { log.Action, log.CreatedAt });
            entity.HasIndex(log => new { log.ActorEmail, log.CreatedAt });
            entity.HasIndex(log => log.CreatedAt);
            entity.HasOne(log => log.ActorAdminUser)
                .WithMany()
                .HasForeignKey(log => log.ActorAdminUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(log => log.Node)
                .WithMany()
                .HasForeignKey(log => log.NodeId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(log => log.Store)
                .WithMany()
                .HasForeignKey(log => log.StoreId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(log => log.ControlAction)
                .WithMany()
                .HasForeignKey(log => log.ControlActionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
