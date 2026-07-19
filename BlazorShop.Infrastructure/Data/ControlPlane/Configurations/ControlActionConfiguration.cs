namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlActionConfiguration : IEntityTypeConfiguration<ControlAction>
    {
        public void Configure(EntityTypeBuilder<ControlAction> entity)
        {
            entity.ToTable(
                "control_action",
                table => table.HasCheckConstraint(
                    "ck_control_action_status",
                    "status in ('queued', 'running', 'failed', 'succeeded', 'cancelled')"));

            entity.HasKey(action => action.Id);
            entity.Property(action => action.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(action => action.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(action => action.NodeId).HasColumnName("node_id");
            entity.Property(action => action.StoreId).HasColumnName("store_id");
            entity.Property(action => action.ActionType).HasColumnName("action_type").HasColumnType("text").IsRequired();
            entity.Property(action => action.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(action => action.IdempotencyKey).HasColumnName("idempotency_key").HasColumnType("text").IsRequired();
            entity.Property(action => action.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
            entity.Property(action => action.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
            entity.Property(action => action.ErrorCode).HasColumnName("error_code").HasColumnType("text");
            entity.Property(action => action.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
            entity.Property(action => action.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(action => action.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(action => action.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
            entity.Property(action => action.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(action => action.PublicId).IsUnique();
            entity.HasIndex(action => action.NodeId);
            entity.HasIndex(action => action.StoreId);
            entity.HasIndex(action => new { action.NodeId, action.IdempotencyKey }).IsUnique();
            entity.HasIndex(action => new { action.Status, action.CreatedAt });
            entity.HasOne(action => action.Node)
                .WithMany(node => node.Actions)
                .HasForeignKey(action => action.NodeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(action => action.Store)
                .WithMany()
                .HasForeignKey(action => action.StoreId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
