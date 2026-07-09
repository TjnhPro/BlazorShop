namespace BlazorShop.Infrastructure.Data.CommerceNode
{
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

        public DbSet<OrderItem> CheckoutOrderItems => Set<OrderItem>();

        public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();

        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderLine> OrderLines => Set<OrderLine>();

        public DbSet<SeoRedirect> SeoRedirects => Set<SeoRedirect>();

        public DbSet<SeoSettings> SeoSettings => Set<SeoSettings>();

        public DbSet<CommerceTask> CommerceTasks => Set<CommerceTask>();

        public DbSet<CommerceTaskStep> CommerceTaskSteps => Set<CommerceTaskStep>();

        public DbSet<StoreDeployment> StoreDeployments => Set<StoreDeployment>();

        public DbSet<CommerceStore> CommerceStores => Set<CommerceStore>();

        public DbSet<CommerceStoreDomain> CommerceStoreDomains => Set<CommerceStoreDomain>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new SeoRedirectConfiguration());
            modelBuilder.ApplyConfiguration(new SeoSettingsConfiguration());
            modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new AdminSettingsConfiguration());

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(variant => new { variant.ProductId, variant.SizeScale, variant.SizeValue })
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .HasOne(variant => variant.Product)
                .WithMany(product => product.Variants)
                .HasForeignKey(variant => variant.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .Property(order => order.AdminNote)
                .HasMaxLength(2000);

            modelBuilder.Entity<PaymentMethod>().HasData(
                new PaymentMethod
                {
                    Id = Guid.Parse("3604fc1d-cd6a-46ad-ace4-9b5f8e03f43b"),
                    Name = "Credit Card",
                },
                new PaymentMethod
                {
                    Id = Guid.Parse("6f2c2a7e-9f9b-4a0d-9f7f-2a1b3c4d5e6f"),
                    Name = "Cash on Delivery",
                },
                new PaymentMethod
                {
                    Id = Guid.Parse("b2e5c1d4-7a9f-4d2c-8f1e-3a4b5c6d7e8f"),
                    Name = "Bank Transfer",
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
                .HasIndex(subscriber => subscriber.Email)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.Reference)
                .IsUnique();

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.UserId, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.CreatedOn);

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
                entity.Property(store => store.DefaultCurrencyCode).HasColumnName("default_currency_code").HasMaxLength(3).IsRequired();
                entity.Property(store => store.DefaultCulture).HasColumnName("default_culture").HasMaxLength(20).IsRequired();
                entity.Property(store => store.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(store => store.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(store => store.ArchivedAt).HasColumnName("archived_at").HasColumnType("timestamp with time zone");

                entity.HasIndex(store => store.PublicId).IsUnique();
                entity.HasIndex(store => store.ControlPlaneStorePublicId);
                entity.HasIndex(store => store.Status);
                entity.HasIndex(store => store.StoreKey)
                    .IsUnique()
                    .HasFilter("archived_at IS NULL");

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_commerce_store_status",
                        "status in ('active', 'disabled', 'archived')"));
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
    }
}
