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
                entity.Property(product => product.MinOrderQuantity).HasDefaultValue(1);
                entity.Property(product => product.QuantityStep).HasDefaultValue(1);
                entity.Property(product => product.PurchasingDisabled).HasDefaultValue(false);
                entity.Property(product => product.PurchasingDisabledReason)
                    .HasMaxLength(BlazorShop.Domain.Constants.ProductPurchaseConstraints.PurchasingDisabledReasonMaxLength);
                entity.Property(product => product.ManageStock).HasDefaultValue(true);
                entity.Property(product => product.HideWhenOutOfStock).HasDefaultValue(false);
                entity.Property(product => product.ShippingRequired).HasDefaultValue(true);
                entity.Property(product => product.FreeShipping).HasDefaultValue(false);
                entity.Property(product => product.ShippingSurcharge).HasPrecision(18, 2);
                entity.Property(product => product.DeliveryEstimateText)
                    .HasMaxLength(BlazorShop.Domain.Constants.ProductPurchaseConstraints.DeliveryEstimateTextMaxLength);
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
                .Property(variant => variant.IsActive)
                .HasDefaultValue(true);

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

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(order => order.CustomerId).HasColumnName("customer_id");
                entity.HasIndex(order => order.CustomerId);
                entity.HasOne(order => order.Customer)
                    .WithMany(customer => customer.Orders)
                    .HasForeignKey(order => order.CustomerId)
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
                entity.Property(option => option.ControlType)
                    .HasColumnName("control_type")
                    .HasMaxLength(32)
                    .HasDefaultValue(VariationControlTypes.Dropdown)
                    .IsRequired();
                entity.Property(option => option.IsRequired).HasColumnName("is_required").HasDefaultValue(true);
                entity.Property(option => option.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(option => option.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(option => option.PublicId).IsUnique();
                entity.HasIndex(option => new { option.TemplateId, option.Name }).IsUnique();
                entity.HasIndex(option => new { option.TemplateId, option.SortOrder });

                entity.HasOne(option => option.Template)
                    .WithMany(template => template.Options)
                    .HasForeignKey(option => option.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(
                    table => table.HasCheckConstraint(
                        "ck_variation_template_option_control_type",
                        $"control_type in ({SqlIn(VariationControlTypes.All)})"));
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
                entity.Property(value => value.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
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
                .Property(order => order.StorePublicId)
                .HasColumnName("store_public_id");

            modelBuilder.Entity<Order>()
                .Property(order => order.StoreKeySnapshot)
                .HasColumnName("store_key_snapshot")
                .HasMaxLength(128);

            modelBuilder.Entity<Order>()
                .Property(order => order.StoreNameSnapshot)
                .HasColumnName("store_name_snapshot")
                .HasMaxLength(400);

            modelBuilder.Entity<Order>()
                .Property(order => order.StoreBaseUrlSnapshot)
                .HasColumnName("store_base_url_snapshot")
                .HasMaxLength(2048);

            modelBuilder.Entity<Order>()
                .Property(order => order.StoreCompanyNameSnapshot)
                .HasColumnName("store_company_name_snapshot")
                .HasMaxLength(200);

            modelBuilder.Entity<Order>()
                .Property(order => order.StoreCompanyEmailSnapshot)
                .HasColumnName("store_company_email_snapshot")
                .HasMaxLength(254);

            modelBuilder.Entity<Order>()
                .Property(order => order.StoreCompanyPhoneSnapshot)
                .HasColumnName("store_company_phone_snapshot")
                .HasMaxLength(50);

            modelBuilder.Entity<Order>()
                .Property(order => order.StoreCompanyAddressSnapshot)
                .HasColumnName("store_company_address_snapshot")
                .HasMaxLength(500);

            modelBuilder.Entity<Order>()
                .Property(order => order.CustomerName)
                .HasColumnName("customer_name")
                .HasMaxLength(256);

            modelBuilder.Entity<Order>()
                .Property(order => order.CustomerEmail)
                .HasColumnName("customer_email")
                .HasMaxLength(256);

            modelBuilder.Entity<Order>()
                .Property(order => order.BillingAddressSnapshotJson)
                .HasColumnName("billing_address_snapshot_json")
                .HasColumnType("jsonb");

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingAddressSnapshotJson)
                .HasColumnName("shipping_address_snapshot_json")
                .HasColumnType("jsonb");

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
                .Property(order => order.ShippingMethodKey)
                .HasColumnName("shipping_method_key")
                .HasMaxLength(64);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingProviderSystemName)
                .HasColumnName("shipping_provider_system_name")
                .HasMaxLength(64);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingMethodCode)
                .HasColumnName("shipping_method_code")
                .HasMaxLength(64);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingMethodName)
                .HasColumnName("shipping_method_name")
                .HasMaxLength(128);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingTotal)
                .HasColumnName("shipping_total")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingCurrencyCode)
                .HasColumnName("shipping_currency_code")
                .HasMaxLength(3);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingDeliveryEstimateText)
                .HasColumnName("shipping_delivery_estimate_text")
                .HasMaxLength(128);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingMethodSnapshotJson)
                .HasColumnName("shipping_method_snapshot_json")
                .HasColumnType("jsonb");

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
                .Property(order => order.SubtotalAmount)
                .HasColumnName("subtotal_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.ShippingTotalAmount)
                .HasColumnName("shipping_total_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.TaxTotalAmount)
                .HasColumnName("tax_total_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.DiscountTotalAmount)
                .HasColumnName("discount_total_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.GrandTotalAmount)
                .HasColumnName("grand_total_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseCurrencyCode)
                .HasMaxLength(3);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseTotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseSubtotalAmount)
                .HasColumnName("base_subtotal_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseShippingTotalAmount)
                .HasColumnName("base_shipping_total_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseTaxTotalAmount)
                .HasColumnName("base_tax_total_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseDiscountTotalAmount)
                .HasColumnName("base_discount_total_amount")
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.BaseGrandTotalAmount)
                .HasColumnName("base_grand_total_amount")
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
                .Property(order => order.GuestAccessTokenHash)
                .HasColumnName("guest_access_token_hash")
                .HasMaxLength(64);

            modelBuilder.Entity<Order>()
                .Property(order => order.GuestAccessTokenExpiresAtUtc)
                .HasColumnName("guest_access_token_expires_at_utc")
                .HasColumnType("timestamp with time zone");

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.StoreId, order.OrderStatus, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.StoreId, order.PaymentStatus, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => new { order.StoreId, order.CustomerEmail, order.CreatedOn });

            modelBuilder.Entity<Order>()
                .HasIndex(order => order.GuestAccessTokenHash)
                .IsUnique()
                .HasFilter("guest_access_token_hash IS NOT NULL");

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

            modelBuilder.Entity<OrderHistoryEntry>(entity =>
            {
                entity.ToTable("order_history_entries");
                entity.HasKey(entry => entry.Id);
                entity.Property(entry => entry.Id).HasColumnName("id");
                entity.Property(entry => entry.StoreId).HasColumnName("store_id");
                entity.Property(entry => entry.OrderId).HasColumnName("order_id");
                entity.Property(entry => entry.EventType).HasColumnName("event_type").HasMaxLength(80).IsRequired();
                entity.Property(entry => entry.OldValue).HasColumnName("old_value").HasMaxLength(128);
                entity.Property(entry => entry.NewValue).HasColumnName("new_value").HasMaxLength(128);
                entity.Property(entry => entry.Message).HasColumnName("message").HasMaxLength(512).IsRequired();
                entity.Property(entry => entry.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
                entity.Property(entry => entry.VisibleToCustomer).HasColumnName("visible_to_customer").HasDefaultValue(false);
                entity.Property(entry => entry.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(entry => entry.Source).HasColumnName("source").HasMaxLength(64).HasDefaultValue("system").IsRequired();

                entity.HasOne(entry => entry.Order)
                    .WithMany()
                    .HasForeignKey(entry => entry.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(entry => new { entry.StoreId, entry.OrderId, entry.CreatedAtUtc });
                entity.HasIndex(entry => new { entry.StoreId, entry.EventType, entry.CreatedAtUtc });
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

                entity.HasMany(shipment => shipment.Items)
                    .WithOne(item => item.Shipment)
                    .HasForeignKey(item => item.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(shipment => shipment.TrackingEvents)
                    .WithOne(trackingEvent => trackingEvent.Shipment)
                    .HasForeignKey(trackingEvent => trackingEvent.ShipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShipmentItem>(entity =>
            {
                entity.ToTable("ShipmentItems");
                entity.HasKey(item => item.Id);

                entity.Property(item => item.Quantity)
                    .IsRequired();

                entity.Property(item => item.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(item => item.UpdatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(item => item.ShipmentId);
                entity.HasIndex(item => item.OrderLineId);
                entity.HasIndex(item => new { item.ShipmentId, item.OrderLineId })
                    .IsUnique();

                entity.HasOne(item => item.OrderLine)
                    .WithMany()
                    .HasForeignKey(item => item.OrderLineId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ShipmentTrackingEvent>(entity =>
            {
                entity.ToTable("ShipmentTrackingEvents");
                entity.HasKey(trackingEvent => trackingEvent.Id);

                entity.Property(trackingEvent => trackingEvent.Status)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(trackingEvent => trackingEvent.Message)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(trackingEvent => trackingEvent.Location)
                    .HasMaxLength(160);

                entity.Property(trackingEvent => trackingEvent.Source)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(trackingEvent => trackingEvent.OccurredAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(trackingEvent => trackingEvent.CreatedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(trackingEvent => new { trackingEvent.StoreId, trackingEvent.OrderId, trackingEvent.OccurredAtUtc });
                entity.HasIndex(trackingEvent => new { trackingEvent.ShipmentId, trackingEvent.OccurredAtUtc });
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

        }

        private static string SqlIn(IEnumerable<string> values)
        {
            return string.Join(", ", values.Select(value => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'"));
        }

    }
}
