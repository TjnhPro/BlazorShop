namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Payments
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class PaymentProviderEventConfiguration : IEntityTypeConfiguration<PaymentProviderEvent>
    {
        public void Configure(EntityTypeBuilder<PaymentProviderEvent> entity)
        {
            entity.ToTable("payment_provider_events");
            entity.HasKey(paymentEvent => paymentEvent.Id);
            entity.Property(paymentEvent => paymentEvent.Id).HasColumnName("id");
            entity.Property(paymentEvent => paymentEvent.StoreId).HasColumnName("store_id");
            entity.Property(paymentEvent => paymentEvent.PaymentAttemptId).HasColumnName("payment_attempt_id");
            entity.Property(paymentEvent => paymentEvent.ProviderKey).HasColumnName("provider_key").HasMaxLength(64).IsRequired();
            entity.Property(paymentEvent => paymentEvent.EventId).HasColumnName("event_id").HasMaxLength(256);
            entity.Property(paymentEvent => paymentEvent.EventType).HasColumnName("event_type").HasMaxLength(128).IsRequired();
            entity.Property(paymentEvent => paymentEvent.PayloadHash).HasColumnName("payload_hash").HasMaxLength(64).IsRequired();
            entity.Property(paymentEvent => paymentEvent.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
            entity.Property(paymentEvent => paymentEvent.ProcessedAtUtc).HasColumnName("processed_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(paymentEvent => paymentEvent.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(paymentEvent => new { paymentEvent.ProviderKey, paymentEvent.EventId })
                .IsUnique()
                .HasFilter("event_id IS NOT NULL");
            entity.HasIndex(paymentEvent => paymentEvent.PaymentAttemptId);
            entity.HasIndex(paymentEvent => new { paymentEvent.StoreId, paymentEvent.ProviderKey, paymentEvent.CreatedAtUtc });

            entity.HasOne(paymentEvent => paymentEvent.Store)
                .WithMany()
                .HasForeignKey(paymentEvent => paymentEvent.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(paymentEvent => paymentEvent.PaymentAttempt)
                .WithMany()
                .HasForeignKey(paymentEvent => paymentEvent.PaymentAttemptId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
