namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.Configurations;
    using BlazorShop.Infrastructure.Data.Configurations.Admin;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeDbContext : IdentityDbContext<AppUser>
    {
        public CommerceNodeDbContext(DbContextOptions<CommerceNodeDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

        public DbSet<AdminSettings> AdminSettings => Set<AdminSettings>();

        public DbSet<Product> Products => Set<Product>();

        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

        public DbSet<StorePaymentMethod> StorePaymentMethods => Set<StorePaymentMethod>();

        public DbSet<OrderItem> CheckoutOrderItems => Set<OrderItem>();

        public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderLine> OrderLines => Set<OrderLine>();

        public DbSet<Shipment> Shipments => Set<Shipment>();

        public DbSet<SeoRedirect> SeoRedirects => Set<SeoRedirect>();

        public DbSet<SeoSettings> SeoSettings => Set<SeoSettings>();

        public DbSet<StoreSeoSettings> StoreSeoSettings => Set<StoreSeoSettings>();

        public DbSet<StoreFeatureState> StoreFeatureStates => Set<StoreFeatureState>();

        public DbSet<StoreCurrency> StoreCurrencies => Set<StoreCurrency>();

        public DbSet<StoreCurrencyExchangeRate> StoreCurrencyExchangeRates => Set<StoreCurrencyExchangeRate>();

        public DbSet<CommerceTask> CommerceTasks => Set<CommerceTask>();

        public DbSet<CommerceTaskStep> CommerceTaskSteps => Set<CommerceTaskStep>();

        public DbSet<StoreDeployment> StoreDeployments => Set<StoreDeployment>();

        public DbSet<CommerceStore> CommerceStores => Set<CommerceStore>();

        public DbSet<CommerceCustomer> CommerceCustomers => Set<CommerceCustomer>();

        public DbSet<CartSession> CartSessions => Set<CartSession>();

        public DbSet<CartLine> CartLines => Set<CartLine>();

        public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();

        public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();

        public DbSet<PaymentProviderEvent> PaymentProviderEvents => Set<PaymentProviderEvent>();

        public DbSet<CommerceStoreDomain> CommerceStoreDomains => Set<CommerceStoreDomain>();

        public DbSet<StorefrontDeploymentImage> StorefrontDeploymentImages => Set<StorefrontDeploymentImage>();

        public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();

        public DbSet<CommerceMediaAsset> CommerceMediaAssets => Set<CommerceMediaAsset>();

        public DbSet<CategoryMediaAssignment> CategoryMediaAssignments => Set<CategoryMediaAssignment>();

        public DbSet<StorefrontPage> StorefrontPages => Set<StorefrontPage>();

        public DbSet<StoreSeoSlugHistory> StoreSeoSlugHistories => Set<StoreSeoSlugHistory>();

        public DbSet<StoreNavigationMenu> StoreNavigationMenus => Set<StoreNavigationMenu>();

        public DbSet<StoreNavigationMenuItem> StoreNavigationMenuItems => Set<StoreNavigationMenuItem>();

        public DbSet<ProductImportJob> ProductImportJobs => Set<ProductImportJob>();

        public DbSet<ProductImportRow> ProductImportRows => Set<ProductImportRow>();

        public DbSet<VariationTemplate> VariationTemplates => Set<VariationTemplate>();

        public DbSet<VariationTemplateOption> VariationTemplateOptions => Set<VariationTemplateOption>();

        public DbSet<VariationTemplateValue> VariationTemplateValues => Set<VariationTemplateValue>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new SeoRedirectConfiguration());
            modelBuilder.ApplyConfiguration(new SeoSettingsConfiguration());
            modelBuilder.ApplyConfiguration(new StoreSeoSettingsConfiguration());
            modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new AdminSettingsConfiguration());

            modelBuilder.Entity<SeoRedirect>(entity =>
            {
                entity.HasOne<CommerceStore>()
                    .WithMany()
                    .HasForeignKey(redirect => redirect.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(category => category.StoreId).IsRequired();
                entity.HasOne<CommerceStore>()
                    .WithMany()
                    .HasForeignKey(category => category.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(product => product.StoreId).IsRequired();
                entity.HasOne<CommerceStore>()
                    .WithMany()
                    .HasForeignKey(product => product.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductVariant>()
                .Property(variant => variant.AttributesJson)
                .HasColumnType("jsonb");

            modelBuilder.Entity<ProductVariant>()
                .Property(variant => variant.AttributeSignature)
                .HasMaxLength(512);

            modelBuilder.Entity<ProductVariant>()
                .Property(variant => variant.DisplayName)
                .HasMaxLength(256);

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(variant => new { variant.ProductId, variant.AttributeSignature })
                .IsUnique()
                .HasFilter("\"AttributeSignature\" IS NOT NULL");

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(variant => variant.ProductId)
                .IsUnique()
                .HasFilter("\"IsDefault\" = TRUE");

            modelBuilder.Entity<ProductVariant>()
                .HasOne(variant => variant.Product)
                .WithMany(product => product.Variants)
                .HasForeignKey(variant => variant.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .Property(product => product.ProductType)
                .HasMaxLength(64)
                .HasDefaultValue(BlazorShop.Domain.Constants.ProductTypes.Simple);

            modelBuilder.Entity<Product>()
                .HasIndex(product => new { product.StoreId, product.ProductType });

            modelBuilder.Entity<Product>()
                .HasIndex(product => product.VariationTemplateId);

            modelBuilder.Entity<Product>()
                .HasOne(product => product.VariationTemplate)
                .WithMany(template => template.Products)
                .HasForeignKey(product => product.VariationTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CommerceCustomer>(entity =>
            {
                entity.ToTable("commerce_customers");
                entity.HasKey(customer => customer.Id);
                entity.Property(customer => customer.Id).HasColumnName("id");
                entity.Property(customer => customer.StoreId).HasColumnName("store_id");
                entity.Property(customer => customer.AppUserId).HasColumnName("app_user_id").HasMaxLength(450);
                entity.Property(customer => customer.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
                entity.Property(customer => customer.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256).IsRequired();
                entity.Property(customer => customer.FullName).HasColumnName("full_name").HasMaxLength(256).IsRequired();
                entity.Property(customer => customer.Phone).HasColumnName("phone").HasMaxLength(64);
                entity.Property(customer => customer.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(customer => customer.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(customer => customer.LastCheckoutAt).HasColumnName("last_checkout_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(customer => new { customer.StoreId, customer.NormalizedEmail }).IsUnique();
                entity.HasIndex(customer => customer.AppUserId).HasFilter("app_user_id IS NOT NULL");

                entity.HasOne(customer => customer.Store)
                    .WithMany()
                    .HasForeignKey(customer => customer.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(customer => customer.AppUser)
                    .WithMany()
                    .HasForeignKey(customer => customer.AppUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(order => order.CustomerId).HasColumnName("customer_id");
                entity.HasIndex(order => order.CustomerId);
                entity.HasOne(order => order.Customer)
                    .WithMany(customer => customer.Orders)
                    .HasForeignKey(order => order.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CartSession>(entity =>
            {
                entity.ToTable("cart_sessions");
                entity.HasKey(cart => cart.Id);
                entity.Property(cart => cart.Id).HasColumnName("id");
                entity.Property(cart => cart.PublicId).HasColumnName("public_id");
                entity.Property(cart => cart.StoreId).HasColumnName("store_id");
                entity.Property(cart => cart.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
                entity.Property(cart => cart.CustomerId).HasColumnName("customer_id");
                entity.Property(cart => cart.AppUserId).HasColumnName("app_user_id").HasMaxLength(450);
                entity.Property(cart => cart.State).HasColumnName("state").HasMaxLength(32).IsRequired();
                entity.Property(cart => cart.Version).HasColumnName("version").HasDefaultValue(1);
                entity.Property(cart => cart.LastActivityAtUtc).HasColumnName("last_activity_at_utc").HasColumnType("timestamp with time zone");
                entity.Property(cart => cart.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
                entity.Property(cart => cart.ConvertedOrderId).HasColumnName("converted_order_id");
                entity.Property(cart => cart.MergedIntoCartId).HasColumnName("merged_into_cart_id");
                entity.Property(cart => cart.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(cart => cart.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(cart => cart.PublicId).IsUnique();
                entity.HasIndex(cart => cart.TokenHash).IsUnique();
                entity.HasIndex(cart => new { cart.StoreId, cart.State });
                entity.HasIndex(cart => cart.CustomerId);
                entity.HasIndex(cart => cart.AppUserId).HasFilter("app_user_id IS NOT NULL");
                entity.HasIndex(cart => cart.ExpiresAtUtc);

                entity.HasOne(cart => cart.Store)
                    .WithMany()
                    .HasForeignKey(cart => cart.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cart => cart.Customer)
                    .WithMany()
                    .HasForeignKey(cart => cart.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(cart => cart.AppUser)
                    .WithMany()
                    .HasForeignKey(cart => cart.AppUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(cart => cart.ConvertedOrder)
                    .WithMany()
                    .HasForeignKey(cart => cart.ConvertedOrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(cart => cart.MergedIntoCart)
                    .WithMany()
                    .HasForeignKey(cart => cart.MergedIntoCartId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CartLine>(entity =>
            {
                entity.ToTable("cart_lines");
                entity.HasKey(line => line.Id);
                entity.Property(line => line.Id).HasColumnName("id");
                entity.Property(line => line.CartSessionId).HasColumnName("cart_session_id");
                entity.Property(line => line.ProductId).HasColumnName("product_id");
                entity.Property(line => line.ProductVariantId).HasColumnName("product_variant_id");
                entity.Property(line => line.LineKey).HasColumnName("line_key").HasMaxLength(64).IsRequired();
                entity.Property(line => line.SelectedAttributesJson).HasColumnName("selected_attributes_json").HasColumnType("jsonb");
                entity.Property(line => line.PersonalizationHash).HasColumnName("personalization_hash").HasMaxLength(128);
                entity.Property(line => line.PersonalizationJson).HasColumnName("personalization_json").HasColumnType("jsonb");
                entity.Property(line => line.ArtworkAssetId).HasColumnName("artwork_asset_id");
                entity.Property(line => line.ArtworkVersion).HasColumnName("artwork_version");
                entity.Property(line => line.FulfillmentProviderKey).HasColumnName("fulfillment_provider_key").HasMaxLength(64);
                entity.Property(line => line.Quantity).HasColumnName("quantity");
                entity.Property(line => line.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasPrecision(18, 2);
                entity.Property(line => line.CurrencyCodeSnapshot).HasColumnName("currency_code_snapshot").HasMaxLength(3);
                entity.Property(line => line.BaseUnitPriceSnapshot).HasColumnName("base_unit_price_snapshot").HasPrecision(18, 2);
                entity.Property(line => line.BaseCurrencyCodeSnapshot).HasColumnName("base_currency_code_snapshot").HasMaxLength(3);
                entity.Property(line => line.ExchangeRateSnapshot).HasColumnName("exchange_rate_snapshot").HasPrecision(18, 8);
                entity.Property(line => line.ExchangeRateProviderKey).HasColumnName("exchange_rate_provider_key").HasMaxLength(64);
                entity.Property(line => line.ExchangeRateSource).HasColumnName("exchange_rate_source").HasMaxLength(256);
                entity.Property(line => line.ExchangeRateEffectiveAtUtc).HasColumnName("exchange_rate_effective_at_utc").HasColumnType("timestamp with time zone");
                entity.Property(line => line.ExchangeRateExpiresAtUtc).HasColumnName("exchange_rate_expires_at_utc").HasColumnType("timestamp with time zone");
                entity.Property(line => line.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(line => line.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(line => new { line.CartSessionId, line.LineKey }).IsUnique();
                entity.HasIndex(line => line.ProductId);
                entity.HasIndex(line => line.ProductVariantId);
                entity.HasIndex(line => line.ArtworkAssetId);

                entity.HasOne(line => line.CartSession)
                    .WithMany(cart => cart.Lines)
                    .HasForeignKey(line => line.CartSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(line => line.Product)
                    .WithMany()
                    .HasForeignKey(line => line.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(line => line.ProductVariant)
                    .WithMany()
                    .HasForeignKey(line => line.ProductVariantId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CheckoutSession>(entity =>
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
                entity.Property(session => session.CartVersion).HasColumnName("cart_version");
                entity.Property(session => session.CustomerEmail).HasColumnName("customer_email").HasMaxLength(256).IsRequired();
                entity.Property(session => session.CustomerName).HasColumnName("customer_name").HasMaxLength(256).IsRequired();
                entity.Property(session => session.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(64);
                entity.Property(session => session.ShippingFullName).HasColumnName("shipping_full_name").HasMaxLength(256).IsRequired();
                entity.Property(session => session.ShippingEmail).HasColumnName("shipping_email").HasMaxLength(256).IsRequired();
                entity.Property(session => session.ShippingPhone).HasColumnName("shipping_phone").HasMaxLength(64);
                entity.Property(session => session.ShippingAddress1).HasColumnName("shipping_address1").HasMaxLength(512).IsRequired();
                entity.Property(session => session.ShippingAddress2).HasColumnName("shipping_address2").HasMaxLength(512);
                entity.Property(session => session.ShippingCity).HasColumnName("shipping_city").HasMaxLength(160).IsRequired();
                entity.Property(session => session.ShippingState).HasColumnName("shipping_state").HasMaxLength(160);
                entity.Property(session => session.ShippingPostalCode).HasColumnName("shipping_postal_code").HasMaxLength(64).IsRequired();
                entity.Property(session => session.ShippingCountryCode).HasColumnName("shipping_country_code").HasMaxLength(2).IsRequired();
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
            });

            modelBuilder.Entity<PaymentAttempt>(entity =>
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
            });

            modelBuilder.Entity<PaymentProviderEvent>(entity =>
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
            });

            modelBuilder.Entity<VariationTemplate>(entity =>
            {
                entity.ToTable("variation_templates");
                entity.HasKey(template => template.Id);
                entity.Property(template => template.Id).HasColumnName("id");
                entity.Property(template => template.PublicId).HasColumnName("public_id");
                entity.Property(template => template.StoreId).HasColumnName("store_id");
                entity.Property(template => template.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
                entity.Property(template => template.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
                entity.Property(template => template.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(template => template.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(template => template.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(template => template.PublicId).IsUnique();
                entity.HasIndex(template => new { template.StoreId, template.Slug }).IsUnique();
                entity.HasIndex(template => new { template.StoreId, template.IsActive });
            });

            modelBuilder.Entity<VariationTemplateOption>(entity =>
            {
                entity.ToTable("variation_template_options");
                entity.HasKey(option => option.Id);
                entity.Property(option => option.Id).HasColumnName("id");
                entity.Property(option => option.PublicId).HasColumnName("public_id");
                entity.Property(option => option.TemplateId).HasColumnName("template_id");
                entity.Property(option => option.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(option => option.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
                entity.Property(option => option.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(option => option.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(option => option.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(option => option.PublicId).IsUnique();
                entity.HasIndex(option => new { option.TemplateId, option.Name }).IsUnique();
                entity.HasIndex(option => new { option.TemplateId, option.SortOrder });

                entity.HasOne(option => option.Template)
                    .WithMany(template => template.Options)
                    .HasForeignKey(option => option.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VariationTemplateValue>(entity =>
            {
                entity.ToTable("variation_template_values");
                entity.HasKey(value => value.Id);
                entity.Property(value => value.Id).HasColumnName("id");
                entity.Property(value => value.PublicId).HasColumnName("public_id");
                entity.Property(value => value.OptionId).HasColumnName("option_id");
                entity.Property(value => value.Value).HasColumnName("value").HasMaxLength(200).IsRequired();
                entity.Property(value => value.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
                entity.Property(value => value.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(value => value.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(value => value.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(value => value.PublicId).IsUnique();
                entity.HasIndex(value => new { value.OptionId, value.Value }).IsUnique();
                entity.HasIndex(value => new { value.OptionId, value.SortOrder });

                entity.HasOne(value => value.Option)
                    .WithMany(option => option.Values)
                    .HasForeignKey(value => value.OptionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductMedia>(entity =>
            {
                entity.ToTable("product_media");
                entity.HasKey(media => media.Id);
                entity.Property(media => media.Id).HasColumnName("id");
                entity.Property(media => media.PublicId).HasColumnName("public_id");
                entity.Property(media => media.StoreId).HasColumnName("store_id");
                entity.Property(media => media.ProductId).HasColumnName("product_id");
                entity.Property(media => media.OriginalSourceUrl).HasColumnName("original_source_url");
                entity.Property(media => media.OriginalStoragePath).HasColumnName("original_storage_path");
                entity.Property(media => media.ContentHash).HasColumnName("content_hash").HasMaxLength(128);
                entity.Property(media => media.FileName).HasColumnName("file_name");
                entity.Property(media => media.MimeType).HasColumnName("mime_type").HasMaxLength(128);
                entity.Property(media => media.Width).HasColumnName("width");
                entity.Property(media => media.Height).HasColumnName("height");
                entity.Property(media => media.FileSizeBytes).HasColumnName("file_size_bytes");
                entity.Property(media => media.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
                entity.Property(media => media.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
                entity.Property(media => media.AltText).HasColumnName("alt_text");
                entity.Property(media => media.Status).HasColumnName("status").IsRequired();
                entity.Property(media => media.ErrorMessage).HasColumnName("error_message");
                entity.Property(media => media.Version).HasColumnName("version").HasDefaultValue(1);
                entity.Property(media => media.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(media => media.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(media => media.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamp with time zone");
                entity.Property(media => media.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(media => media.PublicId).IsUnique();
                entity.HasIndex(media => new { media.StoreId, media.ProductId, media.SortOrder });
                entity.HasIndex(media => new { media.StoreId, media.ProductId, media.Status });
                entity.HasIndex(media => new { media.StoreId, media.ProductId, media.IsPrimary })
                    .IsUnique()
                    .HasFilter("deleted_at IS NULL AND is_primary = TRUE");
                entity.HasIndex(media => new { media.StoreId, media.ContentHash })
                    .HasFilter("content_hash IS NOT NULL");
                entity.HasIndex(media => media.Status);
                entity.HasIndex(media => media.DeletedAt);

                entity.HasOne(media => media.Product)
                    .WithMany()
                    .HasForeignKey(media => media.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    "product_media",
                    table =>
                    {
                        table.HasCheckConstraint(
                            "ck_product_media_status",
                            "status in ('pending', 'downloading', 'stored', 'failed', 'deleted')");
                        table.HasCheckConstraint("ck_product_media_sort_order", "sort_order >= 0");
                        table.HasCheckConstraint("ck_product_media_version", "version >= 1");
                        table.HasCheckConstraint("ck_product_media_width", "width IS NULL OR width > 0");
                        table.HasCheckConstraint("ck_product_media_height", "height IS NULL OR height > 0");
                        table.HasCheckConstraint("ck_product_media_file_size", "file_size_bytes IS NULL OR file_size_bytes > 0");
                    });
            });

            modelBuilder.Entity<CommerceMediaAsset>(entity =>
            {
                entity.ToTable("commerce_media_asset");
                entity.HasKey(asset => asset.Id);
                entity.Property(asset => asset.Id).HasColumnName("id");
                entity.Property(asset => asset.PublicId).HasColumnName("public_id");
                entity.Property(asset => asset.StoreId).HasColumnName("store_id");
                entity.Property(asset => asset.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260).IsRequired();
                entity.Property(asset => asset.CanonicalFileName).HasColumnName("canonical_file_name").HasMaxLength(260).IsRequired();
                entity.Property(asset => asset.DisplayName).HasColumnName("display_name").HasMaxLength(260).IsRequired();
                entity.Property(asset => asset.AltText).HasColumnName("alt_text").HasMaxLength(500).IsRequired();
                entity.Property(asset => asset.TitleText).HasColumnName("title_text").HasMaxLength(500);
                entity.Property(asset => asset.UsageType).HasColumnName("usage_type").HasMaxLength(32).HasDefaultValue(CommerceMediaAssetUsageTypes.Content).IsRequired();
                entity.Property(asset => asset.OriginalStoragePath).HasColumnName("original_storage_path").IsRequired();
                entity.Property(asset => asset.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
                entity.Property(asset => asset.MimeType).HasColumnName("mime_type").HasMaxLength(128).IsRequired();
                entity.Property(asset => asset.Extension).HasColumnName("extension").HasMaxLength(16).IsRequired();
                entity.Property(asset => asset.Width).HasColumnName("width");
                entity.Property(asset => asset.Height).HasColumnName("height");
                entity.Property(asset => asset.FileSizeBytes).HasColumnName("file_size_bytes");
                entity.Property(asset => asset.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(asset => asset.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(asset => asset.PublicId).IsUnique();
                entity.HasIndex(asset => new { asset.StoreId, asset.UpdatedAt });
                entity.HasIndex(asset => new { asset.StoreId, asset.UsageType, asset.UpdatedAt });
                entity.HasIndex(asset => new { asset.StoreId, asset.CanonicalFileName });
                entity.HasIndex(asset => new { asset.StoreId, asset.ContentHash });

                entity.ToTable(
                    "commerce_media_asset",
                    table =>
                    {
                        table.HasCheckConstraint("ck_commerce_media_asset_file_size", "file_size_bytes > 0");
                        table.HasCheckConstraint("ck_commerce_media_asset_width", "width IS NULL OR width > 0");
                        table.HasCheckConstraint("ck_commerce_media_asset_height", "height IS NULL OR height > 0");
                        table.HasCheckConstraint("ck_commerce_media_asset_usage_type", "usage_type in ('content', 'branding', 'theme', 'category')");
                    });
            });

            modelBuilder.Entity<CategoryMediaAssignment>(entity =>
            {
                entity.ToTable("category_media_assignment");
                entity.HasKey(assignment => assignment.Id);
                entity.Property(assignment => assignment.Id).HasColumnName("id");
                entity.Property(assignment => assignment.StoreId).HasColumnName("store_id");
                entity.Property(assignment => assignment.CategoryId).HasColumnName("category_id");
                entity.Property(assignment => assignment.MediaAssetId).HasColumnName("media_asset_id");
                entity.Property(assignment => assignment.AltText).HasColumnName("alt_text").HasMaxLength(500);
                entity.Property(assignment => assignment.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
                entity.Property(assignment => assignment.IsPrimary).HasColumnName("is_primary").HasDefaultValue(true);
                entity.Property(assignment => assignment.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(assignment => assignment.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(assignment => new { assignment.StoreId, assignment.CategoryId, assignment.IsPrimary })
                    .IsUnique()
                    .HasFilter("is_primary = TRUE");
                entity.HasIndex(assignment => new { assignment.StoreId, assignment.MediaAssetId });
                entity.HasIndex(assignment => new { assignment.StoreId, assignment.CategoryId, assignment.SortOrder });

                entity.HasOne(assignment => assignment.Category)
                    .WithMany()
                    .HasForeignKey(assignment => assignment.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(assignment => assignment.MediaAsset)
                    .WithMany()
                    .HasForeignKey(assignment => assignment.MediaAssetId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(
                    "category_media_assignment",
                    table =>
                    {
                        table.HasCheckConstraint("ck_category_media_assignment_sort_order", "sort_order >= 0");
                    });
            });

            modelBuilder.Entity<StorefrontPage>(entity =>
            {
                entity.ToTable("storefront_page");
                entity.HasKey(page => page.Id);
                entity.Property(page => page.Id).HasColumnName("id");
                entity.Property(page => page.PublicId).HasColumnName("public_id");
                entity.Property(page => page.StoreId).HasColumnName("store_id");
                entity.Property(page => page.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
                entity.Property(page => page.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
                entity.Property(page => page.Intro).HasColumnName("intro").HasMaxLength(1000);
                entity.Property(page => page.BodyHtml).HasColumnName("body_html").IsRequired();
                entity.Property(page => page.IsPublished).HasColumnName("is_published").HasDefaultValue(false);
                entity.Property(page => page.IncludeInSitemap).HasColumnName("include_in_sitemap").HasDefaultValue(false);
                entity.Property(page => page.PageKey).HasColumnName("page_key").HasMaxLength(80);
                entity.Property(page => page.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
                entity.Property(page => page.IncludeInNavigation).HasColumnName("include_in_navigation").HasDefaultValue(false);
                entity.Property(page => page.NavigationLocation).HasColumnName("navigation_location").HasMaxLength(50);
                entity.Property(page => page.MetaTitle).HasColumnName("meta_title").HasMaxLength(400);
                entity.Property(page => page.MetaDescription).HasColumnName("meta_description").HasMaxLength(4000);
                entity.Property(page => page.CanonicalUrl).HasColumnName("canonical_url").HasMaxLength(2048);
                entity.Property(page => page.OgTitle).HasColumnName("og_title").HasMaxLength(400);
                entity.Property(page => page.OgDescription).HasColumnName("og_description").HasMaxLength(4000);
                entity.Property(page => page.OgImage).HasColumnName("og_image").HasMaxLength(2048);
                entity.Property(page => page.RobotsIndex).HasColumnName("robots_index").HasDefaultValue(true);
                entity.Property(page => page.RobotsFollow).HasColumnName("robots_follow").HasDefaultValue(true);
                entity.Property(page => page.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(page => page.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(page => page.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(page => page.PublicId).IsUnique();
                entity.HasIndex(page => new { page.StoreId, page.Slug }).IsUnique();
                entity.HasIndex(page => new { page.StoreId, page.PageKey })
                    .IsUnique()
                    .HasFilter("page_key IS NOT NULL AND archived_at IS NULL");
                entity.HasIndex(page => new { page.StoreId, page.PageKey, page.ArchivedAt });
                entity.HasIndex(page => new { page.StoreId, page.IncludeInNavigation, page.IsPublished, page.ArchivedAt, page.DisplayOrder });
                entity.HasIndex(page => new { page.StoreId, page.IsPublished, page.ArchivedAt });
                entity.HasIndex(page => new { page.StoreId, page.IncludeInSitemap, page.IsPublished, page.ArchivedAt });
                entity.HasIndex(page => new { page.StoreId, page.UpdatedAt });

                entity.HasOne(page => page.Store)
                    .WithMany()
                    .HasForeignKey(page => page.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StoreSeoSlugHistory>(entity =>
            {
                entity.ToTable("store_seo_slug_history");
                entity.HasKey(history => history.Id);
                entity.Property(history => history.Id).HasColumnName("id");
                entity.Property(history => history.StoreId).HasColumnName("store_id");
                entity.Property(history => history.EntityType).HasColumnName("entity_type").HasMaxLength(64).IsRequired();
                entity.Property(history => history.EntityId).HasColumnName("entity_id");
                entity.Property(history => history.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
                entity.Property(history => history.LanguageCode).HasColumnName("language_code").HasMaxLength(20);
                entity.Property(history => history.IsActive).HasColumnName("is_active");
                entity.Property(history => history.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(history => history.ReplacedAt).HasColumnName("replaced_at").HasColumnType("timestamp with time zone");
                entity.Property(history => history.ReplacedBySlug).HasColumnName("replaced_by_slug").HasMaxLength(200);

                entity.HasOne(history => history.Store)
                    .WithMany()
                    .HasForeignKey(history => history.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(history => new { history.StoreId, history.EntityType, history.EntityId, history.LanguageCode })
                    .IsUnique()
                    .HasFilter("is_active = true");
                entity.HasIndex(history => new { history.StoreId, history.EntityType, history.Slug, history.LanguageCode })
                    .IsUnique()
                    .HasFilter("is_active = true");
                entity.HasIndex(history => new { history.StoreId, history.EntityType, history.EntityId, history.CreatedAt });
            });

            modelBuilder.Entity<StoreNavigationMenu>(entity =>
            {
                entity.ToTable("store_navigation_menu");
                entity.HasKey(menu => menu.Id);
                entity.Property(menu => menu.Id).HasColumnName("id");
                entity.Property(menu => menu.PublicId).HasColumnName("public_id");
                entity.Property(menu => menu.StoreId).HasColumnName("store_id");
                entity.Property(menu => menu.SystemName).HasColumnName("system_name").HasMaxLength(80).IsRequired();
                entity.Property(menu => menu.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
                entity.Property(menu => menu.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
                entity.Property(menu => menu.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(menu => menu.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(menu => menu.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(menu => menu.PublicId).IsUnique();
                entity.HasIndex(menu => new { menu.StoreId, menu.SystemName })
                    .IsUnique()
                    .HasFilter("archived_at IS NULL");
                entity.HasIndex(menu => new { menu.StoreId, menu.IsEnabled });

                entity.HasOne(menu => menu.Store)
                    .WithMany()
                    .HasForeignKey(menu => menu.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    "store_navigation_menu",
                    table =>
                    {
                        table.HasCheckConstraint(
                            "ck_store_navigation_menu_system_name",
                            $"system_name in ({SqlIn(StoreNavigationMenuNames.All)})");
                    });
            });

            modelBuilder.Entity<StoreNavigationMenuItem>(entity =>
            {
                entity.ToTable("store_navigation_menu_item");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Id).HasColumnName("id");
                entity.Property(item => item.PublicId).HasColumnName("public_id");
                entity.Property(item => item.StoreId).HasColumnName("store_id");
                entity.Property(item => item.MenuId).HasColumnName("menu_id");
                entity.Property(item => item.ParentItemId).HasColumnName("parent_item_id");
                entity.Property(item => item.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
                entity.Property(item => item.TargetType).HasColumnName("target_type").HasMaxLength(50).IsRequired();
                entity.Property(item => item.TargetKey).HasColumnName("target_key").HasMaxLength(120);
                entity.Property(item => item.TargetEntityPublicId).HasColumnName("target_entity_public_id");
                entity.Property(item => item.Url).HasColumnName("url").HasMaxLength(2048);
                entity.Property(item => item.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
                entity.Property(item => item.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
                entity.Property(item => item.OpensInNewTab).HasColumnName("opens_in_new_tab").HasDefaultValue(false);
                entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(item => item.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(item => item.PublicId).IsUnique();
                entity.HasIndex(item => new { item.StoreId, item.MenuId, item.ParentItemId, item.DisplayOrder });
                entity.HasIndex(item => new { item.StoreId, item.TargetType, item.TargetEntityPublicId });
                entity.HasIndex(item => new { item.MenuId, item.IsEnabled, item.ArchivedAt });

                entity.HasOne(item => item.Store)
                    .WithMany()
                    .HasForeignKey(item => item.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(item => item.Menu)
                    .WithMany(menu => menu.Items)
                    .HasForeignKey(item => item.MenuId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(item => item.ParentItem)
                    .WithMany(item => item.Children)
                    .HasForeignKey(item => item.ParentItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(
                    "store_navigation_menu_item",
                    table =>
                    {
                        table.HasCheckConstraint(
                            "ck_store_navigation_menu_item_target_type",
                            $"target_type in ({SqlIn(StoreNavigationTargetTypes.All)})");
                        table.HasCheckConstraint("ck_store_navigation_menu_item_display_order", "display_order >= 0");
                        table.HasCheckConstraint(
                            "ck_store_navigation_menu_item_external_url",
                            "target_type <> 'external_url' OR url LIKE 'https://%'");
                        table.HasCheckConstraint(
                            "ck_store_navigation_menu_item_group_shape",
                            "target_type <> 'group' OR (target_key IS NULL AND target_entity_public_id IS NULL AND url IS NULL)");
                    });
            });

            modelBuilder.Entity<ProductImportJob>(entity =>
            {
                entity.ToTable("product_import_job");
                entity.HasKey(job => job.Id);
                entity.Property(job => job.Id).HasColumnName("id");
                entity.Property(job => job.PublicId).HasColumnName("public_id");
                entity.Property(job => job.StoreId).HasColumnName("store_id");
                entity.Property(job => job.TaskPublicId).HasColumnName("task_public_id");
                entity.Property(job => job.Mode).HasColumnName("mode").HasMaxLength(32).IsRequired();
                entity.Property(job => job.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
                entity.Property(job => job.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
                entity.Property(job => job.StoredFilePath).HasColumnName("stored_file_path").IsRequired();
                entity.Property(job => job.FileHash).HasColumnName("file_hash").HasMaxLength(128).IsRequired();
                entity.Property(job => job.FileSizeBytes).HasColumnName("file_size_bytes");
                entity.Property(job => job.TotalRows).HasColumnName("total_rows");
                entity.Property(job => job.CreatedCount).HasColumnName("created_count");
                entity.Property(job => job.UpdatedCount).HasColumnName("updated_count");
                entity.Property(job => job.FailedCount).HasColumnName("failed_count");
                entity.Property(job => job.SkippedCount).HasColumnName("skipped_count");
                entity.Property(job => job.MediaQueuedCount).HasColumnName("media_queued_count");
                entity.Property(job => job.ErrorMessage).HasColumnName("error_message");
                entity.Property(job => job.ErrorJson).HasColumnName("error_json").HasColumnType("jsonb");
                entity.Property(job => job.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
                entity.Property(job => job.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(job => job.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
                entity.Property(job => job.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
                entity.Property(job => job.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(job => job.PublicId).IsUnique();
                entity.HasIndex(job => new { job.StoreId, job.Mode, job.FileHash }).IsUnique();
                entity.HasIndex(job => new { job.StoreId, job.Status, job.CreatedAt });
                entity.HasIndex(job => job.TaskPublicId);

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_product_import_job_status",
                        "status in ('Queued', 'Running', 'Completed', 'CompletedWithErrors', 'Failed')"));
                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_product_import_job_mode",
                        "mode in ('create_only', 'upsert')"));
            });

            modelBuilder.Entity<ProductImportRow>(entity =>
            {
                entity.ToTable("product_import_row");
                entity.HasKey(row => row.Id);
                entity.Property(row => row.Id).HasColumnName("id");
                entity.Property(row => row.JobId).HasColumnName("job_id");
                entity.Property(row => row.RowNumber).HasColumnName("row_number");
                entity.Property(row => row.Sku).HasColumnName("sku").HasMaxLength(64);
                entity.Property(row => row.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
                entity.Property(row => row.Action).HasColumnName("action").HasMaxLength(64).IsRequired();
                entity.Property(row => row.ProductId).HasColumnName("product_id");
                entity.Property(row => row.MediaStatus).HasColumnName("media_status").HasMaxLength(64).IsRequired();
                entity.Property(row => row.MediaTaskPublicId).HasColumnName("media_task_public_id");
                entity.Property(row => row.ErrorMessage).HasColumnName("error_message");
                entity.Property(row => row.ErrorJson).HasColumnName("error_json").HasColumnType("jsonb");
                entity.Property(row => row.RawDataJson).HasColumnName("raw_data_json").HasColumnType("jsonb");
                entity.Property(row => row.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(row => row.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(row => new { row.JobId, row.RowNumber }).IsUnique();
                entity.HasIndex(row => new { row.JobId, row.Status });
                entity.HasIndex(row => new { row.JobId, row.Sku });
                entity.HasIndex(row => row.ProductId);
                entity.HasIndex(row => row.MediaTaskPublicId);

                entity.HasOne(row => row.Job)
                    .WithMany(job => job.Rows)
                    .HasForeignKey(row => row.JobId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_product_import_row_status",
                        "status in ('Pending', 'Succeeded', 'Failed', 'Skipped')"));
                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_product_import_row_action",
                        "action in ('Created', 'Updated', 'Skipped', 'Failed')"));
                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_product_import_row_media_status",
                        "media_status in ('None', 'Queued')"));
            });

            modelBuilder.Entity<IdentityUserLogin<string>>()
                .Property(login => login.LoginProvider)
                .HasMaxLength(128);

            modelBuilder.Entity<IdentityUserLogin<string>>()
                .Property(login => login.ProviderKey)
                .HasMaxLength(128);

            modelBuilder.Entity<IdentityUserToken<string>>()
                .Property(token => token.LoginProvider)
                .HasMaxLength(128);

            modelBuilder.Entity<IdentityUserToken<string>>()
                .Property(token => token.Name)
                .HasMaxLength(128);

            modelBuilder.Entity<RefreshToken>()
                .Property(token => token.TokenHash)
                .HasMaxLength(64);

            modelBuilder.Entity<RefreshToken>()
                .Property(token => token.ReplacedByTokenHash)
                .HasMaxLength(64);

            modelBuilder.Entity<RefreshToken>()
                .Property(token => token.CreatedByIp)
                .HasMaxLength(64);

            modelBuilder.Entity<RefreshToken>()
                .Property(token => token.RevokedByIp)
                .HasMaxLength(64);

            modelBuilder.Entity<RefreshToken>()
                .Property(token => token.UserAgent)
                .HasMaxLength(512);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(token => token.TokenHash)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(token => new { token.UserId, token.RevokedAtUtc });

            modelBuilder.Entity<RefreshToken>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppUser>()
                .Property(user => user.CreatedOn)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Order>()
                .Property(order => order.OrderStatus)
                .HasColumnName("order_status")
                .HasMaxLength(32)
                .HasDefaultValue(OrderStatuses.Pending);

            modelBuilder.Entity<Order>()
                .Property(order => order.PaymentStatus)
                .HasColumnName("payment_status")
                .HasMaxLength(32)
                .HasDefaultValue(PaymentStatuses.Pending);

            modelBuilder.Entity<Order>()
                .Property(order => order.PaymentMethodKey)
                .HasColumnName("payment_method_key")
                .HasMaxLength(64)
                .HasDefaultValue(PaymentMethodKeys.Cod);

            modelBuilder.Entity<Order>()
                .Property(order => order.PaymentAt)
                .HasColumnName("payment_at")
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Order>()
                .Property(order => order.PaymentMetadataJson)
                .HasColumnName("payment_metadata_json")
                .HasColumnType("jsonb");

            modelBuilder.Entity<Order>()
                .Property(order => order.CustomerName)
                .HasColumnName("customer_name")
                .HasMaxLength(256);

            modelBuilder.Entity<Order>()
                .Property(order => order.CustomerEmail)
                .HasColumnName("customer_email")
                .HasMaxLength(256);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingFullName)
                .HasColumnName("shipping_full_name")
                .HasMaxLength(256)
                .HasDefaultValue(string.Empty);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingEmail)
                .HasColumnName("shipping_email")
                .HasMaxLength(256)
                .HasDefaultValue(string.Empty);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingPhone)
                .HasColumnName("shipping_phone")
                .HasMaxLength(64);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingAddress1)
                .HasColumnName("shipping_address1")
                .HasMaxLength(400)
                .HasDefaultValue(string.Empty);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingAddress2)
                .HasColumnName("shipping_address2")
                .HasMaxLength(400);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingCity)
                .HasColumnName("shipping_city")
                .HasMaxLength(160)
                .HasDefaultValue(string.Empty);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingState)
                .HasColumnName("shipping_state")
                .HasMaxLength(160);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingPostalCode)
                .HasColumnName("shipping_postal_code")
                .HasMaxLength(64)
                .HasDefaultValue(string.Empty);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingCountryCode)
                .HasColumnName("shipping_country_code")
                .HasMaxLength(2)
                .HasDefaultValue(string.Empty);

            modelBuilder.Entity<Order>()
                .Property(order => order.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Order>()
                .Property(order => order.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Order>()
                .Property(order => order.CancelledAt)
                .HasColumnName("cancelled_at")
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Order>()
                .Property(order => order.AdminNote)
                .HasMaxLength(2000);

            modelBuilder.Entity<Order>()
                .Property(order => order.CurrencyCode)
                .HasMaxLength(3);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseCurrencyCode)
                .HasMaxLength(3);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseTotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.ExchangeRate)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Order>()
                .Property(order => order.ExchangeRateProviderKey)
                .HasMaxLength(64);

            modelBuilder.Entity<Order>()
                .Property(order => order.ExchangeRateSource)
                .HasMaxLength(256);

            modelBuilder.Entity<Order>()
                .Property(order => order.ExchangeRateEffectiveAtUtc)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Order>()
                .Property(order => order.ExchangeRateExpiresAtUtc)
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.StoreId, order.OrderStatus, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.StoreId, order.PaymentStatus, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.StoreId, order.CustomerEmail, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.PaymentMethodKey);

            modelBuilder.Entity<Order>()
                .ToTable(
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

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.Sku)
                .HasMaxLength(64);

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.VariantAttributesJson)
                .HasColumnType("jsonb");

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.PersonalizationHash)
                .HasMaxLength(128);

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.PersonalizationJson)
                .HasColumnType("jsonb");

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.FulfillmentProviderKey)
                .HasMaxLength(64);

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.CurrencyCode)
                .HasMaxLength(3);

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.BaseUnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.ConvertedUnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.LineTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderLine>()
                .Property(line => line.BaseLineTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderLine>()
                .HasIndex(line => line.ProductVariantId);

            modelBuilder.Entity<OrderLine>()
                .HasIndex(line => line.ArtworkAssetId);

            modelBuilder.Entity<OrderLine>()
                .HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(line => line.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Shipment>(entity =>
            {
                entity.ToTable("Shipments");
                entity.HasKey(shipment => shipment.Id);

                entity.Property(shipment => shipment.ShipDate)
                    .HasColumnType("timestamp with time zone");

                entity.Property(shipment => shipment.CarrierName)
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(shipment => shipment.CarrierService)
                    .HasMaxLength(128);

                entity.Property(shipment => shipment.TrackingNumber)
                    .HasMaxLength(160)
                    .IsRequired();

                entity.Property(shipment => shipment.TrackingUrl)
                    .HasMaxLength(1024);

                entity.Property(shipment => shipment.Note)
                    .HasMaxLength(1000);

                entity.Property(shipment => shipment.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(shipment => shipment.UpdatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(shipment => new { shipment.StoreId, shipment.OrderId })
                    .IsUnique();

                entity.HasIndex(shipment => shipment.StoreId);

                entity.HasIndex(shipment => shipment.OrderId);

                entity.HasOne(shipment => shipment.Order)
                    .WithMany()
                    .HasForeignKey(shipment => shipment.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PaymentMethod>(entity =>
            {
                entity.Property(method => method.Key)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(method => method.Name)
                    .HasMaxLength(160)
                    .IsRequired();

                entity.Property(method => method.Description)
                    .HasMaxLength(500);

                entity.HasIndex(method => method.Key).IsUnique();

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_payment_methods_key",
                        "\"Key\" in ('cod', 'stripe', 'paypal')"));
            });

            modelBuilder.Entity<StorePaymentMethod>(entity =>
            {
                entity.ToTable("store_payment_methods");
                entity.HasKey(method => method.Id);
                entity.Property(method => method.Id).HasColumnName("id");
                entity.Property(method => method.StoreId).HasColumnName("store_id");
                entity.Property(method => method.PaymentMethodKey).HasColumnName("payment_method_key").HasMaxLength(64).IsRequired();
                entity.Property(method => method.Enabled).HasColumnName("enabled");
                entity.Property(method => method.DisplayName).HasColumnName("display_name").HasMaxLength(160).IsRequired();
                entity.Property(method => method.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(method => method.ShortDisplayText).HasColumnName("short_display_text").HasMaxLength(160);
                entity.Property(method => method.IconUrl).HasColumnName("icon_url").HasMaxLength(1024);
                entity.Property(method => method.DisplayOrder).HasColumnName("display_order");
                entity.Property(method => method.SupportedCurrencyCodesJson).HasColumnName("supported_currency_codes_json").HasColumnType("jsonb");
                entity.Property(method => method.SupportedCountryCodesJson).HasColumnName("supported_country_codes_json").HasColumnType("jsonb");
                entity.Property(method => method.MinOrderTotal).HasColumnName("min_order_total").HasPrecision(18, 2);
                entity.Property(method => method.MaxOrderTotal).HasColumnName("max_order_total").HasPrecision(18, 2);
                entity.Property(method => method.SettingsJson).HasColumnName("settings_json").HasColumnType("jsonb");
                entity.Property(method => method.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(method => method.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(method => new { method.StoreId, method.PaymentMethodKey }).IsUnique();
                entity.HasIndex(method => new { method.StoreId, method.Enabled, method.DisplayOrder });

                entity.HasOne(method => method.Store)
                    .WithMany()
                    .HasForeignKey(method => method.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    "store_payment_methods",
                    table => table.HasCheckConstraint(
                        "ck_store_payment_methods_key",
                        "payment_method_key in ('cod', 'stripe', 'paypal')"));
            });

            modelBuilder.Entity<StoreFeatureState>(entity =>
            {
                entity.ToTable("store_feature_states");
                entity.HasKey(feature => feature.Id);
                entity.Property(feature => feature.Id).HasColumnName("id");
                entity.Property(feature => feature.StoreId).HasColumnName("store_id");
                entity.Property(feature => feature.FeatureKey).HasColumnName("feature_key").HasMaxLength(64).IsRequired();
                entity.Property(feature => feature.Enabled).HasColumnName("enabled");
                entity.Property(feature => feature.Reason).HasColumnName("reason").HasMaxLength(500);
                entity.Property(feature => feature.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(feature => feature.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(feature => new { feature.StoreId, feature.FeatureKey }).IsUnique();
                entity.HasIndex(feature => new { feature.StoreId, feature.Enabled });

                entity.HasOne(feature => feature.Store)
                    .WithMany()
                    .HasForeignKey(feature => feature.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    "store_feature_states",
                    table => table.HasCheckConstraint(
                        "ck_store_feature_states_feature_key",
                        "feature_key in ('checkout', 'customerAccounts', 'newsletter', 'recommendations', 'reviews')"));
            });

            modelBuilder.Entity<StoreCurrency>(entity =>
            {
                entity.ToTable("store_currencies");
                entity.HasKey(currency => currency.Id);
                entity.Property(currency => currency.Id).HasColumnName("id");
                entity.Property(currency => currency.StoreId).HasColumnName("store_id");
                entity.Property(currency => currency.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
                entity.Property(currency => currency.IsEnabled).HasColumnName("is_enabled");
                entity.Property(currency => currency.IsDefaultDisplayCurrency).HasColumnName("is_default_display_currency");
                entity.Property(currency => currency.DisplayOrder).HasColumnName("display_order");
                entity.Property(currency => currency.CultureName).HasColumnName("culture_name").HasMaxLength(32);
                entity.Property(currency => currency.Symbol).HasColumnName("symbol").HasMaxLength(16);
                entity.Property(currency => currency.DecimalDigits).HasColumnName("decimal_digits");
                entity.Property(currency => currency.UnitPriceRoundingMode).HasColumnName("unit_price_rounding_mode").HasMaxLength(32).IsRequired();
                entity.Property(currency => currency.UnitPriceRoundingIncrement).HasColumnName("unit_price_rounding_increment").HasPrecision(18, 4);
                entity.Property(currency => currency.LineTotalRoundingMode).HasColumnName("line_total_rounding_mode").HasMaxLength(32).IsRequired();
                entity.Property(currency => currency.LineTotalRoundingIncrement).HasColumnName("line_total_rounding_increment").HasPrecision(18, 4);
                entity.Property(currency => currency.OrderTotalRoundingMode).HasColumnName("order_total_rounding_mode").HasMaxLength(32).IsRequired();
                entity.Property(currency => currency.OrderTotalRoundingIncrement).HasColumnName("order_total_rounding_increment").HasPrecision(18, 4);
                entity.Property(currency => currency.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(currency => currency.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(currency => new { currency.StoreId, currency.CurrencyCode }).IsUnique();
                entity.HasIndex(currency => new { currency.StoreId, currency.IsEnabled, currency.DisplayOrder });

                entity.HasOne(currency => currency.Store)
                    .WithMany()
                    .HasForeignKey(currency => currency.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    "store_currencies",
                    table =>
                    {
                        table.HasCheckConstraint("ck_store_currencies_currency_code", "char_length(currency_code) = 3");
                        table.HasCheckConstraint("ck_store_currencies_decimal_digits", "decimal_digits >= 0 and decimal_digits <= 4");
                        table.HasCheckConstraint("ck_store_currencies_unit_price_rounding_increment", "unit_price_rounding_increment > 0");
                        table.HasCheckConstraint("ck_store_currencies_line_total_rounding_increment", "line_total_rounding_increment > 0");
                        table.HasCheckConstraint("ck_store_currencies_order_total_rounding_increment", "order_total_rounding_increment > 0");
                    });
            });

            modelBuilder.Entity<StoreCurrencyExchangeRate>(entity =>
            {
                entity.ToTable("store_currency_exchange_rates");
                entity.HasKey(rate => rate.Id);
                entity.Property(rate => rate.Id).HasColumnName("id");
                entity.Property(rate => rate.StoreId).HasColumnName("store_id");
                entity.Property(rate => rate.BaseCurrencyCode).HasColumnName("base_currency_code").HasMaxLength(3).IsRequired();
                entity.Property(rate => rate.TargetCurrencyCode).HasColumnName("target_currency_code").HasMaxLength(3).IsRequired();
                entity.Property(rate => rate.Rate).HasColumnName("rate").HasPrecision(28, 12);
                entity.Property(rate => rate.ProviderKey).HasColumnName("provider_key").HasMaxLength(64).IsRequired();
                entity.Property(rate => rate.Source).HasColumnName("source").HasMaxLength(256);
                entity.Property(rate => rate.EffectiveAt).HasColumnName("effective_at").HasColumnType("timestamp with time zone");
                entity.Property(rate => rate.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
                entity.Property(rate => rate.IsManual).HasColumnName("is_manual");
                entity.Property(rate => rate.IsEnabled).HasColumnName("is_enabled");
                entity.Property(rate => rate.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(rate => rate.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(rate => new { rate.StoreId, rate.BaseCurrencyCode, rate.TargetCurrencyCode, rate.ProviderKey }).IsUnique();
                entity.HasIndex(rate => new { rate.StoreId, rate.TargetCurrencyCode, rate.IsEnabled });

                entity.HasOne(rate => rate.Store)
                    .WithMany()
                    .HasForeignKey(rate => rate.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    "store_currency_exchange_rates",
                    table =>
                    {
                        table.HasCheckConstraint("ck_store_currency_exchange_rates_base_currency_code", "char_length(base_currency_code) = 3");
                        table.HasCheckConstraint("ck_store_currency_exchange_rates_target_currency_code", "char_length(target_currency_code) = 3");
                        table.HasCheckConstraint("ck_store_currency_exchange_rates_distinct_currency", "base_currency_code <> target_currency_code");
                        table.HasCheckConstraint("ck_store_currency_exchange_rates_rate", "rate > 0");
                        table.HasCheckConstraint("ck_store_currency_exchange_rates_expires_after_effective", "expires_at is null or expires_at > effective_at");
                    });
            });

            modelBuilder.Entity<PaymentMethod>().HasData(
                new PaymentMethod
                {
                    Id = Guid.Parse("3604fc1d-cd6a-46ad-ace4-9b5f8e03f43b"),
                    Key = PaymentMethodKeys.Stripe,
                    Name = "Stripe",
                    Description = "Card payments through Stripe.",
                    IsEnabledByDefault = false,
                    SortOrder = 20,
                },
                new PaymentMethod
                {
                    Id = Guid.Parse("6f2c2a7e-9f9b-4a0d-9f7f-2a1b3c4d5e6f"),
                    Key = PaymentMethodKeys.Cod,
                    Name = "Cash on Delivery",
                    Description = "Test checkout payment method for MVP.",
                    IsEnabledByDefault = true,
                    SortOrder = 10,
                },
                new PaymentMethod
                {
                    Id = Guid.Parse("b2e5c1d4-7a9f-4d2c-8f1e-3a4b5c6d7e8f"),
                    Key = PaymentMethodKeys.PayPal,
                    Name = "PayPal",
                    Description = "PayPal payment skeleton.",
                    IsEnabledByDefault = false,
                    SortOrder = 30,
                });

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "93f5cdac-43de-4895-8426-2048c228e76d",
                    ConcurrencyStamp = "02d86d56-8e63-4d2e-92f8-81b154ba0532",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                },
                new IdentityRole
                {
                    Id = "b7af6842-02fa-4af4-8f61-ae04a49644a2",
                    ConcurrencyStamp = "75e8afa8-8df5-4431-a220-ac56b1fd0cda",
                    Name = "User",
                    NormalizedName = "USER",
                });

            modelBuilder.Entity<NewsletterSubscriber>()
                .HasIndex(subscriber => new { subscriber.StoreId, subscriber.Email })
                .IsUnique()
                .HasFilter("\"StoreId\" IS NOT NULL");

            modelBuilder.Entity<NewsletterSubscriber>()
                .HasIndex(subscriber => subscriber.StoreId);

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.Reference)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.StoreId, order.UserId, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.CreatedOn);

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.StoreId);

            modelBuilder.Entity<OrderItem>()
                .HasIndex(orderItem => new { orderItem.StoreId, orderItem.UserId, orderItem.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasMany(order => order.Lines)
                .WithOne(line => line.Order!)
                .HasForeignKey(line => line.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommerceTask>(entity =>
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

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_commerce_task_status",
                        "status in ('pending', 'running', 'waiting_retry', 'succeeded', 'failed', 'cancelled', 'dead')"));
                entity.ToTable(
                    table => table.HasCheckConstraint("ck_commerce_task_attempt_count", "attempt_count >= 0"));
                entity.ToTable(
                    table => table.HasCheckConstraint("ck_commerce_task_max_attempts", "max_attempts >= 1"));
            });

            modelBuilder.Entity<CommerceTaskStep>(entity =>
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

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_commerce_task_step_status",
                        "status in ('pending', 'running', 'succeeded', 'failed', 'skipped', 'rolled_back')"));
            });

            modelBuilder.Entity<StoreDeployment>(entity =>
            {
                entity.ToTable("store_deployment");
                entity.HasKey(deployment => deployment.Id);
                entity.Property(deployment => deployment.Id).HasColumnName("id");
                entity.Property(deployment => deployment.StoreId).HasColumnName("store_id");
                entity.Property(deployment => deployment.TaskId).HasColumnName("task_id");
                entity.Property(deployment => deployment.StorefrontImage).HasColumnName("storefront_image").IsRequired();
                entity.Property(deployment => deployment.ContainerName).HasColumnName("container_name").IsRequired();
                entity.Property(deployment => deployment.NetworkName).HasColumnName("network_name");
                entity.Property(deployment => deployment.PublicUrl).HasColumnName("public_url");
                entity.Property(deployment => deployment.InternalUrl).HasColumnName("internal_url");
                entity.Property(deployment => deployment.NginxServerName).HasColumnName("nginx_server_name");
                entity.Property(deployment => deployment.NginxConfigPath).HasColumnName("nginx_config_path");
                entity.Property(deployment => deployment.EnvFilePath).HasColumnName("env_file_path");
                entity.Property(deployment => deployment.Status).HasColumnName("status").IsRequired();
                entity.Property(deployment => deployment.LastHealthStatus).HasColumnName("last_health_status");
                entity.Property(deployment => deployment.LastHealthAt).HasColumnName("last_health_at").HasColumnType("timestamp with time zone");
                entity.Property(deployment => deployment.DeployedAt).HasColumnName("deployed_at").HasColumnType("timestamp with time zone");
                entity.Property(deployment => deployment.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(deployment => deployment.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(deployment => deployment.Task)
                    .WithMany()
                    .HasForeignKey(deployment => deployment.TaskId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(deployment => deployment.Store)
                    .WithOne()
                    .HasForeignKey<StoreDeployment>(deployment => deployment.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(deployment => deployment.StoreId).IsUnique();
                entity.HasIndex(deployment => deployment.ContainerName).IsUnique();
                entity.HasIndex(deployment => deployment.Status);

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_store_deployment_status",
                        "status in ('provisioning', 'active', 'failed', 'disabled', 'removed')"));
            });

            modelBuilder.Entity<StorefrontDeploymentImage>(entity =>
            {
                entity.ToTable("storefront_deployment_image");
                entity.HasKey(image => image.Id);
                entity.Property(image => image.Id).HasColumnName("id");
                entity.Property(image => image.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
                entity.Property(image => image.Image).HasColumnName("image").HasMaxLength(500).IsRequired();
                entity.Property(image => image.Version).HasColumnName("version").HasMaxLength(100);
                entity.Property(image => image.IsDefault).HasColumnName("is_default");
                entity.Property(image => image.IsEnabled).HasColumnName("is_enabled");
                entity.Property(image => image.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(image => image.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(image => image.Key).IsUnique();
                entity.HasIndex(image => image.Image).IsUnique();
                entity.HasIndex(image => new { image.IsEnabled, image.IsDefault });
                entity.HasIndex(image => image.IsDefault)
                    .IsUnique()
                    .HasFilter("is_enabled = true AND is_default = true");

                entity.HasData(
                    new StorefrontDeploymentImage
                    {
                        Id = Guid.Parse("0aa383ff-dc89-4a30-bc13-6c4cae7b72b6"),
                        Key = "storefront-v2",
                        Image = "blazorshop-storefront-v2:latest",
                        Version = "latest",
                        IsDefault = true,
                        IsEnabled = true,
                        CreatedAt = new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero),
                        UpdatedAt = new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero),
                    });
            });

            modelBuilder.Entity<CommerceStore>(entity =>
            {
                entity.ToTable("commerce_store");
                entity.HasKey(store => store.Id);
                entity.Property(store => store.Id).HasColumnName("id");
                entity.Property(store => store.PublicId).HasColumnName("public_id");
                entity.Property(store => store.ControlPlaneStorePublicId).HasColumnName("control_plane_store_public_id");
                entity.Property(store => store.StoreKey).HasColumnName("store_key").IsRequired();
                entity.Property(store => store.Name).HasColumnName("name").HasMaxLength(400).IsRequired();
                entity.Property(store => store.Status).HasColumnName("status").IsRequired();
                entity.Property(store => store.BaseUrl).HasColumnName("base_url");
                entity.Property(store => store.ForceHttps).HasColumnName("force_https").HasDefaultValue(true);
                entity.Property(store => store.SslEnabled).HasColumnName("ssl_enabled").HasDefaultValue(true);
                entity.Property(store => store.SslPort).HasColumnName("ssl_port");
                entity.Property(store => store.DisplayOrder).HasColumnName("display_order");
                entity.Property(store => store.HtmlBodyId).HasColumnName("html_body_id").HasMaxLength(128);
                entity.Property(store => store.CdnHost).HasColumnName("cdn_host");
                entity.Property(store => store.LogoUrl).HasColumnName("logo_url");
                entity.Property(store => store.CompanyName).HasColumnName("company_name").HasMaxLength(200);
                entity.Property(store => store.CompanyEmail).HasColumnName("company_email").HasMaxLength(254);
                entity.Property(store => store.CompanyPhone).HasColumnName("company_phone").HasMaxLength(50);
                entity.Property(store => store.CompanyAddress).HasColumnName("company_address").HasMaxLength(500);
                entity.Property(store => store.FaviconUrl).HasColumnName("favicon_url");
                entity.Property(store => store.PngIconUrl).HasColumnName("png_icon_url");
                entity.Property(store => store.AppleTouchIconUrl).HasColumnName("apple_touch_icon_url");
                entity.Property(store => store.MsTileImageUrl).HasColumnName("ms_tile_image_url");
                entity.Property(store => store.MsTileColor).HasColumnName("ms_tile_color").HasMaxLength(32);
                entity.Property(store => store.DefaultCurrencyCode).HasColumnName("default_currency_code").HasMaxLength(3).IsRequired();
                entity.Property(store => store.DefaultCulture).HasColumnName("default_culture").HasMaxLength(20).IsRequired();
                entity.Property(store => store.SupportEmail).HasColumnName("support_email").HasMaxLength(256);
                entity.Property(store => store.SupportPhone).HasColumnName("support_phone").HasMaxLength(64);
                entity.Property(store => store.MaintenanceModeEnabled).HasColumnName("maintenance_mode_enabled");
                entity.Property(store => store.MaintenanceMessage).HasColumnName("maintenance_message");
                entity.Property(store => store.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
                entity.Property(store => store.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(store => store.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(store => store.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(store => store.PublicId).IsUnique();
                entity.HasIndex(store => store.ControlPlaneStorePublicId);
                entity.HasIndex(store => store.Status);
                entity.HasIndex(store => store.DisplayOrder);
                entity.HasIndex(store => store.StoreKey)
                    .IsUnique()
                    .HasFilter("archived_at IS NULL");

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_commerce_store_status",
                        "status in ('active', 'provisioning', 'disabled', 'archived')"));
                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_commerce_store_default_currency_code",
                        "char_length(default_currency_code) = 3"));
            });

            modelBuilder.Entity<CommerceStoreDomain>(entity =>
            {
                entity.ToTable("commerce_store_domain");
                entity.HasKey(domain => domain.Id);
                entity.Property(domain => domain.Id).HasColumnName("id");
                entity.Property(domain => domain.StoreId).HasColumnName("store_id");
                entity.Property(domain => domain.Domain).HasColumnName("domain").IsRequired();
                entity.Property(domain => domain.NormalizedDomain).HasColumnName("normalized_domain").IsRequired();
                entity.Property(domain => domain.IsPrimary).HasColumnName("is_primary");
                entity.Property(domain => domain.Status).HasColumnName("status").IsRequired();
                entity.Property(domain => domain.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(domain => domain.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(domain => domain.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamp with time zone");
                entity.Property(domain => domain.DisabledAt).HasColumnName("disabled_at").HasColumnType("timestamp with time zone");

                entity.HasOne(domain => domain.Store)
                    .WithMany(store => store.Domains)
                    .HasForeignKey(domain => domain.StoreId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(domain => domain.StoreId);
                entity.HasIndex(domain => domain.NormalizedDomain)
                    .IsUnique()
                    .HasFilter("disabled_at IS NULL");
                entity.HasIndex(domain => new { domain.StoreId, domain.IsPrimary })
                    .IsUnique()
                    .HasFilter("is_primary = true AND disabled_at IS NULL");

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_commerce_store_domain_status",
                        "status in ('pending', 'verified', 'disabled')"));
            });
        }

        private static string SqlIn(IEnumerable<string> values)
        {
            return string.Join(", ", values.Select(value => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'"));
        }
    }
}
