namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Tasks
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CommerceTaskStepConfiguration : IEntityTypeConfiguration<CommerceTaskStep>
    {
        public void Configure(EntityTypeBuilder<CommerceTaskStep> entity)
        {
            entity.ToTable("commerce_task_step");
            entity.HasKey(step => step.Id);
            entity.Property(step => step.Id).HasColumnName("id");
            entity.Property(step => step.TaskId).HasColumnName("task_id");
            entity.Property(step => step.StepKey).HasColumnName("step_key").IsRequired();
            entity.Property(step => step.Status).HasColumnName("status").IsRequired();
            entity.Property(step => step.AttemptNumber).HasColumnName("attempt_number");
            entity.Property(step => step.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
            entity.Property(step => step.ErrorCode).HasColumnName("error_code");
            entity.Property(step => step.ErrorMessage).HasColumnName("error_message");
            entity.Property(step => step.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
            entity.Property(step => step.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");

            entity.HasOne(step => step.Task)
                .WithMany(task => task.Steps)
                .HasForeignKey(step => step.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(step => step.TaskId);
            entity.HasIndex(step => new { step.TaskId, step.StepKey, step.AttemptNumber });

            entity.ToTable(table => table.HasCheckConstraint("ck_commerce_task_step_status", "status in ('pending', 'running', 'succeeded', 'failed', 'skipped', 'rolled_back')"));
        }
    }
}
