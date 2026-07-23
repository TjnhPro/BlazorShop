namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed partial class CommerceNodeDevelopmentSeeder
    {
        private async Task<bool> HasRequiredQaSeedDataAsync(CancellationToken cancellationToken)
        {
            var defaultStoreExists = await this.dbContext.CommerceStores
                .AnyAsync(store => store.StoreKey == DefaultStoreKey, cancellationToken);
            if (!defaultStoreExists)
            {
                return false;
            }

            var coreCatalogExists = await this.dbContext.Categories
                .AnyAsync(category => category.Id == ApparelCategoryId, cancellationToken)
                && await this.dbContext.Products
                    .AnyAsync(product => product.Id == SimpleProductId, cancellationToken);

            return coreCatalogExists;
        }

        private async Task EnsureIncrementalQaSeedDataAsync(CancellationToken cancellationToken)
        {
            var defaultStoreId = await this.dbContext.CommerceStores
                .Where(store => store.StoreKey == DefaultStoreKey)
                .Select(store => (Guid?)store.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!defaultStoreId.HasValue)
            {
                return;
            }

            await this.EnsureCategoriesAsync(defaultStoreId.Value, cancellationToken);
            await this.EnsureProductsAsync(defaultStoreId.Value, cancellationToken);
            await this.EnsureStorefrontPagesAsync(defaultStoreId.Value, cancellationToken);
            await this.EnsureMediaFixtureFilesAsync(cancellationToken);

            var defaultGalleryCount = await this.dbContext.ProductMedia
                .CountAsync(media => media.StoreId == defaultStoreId.Value
                    && media.ProductId == SeoMediaProductId
                    && media.Status == ProductMediaStatuses.Stored
                    && media.DeletedAt == null,
                    cancellationToken);

            if (defaultGalleryCount >= 3)
            {
                return;
            }

            await this.EnsureProductMediaFixturesAsync(defaultStoreId.Value, cancellationToken);
        }

        private async Task<CommerceStore> EnsureStoreAsync(CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var store = await this.dbContext.CommerceStores
                .Include(candidate => candidate.Domains)
                .FirstOrDefaultAsync(candidate => candidate.StoreKey == DefaultStoreKey, cancellationToken);

            if (store is not null)
            {
                var primaryDomain = store.Domains.FirstOrDefault(domain => domain.IsPrimary && domain.DisabledAt == null);
                if (primaryDomain is null)
                {
                    store.Domains.Add(new CommerceStoreDomain
                    {
                        Domain = "localhost",
                        NormalizedDomain = "localhost",
                        IsPrimary = true,
                        Status = CommerceStoreDomainStatuses.Verified,
                        CreatedAt = now,
                        UpdatedAt = now,
                        VerifiedAt = now,
                    });

                    store.UpdatedAt = now;
                    await this.dbContext.SaveChangesAsync(cancellationToken);
                }

                return store;
            }

            store = new CommerceStore
            {
                StoreKey = DefaultStoreKey,
                Name = "Default QA Store",
                Status = CommerceStoreStatuses.Active,
                BaseUrl = "http://localhost:18598",
                ForceHttps = false,
                SslEnabled = false,
                LogoUrl = "/images/banner-bg.jpg",
                FaviconUrl = "/favicon.ico",
                PngIconUrl = "/icon-192.png",
                DefaultCurrencyCode = "EUR",
                DefaultCulture = "en-US",
                SupportEmail = "support@example.local",
                SupportPhone = "+1 555 0100",
                CompanyName = "Default QA Store Ltd",
                CompanyEmail = "support@example.local",
                CompanyPhone = "+1 555 0100",
                CompanyAddress = "1 QA Street, QA City, US",
                CreatedAt = now,
                UpdatedAt = now,
            };

            store.Domains.Add(new CommerceStoreDomain
            {
                Domain = "localhost",
                NormalizedDomain = "localhost",
                IsPrimary = true,
                Status = "verified",
                CreatedAt = now,
                UpdatedAt = now,
                VerifiedAt = now,
            });

            this.dbContext.CommerceStores.Add(store);
            await this.dbContext.SaveChangesAsync(cancellationToken);
            return store;
        }

        private async Task<CommerceStore> EnsureAuxiliaryStoresAsync(CancellationToken cancellationToken)
        {
            var isolationStore = await this.EnsureAuxiliaryStoreAsync(
                QaS2StoreId,
                IsolationStoreKey,
                "QA Isolation Store",
                CommerceStoreStatuses.Active,
                "http://s2.localhost:18598",
                "s2.localhost",
                maintenanceModeEnabled: false,
                maintenanceMessage: null,
                cancellationToken);

            await this.EnsureAuxiliaryStoreAsync(
                QaMaintenanceStoreId,
                MaintenanceStoreKey,
                "QA Maintenance Store",
                CommerceStoreStatuses.Active,
                "http://maintenance.localhost:18598",
                "maintenance.localhost",
                maintenanceModeEnabled: true,
                maintenanceMessage: "QA maintenance window is active.",
                cancellationToken);

            await this.EnsureAuxiliaryStoreAsync(
                QaDisabledStoreId,
                DisabledStoreKey,
                "QA Disabled Store",
                CommerceStoreStatuses.Disabled,
                "http://disabled.localhost:18598",
                "disabled.localhost",
                maintenanceModeEnabled: false,
                maintenanceMessage: null,
                cancellationToken);

            return isolationStore;
        }

        private async Task<CommerceStore> EnsureAuxiliaryStoreAsync(
            Guid storeId,
            string storeKey,
            string name,
            string status,
            string baseUrl,
            string domainName,
            bool maintenanceModeEnabled,
            string? maintenanceMessage,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var store = await this.dbContext.CommerceStores
                .Include(candidate => candidate.Domains)
                .FirstOrDefaultAsync(candidate => candidate.StoreKey == storeKey, cancellationToken);

            if (store is null)
            {
                store = new CommerceStore
                {
                    Id = storeId,
                    StoreKey = storeKey,
                    Name = name,
                    Status = status,
                    BaseUrl = baseUrl,
                    ForceHttps = false,
                    SslEnabled = false,
                    DefaultCurrencyCode = "EUR",
                    DefaultCulture = "en-US",
                    SupportEmail = "support@example.local",
                    LogoUrl = "/images/banner-bg.jpg",
                    FaviconUrl = "/favicon.ico",
                    PngIconUrl = "/icon-192.png",
                    CompanyName = $"{name} Ltd",
                    CompanyEmail = "support@example.local",
                    MaintenanceModeEnabled = maintenanceModeEnabled,
                    MaintenanceMessage = maintenanceMessage,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                this.dbContext.CommerceStores.Add(store);
            }

            if (store.Domains.All(domain => !string.Equals(domain.NormalizedDomain, domainName, StringComparison.OrdinalIgnoreCase)))
            {
                store.Domains.Add(new CommerceStoreDomain
                {
                    Domain = domainName,
                    NormalizedDomain = domainName,
                    IsPrimary = true,
                    Status = CommerceStoreDomainStatuses.Verified,
                    CreatedAt = now,
                    UpdatedAt = now,
                    VerifiedAt = now,
                });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
            return store;
        }

        private async Task EnsureStorePaymentMethodsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var existingKeys = await this.dbContext.StorePaymentMethods
                .Where(method => method.StoreId == storeId)
                .Select(method => method.PaymentMethodKey)
                .ToArrayAsync(cancellationToken);

            var existing = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
            var methods = new[]
            {
                new StorePaymentMethod
                {
                    StoreId = storeId,
                    PaymentMethodKey = PaymentMethodKeys.Cod,
                    Enabled = true,
                    DisplayName = "Cash on Delivery",
                    Description = "Test checkout payment method for MVP.",
                    DisplayOrder = 10,
                },
                new StorePaymentMethod
                {
                    StoreId = storeId,
                    PaymentMethodKey = PaymentMethodKeys.Stripe,
                    Enabled = false,
                    DisplayName = "Stripe",
                    Description = "Card payments through Stripe.",
                    DisplayOrder = 20,
                },
            };

            foreach (var method in methods)
            {
                if (!existing.Contains(method.PaymentMethodKey))
                {
                    this.dbContext.StorePaymentMethods.Add(method);
                }
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
