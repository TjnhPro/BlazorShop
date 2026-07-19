namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Payments
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
    {
        public void Configure(EntityTypeBuilder<PaymentAttempt> entity)
        {
            entity.ToTable("payment_attempts");
            entity.HasKey(attempt => attempt.Id);
            entity.Property(attempt => attempt.Id).HasColumnName("id");
            entity.Property(attempt => attempt.PublicId).HasColumnName("public_id");
            entity.Property(attempt => attempt.StoreId).HasColumnName("store_id");
            entity.Property(attempt => attempt.CheckoutSessionId).HasColumnName("checkout_session_id");
            entity.Property(attempt => attempt.OrderId).HasColumnName("order_id");
            entity.Property(attempt => attempt.PaymentMethodKey).HasColumnName("payment_method_key").HasMaxLength(64).IsRequired();
            entity.Property(attempt => attempt.ProviderKey).HasColumnName("provider_key").HasMaxLength(64).IsRequired();
            entity.Property(attempt => attempt.State).HasColumnName("state").HasMaxLength(32).IsRequired();
            entity.Property(attempt => attempt.Amount).HasColumnName("amount").HasPrecision(18, 2);
            entity.Property(attempt => attempt.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            entity.Property(attempt => attempt.BaseCurrencyCode).HasColumnName("base_currency_code").HasMaxLength(3);
            entity.Property(attempt => attempt.BaseAmount).HasColumnName("base_amount").HasPrecision(18, 2);
            entity.Property(attempt => attempt.ExchangeRate).HasColumnName("exchange_rate").HasPrecision(18, 8);
            entity.Property(attempt => attempt.ExchangeRateProviderKey).HasColumnName("exchange_rate_provider_key").HasMaxLength(64);
            entity.Property(attempt => attempt.ExchangeRateSource).HasColumnName("exchange_rate_source").HasMaxLength(256);
            entity.Property(attempt => attempt.ExchangeRateEffectiveAtUtc).HasColumnName("exchange_rate_effective_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(attempt => attempt.ExchangeRateExpiresAtUtc).HasColumnName("exchange_rate_expires_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(attempt => attempt.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
            entity.Property(attempt => attempt.ProviderReference).HasColumnName("provider_reference").HasMaxLength(256);
            entity.Property(attempt => attempt.ProviderSessionId).HasColumnName("provider_session_id").HasMaxLength(256);
            entity.Property(attempt => attempt.NextActionType).HasColumnName("next_action_type").HasMaxLength(64);
            entity.Property(attempt => attempt.NextActionUrl).HasColumnName("next_action_url").HasMaxLength(2048);
            entity.Property(attempt => attempt.FailureCode).HasColumnName("failure_code").HasMaxLength(128);
            entity.Property(attempt => attempt.FailureMessage).HasColumnName("failure_message").HasMaxLength(512);
            entity.Property(attempt => attempt.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(attempt => attempt.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(attempt => attempt.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(attempt => attempt.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(attempt => attempt.PublicId).IsUnique();
            entity.HasIndex(attempt => new { attempt.StoreId, attempt.IdempotencyKey }).IsUnique();
            entity.HasIndex(attempt => new { attempt.StoreId, attempt.State, attempt.CreatedAtUtc });
            entity.HasIndex(attempt => attempt.CheckoutSessionId);
            entity.HasIndex(attempt => attempt.OrderId);
            entity.HasIndex(attempt => new { attempt.ProviderKey, attempt.ProviderSessionId })
                .HasFilter("provider_session_id IS NOT NULL");

            entity.HasOne(attempt => attempt.Store)
                .WithMany()
                .HasForeignKey(attempt => attempt.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(attempt => attempt.CheckoutSession)
                .WithMany()
                .HasForeignKey(attempt => attempt.CheckoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(attempt => attempt.Order)
                .WithMany()
                .HasForeignKey(attempt => attempt.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(
                "payment_attempts",
                table => table.HasCheckConstraint(
                    "ck_payment_attempts_state",
                    "state in ('created', 'requires_action', 'authorized', 'captured', 'failed', 'cancelled', 'expired')"));
        }
    }
}
