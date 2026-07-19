namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Payments
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class PaymentAttemptAuditLogConfiguration : IEntityTypeConfiguration<PaymentAttemptAuditLog>
    {
        public void Configure(EntityTypeBuilder<PaymentAttemptAuditLog> entity)
        {
            entity.ToTable("payment_attempt_audit_logs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).HasColumnName("id");
            entity.Property(log => log.StoreId).HasColumnName("store_id");
            entity.Property(log => log.OrderId).HasColumnName("order_id");
            entity.Property(log => log.PaymentAttemptId).HasColumnName("payment_attempt_id");
            entity.Property(log => log.ProviderKey).HasColumnName("provider_key").HasMaxLength(64).IsRequired();
            entity.Property(log => log.EventType).HasColumnName("event_type").HasMaxLength(128).IsRequired();
            entity.Property(log => log.OldState).HasColumnName("old_state").HasMaxLength(32);
            entity.Property(log => log.NewState).HasColumnName("new_state").HasMaxLength(32).IsRequired();
            entity.Property(log => log.Message).HasColumnName("message").HasMaxLength(512).IsRequired();
            entity.Property(log => log.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(log => log.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(log => new { log.StoreId, log.PaymentAttemptId, log.CreatedAtUtc });
            entity.HasIndex(log => log.OrderId);

            entity.HasOne(log => log.Store)
                .WithMany()
                .HasForeignKey(log => log.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(log => log.PaymentAttempt)
                .WithMany()
                .HasForeignKey(log => log.PaymentAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(log => log.Order)
                .WithMany()
                .HasForeignKey(log => log.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
