namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Tasks
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CommerceTaskConfiguration : IEntityTypeConfiguration<CommerceTask>
    {
        public void Configure(EntityTypeBuilder<CommerceTask> entity)
        {
            entity.ToTable("commerce_task");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Id).HasColumnName("id");
            entity.Property(task => task.PublicId).HasColumnName("public_id");
            entity.Property(task => task.TaskType).HasColumnName("task_type").IsRequired();
            entity.Property(task => task.Status).HasColumnName("status").IsRequired();
            entity.Property(task => task.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(task => task.LockKey).HasColumnName("lock_key");
            entity.Property(task => task.PayloadSchemaVersion).HasColumnName("payload_schema_version").IsRequired();
            entity.Property(task => task.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
            entity.Property(task => task.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
            entity.Property(task => task.ErrorCode).HasColumnName("error_code");
            entity.Property(task => task.ErrorMessage).HasColumnName("error_message");
            entity.Property(task => task.AttemptCount).HasColumnName("attempt_count");
            entity.Property(task => task.MaxAttempts).HasColumnName("max_attempts");
            entity.Property(task => task.NextAttemptAt).HasColumnName("next_attempt_at").HasColumnType("timestamp with time zone");
            entity.Property(task => task.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
            entity.Property(task => task.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
            entity.Property(task => task.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(task => task.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(task => task.CreatedBy).HasColumnName("created_by");
            entity.Property(task => task.CorrelationId).HasColumnName("correlation_id");
            entity.Property(task => task.CancelRequestedAt).HasColumnName("cancel_requested_at").HasColumnType("timestamp with time zone");
            entity.Property(task => task.CancelReason).HasColumnName("cancel_reason");
            entity.Property(task => task.WorkerId).HasColumnName("worker_id");
            entity.Property(task => task.LastHeartbeatAt).HasColumnName("last_heartbeat_at").HasColumnType("timestamp with time zone");

            entity.HasIndex(task => task.PublicId).IsUnique();
            entity.HasIndex(task => task.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(task => new { task.Status, task.NextAttemptAt });
            entity.HasIndex(task => task.TaskType);
            entity.HasIndex(task => new { task.LockKey, task.Status });
            entity.HasIndex(task => task.CorrelationId);

            entity.ToTable(table => table.HasCheckConstraint("ck_commerce_task_status", "status in ('pending', 'running', 'waiting_retry', 'succeeded', 'failed', 'cancelled', 'dead')"));
            entity.ToTable(table => table.HasCheckConstraint("ck_commerce_task_attempt_count", "attempt_count >= 0"));
            entity.ToTable(table => table.HasCheckConstraint("ck_commerce_task_max_attempts", "max_attempts >= 1"));
        }
    }
}
