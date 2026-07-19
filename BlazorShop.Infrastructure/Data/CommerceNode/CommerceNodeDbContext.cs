namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode.Configurations;
    using BlazorShop.Infrastructure.Data.Configurations;
    using BlazorShop.Infrastructure.Data.Configurations.Admin;

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
        }

    }
}
