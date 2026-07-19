namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using BlazorShop.Domain.Constants;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode.Configurations;
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

        public DbSet<OrderHistoryEntry> OrderHistoryEntries => Set<OrderHistoryEntry>();

        public DbSet<Shipment> Shipments => Set<Shipment>();

        public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

        public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents => Set<ShipmentTrackingEvent>();

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

        public DbSet<CommerceCustomerAddress> CommerceCustomerAddresses => Set<CommerceCustomerAddress>();

        public DbSet<CartSession> CartSessions => Set<CartSession>();

        public DbSet<CartLine> CartLines => Set<CartLine>();

        public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();

        public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();

        public DbSet<PaymentProviderEvent> PaymentProviderEvents => Set<PaymentProviderEvent>();

        public DbSet<PaymentAttemptAuditLog> PaymentAttemptAuditLogs => Set<PaymentAttemptAuditLog>();

        public DbSet<CommerceStoreDomain> CommerceStoreDomains => Set<CommerceStoreDomain>();

        public DbSet<StorefrontConsentState> StorefrontConsentStates => Set<StorefrontConsentState>();

        public DbSet<StorefrontConsentEvent> StorefrontConsentEvents => Set<StorefrontConsentEvent>();

        public DbSet<StoreSecurityPrivacySettings> StoreSecurityPrivacySettings => Set<StoreSecurityPrivacySettings>();

        public DbSet<StoreShippingSettings> StoreShippingSettings => Set<StoreShippingSettings>();

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

        public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();

        public DbSet<QueuedMessage> QueuedMessages => Set<QueuedMessage>();

        public DbSet<StoreEmailSettings> StoreEmailSettings => Set<StoreEmailSettings>();

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
            modelBuilder.ApplyCommerceNodeConfigurations();

            modelBuilder.Entity<SeoRedirect>(entity =>
            {
                entity.HasOne<CommerceStore>()
                    .WithMany()
                    .HasForeignKey(redirect => redirect.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<OrderItem>()
                .HasIndex(orderItem => new { orderItem.StoreId, orderItem.UserId, orderItem.CreatedOn });

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

        }

        private static string SqlIn(IEnumerable<string> values)
        {
            return string.Join(", ", values.Select(value => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'"));
        }

    }
}
