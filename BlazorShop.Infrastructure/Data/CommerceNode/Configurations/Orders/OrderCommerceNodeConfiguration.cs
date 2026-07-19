namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations.Orders
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    internal sealed class OrderCommerceNodeConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> entity)
        {
            entity.Property(order => order.CustomerId).HasColumnName("customer_id");
            entity.HasIndex(order => order.CustomerId);
            entity.HasOne(order => order.Customer)
                .WithMany(customer => customer.Orders)
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(order => order.OrderStatus)
                .HasColumnName("order_status")
                .HasMaxLength(32)
                .HasDefaultValue(OrderStatuses.Pending);

            entity.Property(order => order.PaymentStatus)
                .HasColumnName("payment_status")
                .HasMaxLength(32)
                .HasDefaultValue(PaymentStatuses.Pending);

            entity.Property(order => order.PaymentMethodKey)
                .HasColumnName("payment_method_key")
                .HasMaxLength(64)
                .HasDefaultValue(PaymentMethodKeys.Cod);

            entity.Property(order => order.PaymentAt)
                .HasColumnName("payment_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(order => order.PaymentMetadataJson)
                .HasColumnName("payment_metadata_json")
                .HasColumnType("jsonb");

            entity.Property(order => order.StorePublicId)
                .HasColumnName("store_public_id");

            entity.Property(order => order.StoreKeySnapshot)
                .HasColumnName("store_key_snapshot")
                .HasMaxLength(128);

            entity.Property(order => order.StoreNameSnapshot)
                .HasColumnName("store_name_snapshot")
                .HasMaxLength(400);

            entity.Property(order => order.StoreBaseUrlSnapshot)
                .HasColumnName("store_base_url_snapshot")
                .HasMaxLength(2048);

            entity.Property(order => order.StoreCompanyNameSnapshot)
                .HasColumnName("store_company_name_snapshot")
                .HasMaxLength(200);

            entity.Property(order => order.StoreCompanyEmailSnapshot)
                .HasColumnName("store_company_email_snapshot")
                .HasMaxLength(254);

            entity.Property(order => order.StoreCompanyPhoneSnapshot)
                .HasColumnName("store_company_phone_snapshot")
                .HasMaxLength(50);

            entity.Property(order => order.StoreCompanyAddressSnapshot)
                .HasColumnName("store_company_address_snapshot")
                .HasMaxLength(500);

            entity.Property(order => order.CustomerName)
                .HasColumnName("customer_name")
                .HasMaxLength(256);

            entity.Property(order => order.CustomerEmail)
                .HasColumnName("customer_email")
                .HasMaxLength(256);

            entity.Property(order => order.BillingAddressSnapshotJson)
                .HasColumnName("billing_address_snapshot_json")
                .HasColumnType("jsonb");

            entity.Property(order => order.ShippingAddressSnapshotJson)
                .HasColumnName("shipping_address_snapshot_json")
                .HasColumnType("jsonb");

            entity.Property(order => order.ShippingFullName)
                .HasColumnName("shipping_full_name")
                .HasMaxLength(256)
                .HasDefaultValue(string.Empty);

            entity.Property(order => order.ShippingEmail)
                .HasColumnName("shipping_email")
                .HasMaxLength(256)
                .HasDefaultValue(string.Empty);

            entity.Property(order => order.ShippingPhone)
                .HasColumnName("shipping_phone")
                .HasMaxLength(64);

            entity.Property(order => order.ShippingAddress1)
                .HasColumnName("shipping_address1")
                .HasMaxLength(400)
                .HasDefaultValue(string.Empty);

            entity.Property(order => order.ShippingAddress2)
                .HasColumnName("shipping_address2")
                .HasMaxLength(400);

            entity.Property(order => order.ShippingCity)
                .HasColumnName("shipping_city")
                .HasMaxLength(160)
                .HasDefaultValue(string.Empty);

            entity.Property(order => order.ShippingState)
                .HasColumnName("shipping_state")
                .HasMaxLength(160);

            entity.Property(order => order.ShippingPostalCode)
                .HasColumnName("shipping_postal_code")
                .HasMaxLength(64)
                .HasDefaultValue(string.Empty);

            entity.Property(order => order.ShippingCountryCode)
                .HasColumnName("shipping_country_code")
                .HasMaxLength(2)
                .HasDefaultValue(string.Empty);

            entity.Property(order => order.ShippingMethodKey)
                .HasColumnName("shipping_method_key")
                .HasMaxLength(64);

            entity.Property(order => order.ShippingProviderSystemName)
                .HasColumnName("shipping_provider_system_name")
                .HasMaxLength(64);

            entity.Property(order => order.ShippingMethodCode)
                .HasColumnName("shipping_method_code")
                .HasMaxLength(64);

            entity.Property(order => order.ShippingMethodName)
                .HasColumnName("shipping_method_name")
                .HasMaxLength(128);

            entity.Property(order => order.ShippingTotal)
                .HasColumnName("shipping_total")
                .HasPrecision(18, 2);

            entity.Property(order => order.ShippingCurrencyCode)
                .HasColumnName("shipping_currency_code")
                .HasMaxLength(3);

            entity.Property(order => order.ShippingDeliveryEstimateText)
                .HasColumnName("shipping_delivery_estimate_text")
                .HasMaxLength(128);

            entity.Property(order => order.ShippingMethodSnapshotJson)
                .HasColumnName("shipping_method_snapshot_json")
                .HasColumnType("jsonb");

            entity.Property(order => order.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(order => order.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(order => order.CancelledAt)
                .HasColumnName("cancelled_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(order => order.AdminNote)
                .HasMaxLength(2000);

            entity.Property(order => order.CurrencyCode)
                .HasMaxLength(3);

            entity.Property(order => order.SubtotalAmount)
                .HasColumnName("subtotal_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.ShippingTotalAmount)
                .HasColumnName("shipping_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.TaxTotalAmount)
                .HasColumnName("tax_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.DiscountTotalAmount)
                .HasColumnName("discount_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.GrandTotalAmount)
                .HasColumnName("grand_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.BaseCurrencyCode)
                .HasMaxLength(3);

            entity.Property(order => order.BaseTotalAmount)
                .HasPrecision(18, 2);

            entity.Property(order => order.BaseSubtotalAmount)
                .HasColumnName("base_subtotal_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.BaseShippingTotalAmount)
                .HasColumnName("base_shipping_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.BaseTaxTotalAmount)
                .HasColumnName("base_tax_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.BaseDiscountTotalAmount)
                .HasColumnName("base_discount_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.BaseGrandTotalAmount)
                .HasColumnName("base_grand_total_amount")
                .HasPrecision(18, 2);

            entity.Property(order => order.ExchangeRate)
                .HasPrecision(18, 8);

            entity.Property(order => order.ExchangeRateProviderKey)
                .HasMaxLength(64);

            entity.Property(order => order.ExchangeRateSource)
                .HasMaxLength(256);

            entity.Property(order => order.ExchangeRateEffectiveAtUtc)
                .HasColumnType("timestamp with time zone");

            entity.Property(order => order.ExchangeRateExpiresAtUtc)
                .HasColumnType("timestamp with time zone");

            entity.Property(order => order.GuestAccessTokenHash)
                .HasColumnName("guest_access_token_hash")
                .HasMaxLength(64);

            entity.Property(order => order.GuestAccessTokenExpiresAtUtc)
                .HasColumnName("guest_access_token_expires_at_utc")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(order => order.Reference)
                .IsUnique();

            entity.HasIndex(order => new { order.StoreId, order.UserId, order.CreatedOn });
            entity.HasIndex(order => order.CreatedOn);
            entity.HasIndex(order => order.StoreId);
            entity.HasIndex(order => new { order.StoreId, order.OrderStatus, order.CreatedOn });
            entity.HasIndex(order => new { order.StoreId, order.PaymentStatus, order.CreatedOn });
            entity.HasIndex(order => new { order.StoreId, order.CustomerEmail, order.CreatedOn });
            entity.HasIndex(order => order.GuestAccessTokenHash)
                .IsUnique()
                .HasFilter("guest_access_token_hash IS NOT NULL");
            entity.HasIndex(order => order.PaymentMethodKey);

            entity.HasMany(order => order.Lines)
                .WithOne(line => line.Order!)
                .HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_orders_order_status",
                        "order_status in ('pending', 'processing', 'complete', 'cancelled')");
                    table.HasCheckConstraint(
                        "ck_orders_payment_status",
                        "payment_status in ('pending', 'authorized', 'paid', 'partially_refunded', 'refunded', 'voided')");
                    table.HasCheckConstraint(
                        "ck_orders_payment_method_key",
                        "payment_method_key in ('cod', 'stripe', 'paypal')");
                });
        }
    }
}
