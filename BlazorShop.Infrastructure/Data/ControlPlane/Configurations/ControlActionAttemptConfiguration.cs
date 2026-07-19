namespace BlazorShop.Infrastructure.Data.ControlPlane.Configurations
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.ControlPlane;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    internal sealed class ControlActionAttemptConfiguration : IEntityTypeConfiguration<ControlActionAttempt>
    {
        public void Configure(EntityTypeBuilder<ControlActionAttempt> entity)
        {
            entity.ToTable(
                "control_action_attempt",
                table => table.HasCheckConstraint(
                    "ck_control_action_attempt_status",
                    "status in ('running', 'failed', 'succeeded', 'cancelled')"));

            entity.HasKey(attempt => attempt.Id);
            entity.Property(attempt => attempt.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            entity.Property(attempt => attempt.ActionId).HasColumnName("action_id");
            entity.Property(attempt => attempt.AttemptNumber).HasColumnName("attempt_number");
            entity.Property(attempt => attempt.Status).HasColumnName("status").HasColumnType("text").IsRequired();
            entity.Property(attempt => attempt.HttpStatusCode).HasColumnName("http_status_code");
            entity.Property(attempt => attempt.DurationMs).HasColumnName("duration_ms");
            entity.Property(attempt => attempt.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb");
            entity.Property(attempt => attempt.ErrorCode).HasColumnName("error_code").HasColumnType("text");
            entity.Property(attempt => attempt.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
            entity.Property(attempt => attempt.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(attempt => attempt.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(attempt => attempt.ActionId);
            entity.HasIndex(attempt => new { attempt.ActionId, attempt.AttemptNumber }).IsUnique();
            entity.HasOne(attempt => attempt.Action)
                .WithMany(action => action.Attempts)
                .HasForeignKey(attempt => attempt.ActionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
