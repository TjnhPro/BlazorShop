namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Checkout
{
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class CheckoutSessionConfiguration : IEntityTypeConfiguration<CheckoutSession>
    {
        public void Configure(EntityTypeBuilder<CheckoutSession> entity)
        {
            entity.ToTable("checkout_sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Id).HasColumnName("id");
            entity.Property(session => session.PublicId).HasColumnName("public_id");
            entity.Property(session => session.StoreId).HasColumnName("store_id");
            entity.Property(session => session.CartSessionId).HasColumnName("cart_session_id");
            entity.Property(session => session.CustomerId).HasColumnName("customer_id");
            entity.Property(session => session.OrderId).HasColumnName("order_id");
            entity.Property(session => session.State).HasColumnName("state").HasMaxLength(32).IsRequired();
            entity.Property(session => session.CheckoutVersion).HasColumnName("checkout_version").HasDefaultValue(1);
            entity.Property(session => session.CurrentStep).HasColumnName("current_step").HasMaxLength(64).HasDefaultValue("entry").IsRequired();
            entity.Property(session => session.CompletedStepsJson).HasColumnName("completed_steps_json").HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
            entity.Property(session => session.CartVersion).HasColumnName("cart_version");
            entity.Property(session => session.LastValidatedCartVersion).HasColumnName("last_validated_cart_version");
            entity.Property(session => session.CustomerEmail).HasColumnName("customer_email").HasMaxLength(256).IsRequired();
            entity.Property(session => session.CustomerName).HasColumnName("customer_name").HasMaxLength(256).IsRequired();
            entity.Property(session => session.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(64);
            entity.Property(session => session.BillingAddressSnapshotJson).HasColumnName("billing_address_snapshot_json").HasColumnType("jsonb");
            entity.Property(session => session.ShippingAddressSource).HasColumnName("shipping_address_source").HasMaxLength(64).HasDefaultValue("direct").IsRequired();
            entity.Property(session => session.ShippingFullName).HasColumnName("shipping_full_name").HasMaxLength(256).IsRequired();
            entity.Property(session => session.ShippingEmail).HasColumnName("shipping_email").HasMaxLength(256).IsRequired();
            entity.Property(session => session.ShippingPhone).HasColumnName("shipping_phone").HasMaxLength(64);
            entity.Property(session => session.ShippingAddress1).HasColumnName("shipping_address1").HasMaxLength(512).IsRequired();
            entity.Property(session => session.ShippingAddress2).HasColumnName("shipping_address2").HasMaxLength(512);
            entity.Property(session => session.ShippingCity).HasColumnName("shipping_city").HasMaxLength(160).IsRequired();
            entity.Property(session => session.ShippingState).HasColumnName("shipping_state").HasMaxLength(160);
            entity.Property(session => session.ShippingPostalCode).HasColumnName("shipping_postal_code").HasMaxLength(64).IsRequired();
            entity.Property(session => session.ShippingCountryCode).HasColumnName("shipping_country_code").HasMaxLength(2).IsRequired();
            entity.Property(session => session.SelectedShippingOptionJson).HasColumnName("selected_shipping_option_json").HasColumnType("jsonb");
            entity.Property(session => session.PaymentMethodKey).HasColumnName("payment_method_key").HasMaxLength(64).IsRequired();
            entity.Property(session => session.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
            entity.Property(session => session.ShippingTotal).HasColumnName("shipping_total").HasPrecision(18, 2);
            entity.Property(session => session.TaxTotal).HasColumnName("tax_total").HasPrecision(18, 2);
            entity.Property(session => session.DiscountTotal).HasColumnName("discount_total").HasPrecision(18, 2);
            entity.Property(session => session.GrandTotal).HasColumnName("grand_total").HasPrecision(18, 2);
            entity.Property(session => session.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            entity.Property(session => session.BaseCurrencyCode).HasColumnName("base_currency_code").HasMaxLength(3);
            entity.Property(session => session.BaseSubtotal).HasColumnName("base_subtotal").HasPrecision(18, 2);
            entity.Property(session => session.BaseGrandTotal).HasColumnName("base_grand_total").HasPrecision(18, 2);
            entity.Property(session => session.ExchangeRate).HasColumnName("exchange_rate").HasPrecision(18, 8);
            entity.Property(session => session.ExchangeRateProviderKey).HasColumnName("exchange_rate_provider_key").HasMaxLength(64);
            entity.Property(session => session.ExchangeRateSource).HasColumnName("exchange_rate_source").HasMaxLength(256);
            entity.Property(session => session.ExchangeRateEffectiveAtUtc).HasColumnName("exchange_rate_effective_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(session => session.ExchangeRateExpiresAtUtc).HasColumnName("exchange_rate_expires_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(session => session.ValidationIssuesJson).HasColumnName("validation_issues_json").HasColumnType("jsonb");
            entity.Property(session => session.TermsAccepted).HasColumnName("terms_accepted").HasDefaultValue(false);
            entity.Property(session => session.TermsVersion).HasColumnName("terms_version").HasMaxLength(64);
            entity.Property(session => session.TermsAcceptedAtUtc).HasColumnName("terms_accepted_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(session => session.NextAction).HasColumnName("next_action").HasMaxLength(64).IsRequired();
            entity.Property(session => session.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
            entity.Property(session => session.PlacedAtUtc).HasColumnName("placed_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(session => session.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(session => session.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(session => session.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(session => session.PublicId).IsUnique();
            entity.HasIndex(session => new { session.StoreId, session.CartSessionId, session.State });
            entity.HasIndex(session => session.CustomerId);
            entity.HasIndex(session => session.OrderId);
            entity.HasIndex(session => new { session.StoreId, session.IdempotencyKey })
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(session => session.ExpiresAtUtc);

            entity.HasOne(session => session.Store)
                .WithMany()
                .HasForeignKey(session => session.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(session => session.CartSession)
                .WithMany()
                .HasForeignKey(session => session.CartSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(session => session.Customer)
                .WithMany()
                .HasForeignKey(session => session.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(session => session.Order)
                .WithMany()
                .HasForeignKey(session => session.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.ToTable(
                "checkout_sessions",
                table => table.HasCheckConstraint(
                    "ck_checkout_sessions_state",
                    "state in ('draft', 'ready', 'order_pending', 'completed', 'expired', 'cancelled')"));
        }
    }
}
