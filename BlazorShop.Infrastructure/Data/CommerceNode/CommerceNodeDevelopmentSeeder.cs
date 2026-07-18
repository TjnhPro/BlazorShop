namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeDevelopmentSeeder
    {
        private const string DefaultStoreKey = "default";
        private const string IsolationStoreKey = "qa-s2";
        private const string MaintenanceStoreKey = "qa-maintenance";
        private const string DisabledStoreKey = "qa-disabled";
        private const string QaCustomerEmail = "qa.customer@example.local";
        private const string QaCustomerPassword = "QaCustomer123!";
        private const string QaSecondStoreCustomerEmail = "qa.s2.customer@example.local";
        private const string QaSecondStoreCustomerPassword = "QaS2Customer123!";
        private const string StorefrontQaUserRole = "User";

        private static readonly Guid ApparelCategoryId = Guid.Parse("8d4830f9-a21f-4f4a-96d7-83d1e6dc0201");
        private static readonly Guid TshirtsCategoryId = Guid.Parse("e0e5e4f8-3f12-4c17-b041-7a8fc62e6b14");
        private static readonly Guid LegalPageId = Guid.Parse("1a111111-1111-4111-8111-111111111111");
        private static readonly Guid CookiePageId = Guid.Parse("1a111111-1111-4111-8111-111111111112");
        private static readonly Guid DraftPageId = Guid.Parse("1a111111-1111-4111-8111-111111111113");
        private static readonly Guid LegalPagePublicId = Guid.Parse("1a111111-1111-4111-8111-111111111211");
        private static readonly Guid CookiePagePublicId = Guid.Parse("1a111111-1111-4111-8111-111111111212");
        private static readonly Guid DraftPagePublicId = Guid.Parse("1a111111-1111-4111-8111-111111111213");
        private static readonly Guid TshirtProductId = Guid.Parse("68ba3d10-4d13-46c4-8c8d-4a53b37cf201");
        private static readonly Guid LowStockProductId = Guid.Parse("e9f21b8f-7b2d-4a08-8971-c0dfe037fc1a");
        private static readonly Guid SimpleProductId = Guid.Parse("2b111111-1111-4111-8111-111111111101");
        private static readonly Guid OutOfStockProductId = Guid.Parse("2b111111-1111-4111-8111-111111111102");
        private static readonly Guid UnmanagedStockProductId = Guid.Parse("2b111111-1111-4111-8111-111111111103");
        private static readonly Guid QuantityRuleProductId = Guid.Parse("2b111111-1111-4111-8111-111111111104");
        private static readonly Guid UnpublishedProductId = Guid.Parse("2b111111-1111-4111-8111-111111111108");
        private static readonly Guid FutureProductId = Guid.Parse("2b111111-1111-4111-8111-111111111109");
        private static readonly Guid ExpiredProductId = Guid.Parse("2b111111-1111-4111-8111-111111111110");
        private static readonly Guid SurchargeProductId = Guid.Parse("2b111111-1111-4111-8111-111111111111");
        private static readonly Guid DigitalProductId = Guid.Parse("2b111111-1111-4111-8111-111111111112");
        private static readonly Guid SeoMediaProductId = Guid.Parse("2b111111-1111-4111-8111-111111111113");
        private static readonly Guid HtmlNameProductId = Guid.Parse("2b111111-1111-4111-8111-111111111115");
        private static readonly Guid QaCustomerId = Guid.Parse("3c111111-1111-4111-8111-111111111101");
        private static readonly Guid QaCustomerShippingAddressId = Guid.Parse("3c111111-1111-4111-8111-111111111201");
        private static readonly Guid QaCustomerBillingAddressId = Guid.Parse("3c111111-1111-4111-8111-111111111202");
        private static readonly Guid QaS2StoreId = Guid.Parse("4d111111-1111-4111-8111-111111111102");
        private static readonly Guid QaMaintenanceStoreId = Guid.Parse("4d111111-1111-4111-8111-111111111103");
        private static readonly Guid QaDisabledStoreId = Guid.Parse("4d111111-1111-4111-8111-111111111104");
        private static readonly Guid QaS2CategoryId = Guid.Parse("5e111111-1111-4111-8111-111111111102");
        private static readonly Guid QaS2ProductId = Guid.Parse("5e111111-1111-4111-8111-111111111202");
        private static readonly Guid QaS2CustomerId = Guid.Parse("5e111111-1111-4111-8111-111111111302");
        private static readonly Guid QaS2CustomerAddressId = Guid.Parse("5e111111-1111-4111-8111-111111111402");
        private static readonly Guid TshirtRedMVariantId = Guid.Parse("c34f5a0f-401d-4f58-b3d9-c9349ed6d101");
        private static readonly Guid TshirtRedXlVariantId = Guid.Parse("910cb350-8d44-43a7-b86d-8e38ea0cd102");
        private static readonly Guid TshirtBlackMVariantId = Guid.Parse("6894d9f0-071b-4f77-83a7-3d81d8a3d103");

        private readonly CommerceNodeDbContext dbContext;
        private readonly UserManager<AppUser> userManager;

        public CommerceNodeDevelopmentSeeder(
            CommerceNodeDbContext dbContext,
            UserManager<AppUser> userManager)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var store = await this.EnsureStoreAsync(cancellationToken);
            var isolationStore = await this.EnsureAuxiliaryStoresAsync(cancellationToken);
            await this.EnsureStorePaymentMethodsAsync(store.Id, cancellationToken);
            await this.EnsureStorePaymentMethodsAsync(isolationStore.Id, cancellationToken);
            await this.EnsureStoreConfigurationAsync(store.Id, cancellationToken);
            await this.EnsureStoreConfigurationAsync(isolationStore.Id, cancellationToken);
            await this.EnsureCategoriesAsync(store.Id, cancellationToken);
            await this.EnsureProductsAsync(store.Id, cancellationToken);
            await this.EnsureStorefrontPagesAsync(store.Id, cancellationToken);
            await this.EnsureNavigationAsync(store.Id, cancellationToken);
            var customer = await this.EnsureCustomerAsync(
                store.Id,
                QaCustomerId,
                QaCustomerShippingAddressId,
                QaCustomerBillingAddressId,
                QaCustomerEmail,
                QaCustomerPassword,
                "QA Customer",
                cancellationToken);
            await this.EnsureIsolationCatalogAsync(isolationStore.Id, cancellationToken);
            var isolationCustomer = await this.EnsureCustomerAsync(
                isolationStore.Id,
                QaS2CustomerId,
                QaS2CustomerAddressId,
                null,
                QaSecondStoreCustomerEmail,
                QaSecondStoreCustomerPassword,
                "QA S2 Customer",
                cancellationToken);
            await this.EnsureSampleOrderAsync(store, customer, cancellationToken);
            await this.EnsureSampleOrderAsync(isolationStore, isolationCustomer, cancellationToken);
        }

        private async Task<CommerceStore> EnsureStoreAsync(CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var store = await this.dbContext.CommerceStores
                .Include(candidate => candidate.Domains)
                .FirstOrDefaultAsync(candidate => candidate.StoreKey == DefaultStoreKey, cancellationToken);

            if (store is not null)
            {
                store.Name = "Default QA Store";
                store.Status = CommerceStoreStatuses.Active;
                store.BaseUrl = "http://localhost:18598";
                store.ForceHttps = false;
                store.SslEnabled = false;
                store.LogoUrl = "/images/banner-bg.jpg";
                store.FaviconUrl = "/favicon.ico";
                store.PngIconUrl = "/icon-192.png";
                store.DefaultCurrencyCode = "EUR";
                store.DefaultCulture = "en-US";
                store.SupportEmail = "support@example.local";
                store.SupportPhone = "+1 555 0100";
                store.CompanyName = "Default QA Store Ltd";
                store.CompanyEmail = "support@example.local";
                store.CompanyPhone = "+1 555 0100";
                store.CompanyAddress = "1 QA Street, QA City, US";
                store.MaintenanceModeEnabled = false;
                store.MaintenanceMessage = null;
                store.UpdatedAt = now;

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
                }

                await this.dbContext.SaveChangesAsync(cancellationToken);
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
                    CreatedAt = now,
                };
                this.dbContext.CommerceStores.Add(store);
            }

            store.Name = name;
            store.Status = status;
            store.BaseUrl = baseUrl;
            store.ForceHttps = false;
            store.SslEnabled = false;
            store.DefaultCurrencyCode = "EUR";
            store.DefaultCulture = "en-US";
            store.SupportEmail = "support@example.local";
            store.LogoUrl = "/images/banner-bg.jpg";
            store.FaviconUrl = "/favicon.ico";
            store.PngIconUrl = "/icon-192.png";
            store.CompanyName = $"{name} Ltd";
            store.CompanyEmail = "support@example.local";
            store.MaintenanceModeEnabled = maintenanceModeEnabled;
            store.MaintenanceMessage = maintenanceMessage;
            store.UpdatedAt = now;

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
                new StorePaymentMethod
                {
                    StoreId = storeId,
                    PaymentMethodKey = PaymentMethodKeys.PayPal,
                    Enabled = false,
                    DisplayName = "PayPal",
                    Description = "PayPal payment skeleton.",
                    DisplayOrder = 30,
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

        private async Task EnsureStoreConfigurationAsync(Guid storeId, CancellationToken cancellationToken)
        {
            foreach (var featureKey in new[]
            {
                StoreFeatureKeys.Checkout,
                StoreFeatureKeys.CustomerAccounts,
                StoreFeatureKeys.Newsletter,
                StoreFeatureKeys.Recommendations,
            })
            {
                var feature = await this.dbContext.StoreFeatureStates
                    .FirstOrDefaultAsync(item => item.StoreId == storeId && item.FeatureKey == featureKey, cancellationToken);
                if (feature is null)
                {
                    this.dbContext.StoreFeatureStates.Add(new StoreFeatureState
                    {
                        StoreId = storeId,
                        FeatureKey = featureKey,
                        Enabled = true,
                        Reason = "Development QA seed",
                    });
                }
                else
                {
                    feature.Enabled = true;
                    feature.Reason = "Development QA seed";
                    feature.UpdatedAt = DateTime.UtcNow;
                }
            }

            await this.EnsureStoreCurrencyAsync(storeId, "EUR", isDefault: true, displayOrder: 10, "de-DE", "€", cancellationToken);
            await this.EnsureStoreCurrencyAsync(storeId, "USD", isDefault: false, displayOrder: 20, "en-US", "$", cancellationToken);
            await this.EnsureExchangeRateAsync(storeId, "EUR", "USD", 1.10m, cancellationToken);
            await this.EnsureShippingSettingsAsync(storeId, cancellationToken);
            await this.EnsureSecurityPrivacySettingsAsync(storeId, cancellationToken);

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureStoreCurrencyAsync(
            Guid storeId,
            string currencyCode,
            bool isDefault,
            int displayOrder,
            string cultureName,
            string symbol,
            CancellationToken cancellationToken)
        {
            var currency = await this.dbContext.StoreCurrencies
                .FirstOrDefaultAsync(item => item.StoreId == storeId && item.CurrencyCode == currencyCode, cancellationToken);
            if (currency is null)
            {
                this.dbContext.StoreCurrencies.Add(new StoreCurrency
                {
                    StoreId = storeId,
                    CurrencyCode = currencyCode,
                    IsEnabled = true,
                    IsDefaultDisplayCurrency = isDefault,
                    DisplayOrder = displayOrder,
                    CultureName = cultureName,
                    Symbol = symbol,
                });
                return;
            }

            currency.IsEnabled = true;
            currency.IsDefaultDisplayCurrency = isDefault;
            currency.DisplayOrder = displayOrder;
            currency.CultureName = cultureName;
            currency.Symbol = symbol;
            currency.UpdatedAt = DateTimeOffset.UtcNow;
        }

        private async Task EnsureExchangeRateAsync(
            Guid storeId,
            string baseCurrencyCode,
            string targetCurrencyCode,
            decimal rate,
            CancellationToken cancellationToken)
        {
            var exchangeRate = await this.dbContext.StoreCurrencyExchangeRates.FirstOrDefaultAsync(
                item => item.StoreId == storeId
                    && item.BaseCurrencyCode == baseCurrencyCode
                    && item.TargetCurrencyCode == targetCurrencyCode
                    && item.ProviderKey == "manual",
                cancellationToken);
            if (exchangeRate is null)
            {
                this.dbContext.StoreCurrencyExchangeRates.Add(new StoreCurrencyExchangeRate
                {
                    StoreId = storeId,
                    BaseCurrencyCode = baseCurrencyCode,
                    TargetCurrencyCode = targetCurrencyCode,
                    Rate = rate,
                    ProviderKey = "manual",
                    Source = "development-qa-seed",
                    EffectiveAt = DateTimeOffset.UtcNow.AddDays(-1),
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
                    IsManual = true,
                    IsEnabled = true,
                });
                return;
            }

            exchangeRate.Rate = rate;
            exchangeRate.Source = "development-qa-seed";
            exchangeRate.EffectiveAt = DateTimeOffset.UtcNow.AddDays(-1);
            exchangeRate.ExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
            exchangeRate.IsManual = true;
            exchangeRate.IsEnabled = true;
            exchangeRate.UpdatedAt = DateTimeOffset.UtcNow;
        }

        private async Task EnsureShippingSettingsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var settings = await this.dbContext.StoreShippingSettings
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            if (settings is null)
            {
                settings = new StoreShippingSettings
                {
                    StoreId = storeId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StoreShippingSettings.Add(settings);
            }

            settings.OriginFullName = "Default QA Store Fulfillment";
            settings.OriginCompany = "Default QA Store Ltd";
            settings.OriginAddress1 = "1 QA Street";
            settings.OriginCity = "QA City";
            settings.OriginStateProvinceCode = "CA";
            settings.OriginPostalCode = "94105";
            settings.OriginCountryCode = "US";
            settings.EnabledCountryCodesJson = JsonSerializer.Serialize(new[] { "US", "VN" });
            settings.DefaultFlatRate = 7.50m;
            settings.FreeShippingThreshold = 150.00m;
            settings.SurchargePolicy = "sum";
            settings.DefaultDeliveryEstimateText = "3-5 business days";
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            settings.UpdatedByUserId = "development-qa-seed";
        }

        private async Task EnsureSecurityPrivacySettingsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var settings = await this.dbContext.StoreSecurityPrivacySettings
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            if (settings is null)
            {
                settings = new StoreSecurityPrivacySettings
                {
                    StoreId = storeId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StoreSecurityPrivacySettings.Add(settings);
            }

            settings.ConsentEnabled = true;
            settings.ConsentVersion = "2026-07-qa";
            settings.ConsentBannerRequired = true;
            settings.OptionalCategoriesDefaultEnabled = false;
            settings.PolicyPagePath = "/pages/cookies";
            settings.CaptchaEnabled = false;
            settings.CaptchaProviderSystemName = "none";
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            settings.UpdatedByUserId = "development-qa-seed";
        }

        private async Task EnsureCategoriesAsync(Guid storeId, CancellationToken cancellationToken)
        {
            if (!await this.dbContext.Categories.AnyAsync(category => category.Id == ApparelCategoryId, cancellationToken))
            {
                this.dbContext.Categories.Add(new Category
                {
                    Id = ApparelCategoryId,
                    StoreId = storeId,
                    Name = "Apparel",
                    Slug = "apparel",
                    Image = "/images/banner-bg.jpg",
                    DisplayOrder = 10,
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow,
                    MetaTitle = "Apparel",
                    MetaDescription = "QA apparel category for catalog expansion.",
                });
            }

            if (!await this.dbContext.Categories.AnyAsync(category => category.Id == TshirtsCategoryId, cancellationToken))
            {
                this.dbContext.Categories.Add(new Category
                {
                    Id = TshirtsCategoryId,
                    StoreId = storeId,
                    ParentCategoryId = ApparelCategoryId,
                    Name = "T-Shirts",
                    Slug = "t-shirts",
                    Image = "/images/banner-bg.jpg",
                    DisplayOrder = 20,
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow,
                    MetaTitle = "T-Shirts",
                    MetaDescription = "QA t-shirt category with variant products.",
                });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureProductsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            if (!await this.dbContext.Products.AnyAsync(product => product.Id == TshirtProductId, cancellationToken))
            {
                this.dbContext.Products.Add(new Product
                {
                    Id = TshirtProductId,
                    StoreId = storeId,
                    CategoryId = TshirtsCategoryId,
                    Name = "Catalog QA T-Shirt",
                    Slug = "catalog-qa-t-shirt",
                    Sku = "QA-TSHIRT",
                    Description = "A catalog QA t-shirt with color and size variants.",
                    ShortDescription = "QA t-shirt with variants.",
                    FullDescription = "A catalog expansion QA product used to verify variant selection, stock filtering, and order snapshots.",
                    Price = 19.99m,
                    ComparePrice = 24.99m,
                    Image = "/images/banner-bg.jpg",
                    Quantity = 30,
                    ProductType = ProductTypes.VariantInventory,
                    ManageStock = true,
                    MinOrderQuantity = 1,
                    QuantityStep = 1,
                    ShippingRequired = true,
                    DisplayOrder = 10,
                    IsPublished = true,
                    CreatedOn = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow,
                    PublishedOn = DateTime.UtcNow.AddDays(-2),
                    MetaTitle = "Catalog QA T-Shirt",
                    MetaDescription = "Catalog expansion QA t-shirt.",
                });
            }

            if (!await this.dbContext.Products.AnyAsync(product => product.Id == LowStockProductId, cancellationToken))
            {
                this.dbContext.Products.Add(new Product
                {
                    Id = LowStockProductId,
                    StoreId = storeId,
                    CategoryId = TshirtsCategoryId,
                    Name = "Catalog QA Low Stock Tee",
                    Slug = "catalog-qa-low-stock-tee",
                    Sku = "QA-LOW-STOCK",
                    Description = "A low-stock QA product.",
                    ShortDescription = "Low-stock QA product.",
                    FullDescription = "A catalog expansion QA product used to verify low-stock and in-stock filtering.",
                    Price = 15.99m,
                    Image = "/images/banner-bg.jpg",
                    Quantity = 1,
                    ManageStock = true,
                    MinOrderQuantity = 1,
                    QuantityStep = 1,
                    DisplayOrder = 20,
                    IsPublished = true,
                    CreatedOn = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow,
                    PublishedOn = DateTime.UtcNow.AddDays(-1),
                });
            }

            await this.dbContext.SaveChangesAsync(cancellationToken);

            await this.EnsureQaProductAsync(
                new ProductSeed(
                    SimpleProductId,
                    "QA Simple Product 100",
                    "qa-simple-product-100",
                    "QA-P1-SIMPLE",
                    "Published simple QA product with managed stock 20 and price 100.",
                    100.00m,
                    20,
                    DisplayOrder: 30),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    OutOfStockProductId,
                    "QA Out Of Stock Product",
                    "qa-out-of-stock-product",
                    "QA-P2-OOS",
                    "Published simple QA product with stock 0 and visible unavailable state.",
                    45.00m,
                    0,
                    DisplayOrder: 40),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    UnmanagedStockProductId,
                    "QA Unmanaged Stock Product",
                    "qa-unmanaged-stock-product",
                    "QA-P3-UNMANAGED",
                    "Published QA product with unmanaged stock and quantity 0.",
                    55.00m,
                    0,
                    DisplayOrder: 50,
                    ManageStock: false),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    QuantityRuleProductId,
                    "QA Quantity Rule Product",
                    "qa-quantity-rule-product",
                    "QA-P4-QTY",
                    "Published QA product with min quantity 2, max 10, step 2.",
                    25.00m,
                    40,
                    DisplayOrder: 60,
                    MinOrderQuantity: 2,
                    MaxOrderQuantity: 10,
                    QuantityStep: 2),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    UnpublishedProductId,
                    "QA Unpublished Product",
                    "qa-unpublished-product",
                    "QA-P8-UNPUBLISHED",
                    "Unpublished QA product for listing and direct-route not-found checks.",
                    31.00m,
                    10,
                    DisplayOrder: 80,
                    IsPublished: false,
                    PublishedOn: null),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    FutureProductId,
                    "QA Future Product",
                    "qa-future-product",
                    "QA-P9-FUTURE",
                    "QA product scheduled for the future.",
                    32.00m,
                    10,
                    DisplayOrder: 90,
                    AvailableStartUtc: DateTime.UtcNow.AddDays(14)),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    ExpiredProductId,
                    "QA Expired Product",
                    "qa-expired-product",
                    "QA-P10-EXPIRED",
                    "QA product whose availability window has ended.",
                    33.00m,
                    10,
                    DisplayOrder: 100,
                    AvailableEndUtc: DateTime.UtcNow.AddDays(-1)),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    SurchargeProductId,
                    "QA Shipping Surcharge Product",
                    "qa-shipping-surcharge-product",
                    "QA-P11-SURCHARGE",
                    "Physical QA product with shipping surcharge.",
                    120.00m,
                    20,
                    DisplayOrder: 110,
                    ShippingSurcharge: 12.50m),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    DigitalProductId,
                    "QA Digital No Shipping Product",
                    "qa-digital-no-shipping-product",
                    "QA-P12-DIGITAL",
                    "QA product that does not require shipping.",
                    35.00m,
                    50,
                    DisplayOrder: 120,
                    ShippingRequired: false),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    SeoMediaProductId,
                    "QA SEO Media Product",
                    "qa-seo-media-product",
                    "QA-P13-SEO-MEDIA",
                    "QA product with compare price and SEO metadata.",
                    88.00m,
                    25,
                    DisplayOrder: 130,
                    ComparePrice: 120.00m,
                    MetaTitle: "QA SEO Media Product",
                    MetaDescription: "Product fixture for SEO, media, compare price, and JSON-LD QA.",
                    OgTitle: "QA SEO Media Product",
                    OgDescription: "SEO product fixture with safe media.",
                    OgImage: "/images/banner-bg.jpg"),
                storeId,
                TshirtsCategoryId,
                cancellationToken);
            await this.EnsureQaProductAsync(
                new ProductSeed(
                    HtmlNameProductId,
                    "QA Unicode <Safe> Tee",
                    "qa-unicode-safe-tee",
                    "QA-P15-UNICODE",
                    "QA product with Unicode and HTML-like text for escaping checks.",
                    42.00m,
                    12,
                    DisplayOrder: 150),
                storeId,
                TshirtsCategoryId,
                cancellationToken);

            var variantProduct = await this.dbContext.Products.FirstAsync(product => product.Id == TshirtProductId, cancellationToken);
            variantProduct.ProductType = ProductTypes.VariantInventory;
            variantProduct.ManageStock = true;
            variantProduct.Quantity = 30;
            variantProduct.ShippingRequired = true;
            variantProduct.UpdatedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            await this.EnsureVariantAsync(
                TshirtRedMVariantId,
                TshirtProductId,
                "QA-TSHIRT-RED-M",
                [new("Color", "Red"), new("Size", "M")],
                19.99m,
                8,
                true,
                true,
                cancellationToken);
            await this.EnsureVariantAsync(
                TshirtRedXlVariantId,
                TshirtProductId,
                "QA-TSHIRT-RED-XL",
                [new("Color", "Red"), new("Size", "XL")],
                21.99m,
                3,
                false,
                true,
                cancellationToken);
            await this.EnsureVariantAsync(
                TshirtBlackMVariantId,
                TshirtProductId,
                "QA-TSHIRT-BLACK-M",
                [new("Color", "Black"), new("Size", "M")],
                null,
                0,
                false,
                false,
                cancellationToken);
        }

        private async Task EnsureQaProductAsync(
            ProductSeed seed,
            Guid storeId,
            Guid categoryId,
            CancellationToken cancellationToken)
        {
            var product = await this.dbContext.Products.FirstOrDefaultAsync(item => item.Id == seed.Id, cancellationToken);
            if (product is null)
            {
                product = new Product { Id = seed.Id };
                this.dbContext.Products.Add(product);
            }

            product.StoreId = storeId;
            product.CategoryId = categoryId;
            product.Name = seed.Name;
            product.Slug = seed.Slug;
            product.Sku = seed.Sku;
            product.Description = seed.Description;
            product.ShortDescription = seed.Description;
            product.FullDescription = seed.Description;
            product.Price = seed.Price;
            product.ComparePrice = seed.ComparePrice;
            product.Image = seed.Image;
            product.Quantity = seed.Quantity;
            product.ProductType = ProductTypes.Simple;
            product.ManageStock = seed.ManageStock;
            product.MinOrderQuantity = seed.MinOrderQuantity;
            product.MaxOrderQuantity = seed.MaxOrderQuantity;
            product.QuantityStep = seed.QuantityStep;
            product.PurchasingDisabled = seed.PurchasingDisabled;
            product.PurchasingDisabledReason = seed.PurchasingDisabledReason;
            product.HideWhenOutOfStock = seed.HideWhenOutOfStock;
            product.ShippingRequired = seed.ShippingRequired;
            product.FreeShipping = seed.FreeShipping;
            product.ShippingSurcharge = seed.ShippingSurcharge;
            product.DeliveryEstimateText = seed.DeliveryEstimateText;
            product.DisplayOrder = seed.DisplayOrder;
            product.IsPublished = seed.IsPublished;
            product.PublishedOn = seed.PublishedOn ?? (seed.IsPublished ? DateTime.UtcNow.AddDays(-3) : null);
            product.AvailableStartUtc = seed.AvailableStartUtc;
            product.AvailableEndUtc = seed.AvailableEndUtc;
            product.MetaTitle = seed.MetaTitle ?? seed.Name;
            product.MetaDescription = seed.MetaDescription ?? seed.Description;
            product.OgTitle = seed.OgTitle ?? seed.Name;
            product.OgDescription = seed.OgDescription ?? seed.Description;
            product.OgImage = seed.OgImage ?? seed.Image;
            product.RobotsIndex = seed.RobotsIndex;
            product.RobotsFollow = seed.RobotsFollow;
            product.SeoContent = seed.SeoContent;
            product.CreatedOn = seed.CreatedOn ?? DateTime.UtcNow.AddDays(-3);
            product.UpdatedAt = DateTime.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureVariantAsync(
            Guid id,
            Guid productId,
            string sku,
            IReadOnlyList<VariantAttributeSeed> attributes,
            decimal? price,
            int stock,
            bool isDefault,
            bool isActive,
            CancellationToken cancellationToken)
        {
            var existing = await this.dbContext.ProductVariants.FirstOrDefaultAsync(variant => variant.Id == id, cancellationToken);
            if (existing is not null)
            {
                existing.Sku = sku;
                existing.Price = price;
                existing.Stock = stock;
                existing.IsDefault = isDefault;
                existing.IsActive = isActive;
                await this.dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var signature = string.Join(
                "|",
                attributes
                    .OrderBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(attribute => $"{attribute.Name.Trim().ToLowerInvariant()}={attribute.Value.Trim().ToLowerInvariant()}"));

            this.dbContext.ProductVariants.Add(new ProductVariant
            {
                Id = id,
                ProductId = productId,
                Sku = sku,
                AttributesJson = JsonSerializer.Serialize(attributes),
                AttributeSignature = signature,
                DisplayName = string.Join(" / ", attributes.Select(attribute => attribute.Value)),
                SizeScale = SizeScale.ClothingAlpha,
                SizeValue = attributes.FirstOrDefault(attribute => attribute.Name == "Size")?.Value ?? string.Empty,
                Color = attributes.FirstOrDefault(attribute => attribute.Name == "Color")?.Value,
                Price = price,
                Stock = stock,
                IsDefault = isDefault,
                IsActive = isActive,
            });

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureStorefrontPagesAsync(Guid storeId, CancellationToken cancellationToken)
        {
            await this.EnsureStorefrontPageAsync(
                LegalPageId,
                storeId,
                "qa-legal",
                "QA Legal Page",
                "Published legal page used by release E2E.",
                "<p>This is a synthetic legal page for Storefront release QA.</p>",
                isPublished: true,
                includeInSitemap: true,
                includeInNavigation: true,
                StorefrontPageContentRules.FooterLegal,
                pageKey: "terms_conditions",
                cancellationToken);
            await this.EnsureStorefrontPageAsync(
                CookiePageId,
                storeId,
                "cookies",
                "Cookies",
                "Cookie information for consent QA.",
                "<p>Cookie policy content for development QA.</p>",
                isPublished: true,
                includeInSitemap: true,
                includeInNavigation: true,
                StorefrontPageContentRules.FooterLegal,
                pageKey: "cookie_information",
                cancellationToken);
            await this.EnsureStorefrontPageAsync(
                DraftPageId,
                storeId,
                "qa-unpublished-page",
                "QA Unpublished Page",
                "This page must not be public.",
                "<p>Unpublished content should not leak.</p>",
                isPublished: false,
                includeInSitemap: false,
                includeInNavigation: false,
                navigationLocation: null,
                pageKey: null,
                cancellationToken);

            await this.EnsureSeoRedirectAsync(
                storeId,
                "/pages/qa-legal-old",
                "/pages/qa-legal",
                "StorefrontPage",
                LegalPageId,
                cancellationToken);
        }

        private async Task EnsureStorefrontPageAsync(
            Guid id,
            Guid storeId,
            string slug,
            string title,
            string intro,
            string bodyHtml,
            bool isPublished,
            bool includeInSitemap,
            bool includeInNavigation,
            string? navigationLocation,
            string? pageKey,
            CancellationToken cancellationToken)
        {
            var page = await this.dbContext.StorefrontPages
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (page is null)
            {
                page = new StorefrontPage
                {
                    Id = id,
                    StoreId = storeId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StorefrontPages.Add(page);
            }

            page.StoreId = storeId;
            page.PublicId = id == LegalPageId
                ? LegalPagePublicId
                : id == CookiePageId
                    ? CookiePagePublicId
                    : id == DraftPageId
                        ? DraftPagePublicId
                        : page.PublicId;
            page.Slug = slug;
            page.Title = title;
            page.Intro = intro;
            page.BodyHtml = bodyHtml;
            page.IsPublished = isPublished;
            page.IncludeInSitemap = includeInSitemap;
            page.IncludeInNavigation = includeInNavigation;
            page.NavigationLocation = navigationLocation;
            page.PageKey = pageKey;
            page.DisplayOrder = 10;
            page.MetaTitle = title;
            page.MetaDescription = intro;
            page.OgTitle = title;
            page.OgDescription = intro;
            page.RobotsIndex = isPublished;
            page.RobotsFollow = isPublished;
            page.ArchivedAt = null;
            page.UpdatedAt = DateTimeOffset.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureSeoRedirectAsync(
            Guid storeId,
            string oldPath,
            string newPath,
            string entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            var redirect = await this.dbContext.SeoRedirects
                .FirstOrDefaultAsync(item => item.StoreId == storeId && item.OldPath == oldPath, cancellationToken);
            if (redirect is null)
            {
                this.dbContext.SeoRedirects.Add(new SeoRedirect
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldPath = oldPath,
                    NewPath = newPath,
                    StatusCode = SeoConstraints.PermanentRedirectStatusCode,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                });
                await this.dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            redirect.NewPath = newPath;
            redirect.EntityType = entityType;
            redirect.EntityId = entityId;
            redirect.StatusCode = SeoConstraints.PermanentRedirectStatusCode;
            redirect.IsActive = true;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureNavigationAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var main = await this.EnsureNavigationMenuAsync(
                storeId,
                StoreNavigationMenuNames.Main,
                "Main navigation",
                cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "Home", StoreNavigationTargetTypes.System, StoreNavigationSystemTargets.Home, null, null, 10, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "Apparel", StoreNavigationTargetTypes.Category, null, ApparelCategoryId, null, 20, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "QA Simple", StoreNavigationTargetTypes.Product, null, SimpleProductId, null, 30, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "QA Legal", StoreNavigationTargetTypes.Page, null, LegalPagePublicId, null, 40, cancellationToken);

            var footerLegal = await this.EnsureNavigationMenuAsync(
                storeId,
                StoreNavigationMenuNames.FooterLegal,
                "Footer legal",
                cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, footerLegal.Id, "Terms", StoreNavigationTargetTypes.Page, null, LegalPagePublicId, null, 10, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, footerLegal.Id, "Cookies", StoreNavigationTargetTypes.Page, null, CookiePagePublicId, null, 20, cancellationToken);
        }

        private async Task<StoreNavigationMenu> EnsureNavigationMenuAsync(
            Guid storeId,
            string systemName,
            string displayName,
            CancellationToken cancellationToken)
        {
            var menu = await this.dbContext.StoreNavigationMenus
                .FirstOrDefaultAsync(item => item.StoreId == storeId && item.SystemName == systemName && item.ArchivedAt == null, cancellationToken);
            if (menu is null)
            {
                menu = new StoreNavigationMenu
                {
                    StoreId = storeId,
                    SystemName = systemName,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StoreNavigationMenus.Add(menu);
            }

            menu.DisplayName = displayName;
            menu.IsEnabled = true;
            menu.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
            return menu;
        }

        private async Task EnsureNavigationItemAsync(
            Guid storeId,
            Guid menuId,
            string label,
            string targetType,
            string? targetKey,
            Guid? targetEntityPublicId,
            string? url,
            int displayOrder,
            CancellationToken cancellationToken)
        {
            var item = await this.dbContext.StoreNavigationMenuItems.FirstOrDefaultAsync(
                candidate => candidate.StoreId == storeId
                    && candidate.MenuId == menuId
                    && candidate.Label == label
                    && candidate.ArchivedAt == null,
                cancellationToken);
            if (item is null)
            {
                item = new StoreNavigationMenuItem
                {
                    StoreId = storeId,
                    MenuId = menuId,
                    Label = label,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StoreNavigationMenuItems.Add(item);
            }

            item.TargetType = targetType;
            item.TargetKey = targetKey;
            item.TargetEntityPublicId = targetEntityPublicId;
            item.Url = url;
            item.IsEnabled = true;
            item.DisplayOrder = displayOrder;
            item.OpensInNewTab = false;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<CommerceCustomer> EnsureCustomerAsync(
            Guid storeId,
            Guid customerId,
            Guid shippingAddressId,
            Guid? billingAddressId,
            string email,
            string password,
            string fullName,
            CancellationToken cancellationToken)
        {
            var user = await this.userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    FullName = fullName,
                    CreatedOn = DateTime.UtcNow,
                };

                var result = await this.userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var message = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not seed storefront QA user {email}: {message}");
                }
            }
            else
            {
                user.EmailConfirmed = true;
                user.FullName = fullName;
                user.UserName = email;
                user.Email = email;
                await this.userManager.UpdateAsync(user);
            }

            if (!await this.userManager.CheckPasswordAsync(user, password))
            {
                var resetToken = await this.userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await this.userManager.ResetPasswordAsync(user, resetToken, password);
                if (!resetResult.Succeeded)
                {
                    var message = string.Join("; ", resetResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not reset storefront QA user password for {email}: {message}");
                }
            }

            if (!await this.userManager.IsInRoleAsync(user, StorefrontQaUserRole))
            {
                var roleResult = await this.userManager.AddToRoleAsync(user, StorefrontQaUserRole);
                if (!roleResult.Succeeded)
                {
                    var message = string.Join("; ", roleResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Could not assign storefront QA user role for {email}: {message}");
                }
            }

            var normalizedEmail = email.ToUpperInvariant();
            var customer = await this.dbContext.CommerceCustomers.FirstOrDefaultAsync(
                item => item.StoreId == storeId && item.NormalizedEmail == normalizedEmail,
                cancellationToken);
            if (customer is null)
            {
                customer = new CommerceCustomer
                {
                    Id = customerId,
                    StoreId = storeId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.CommerceCustomers.Add(customer);
            }

            customer.AppUserId = user.Id;
            customer.Email = email;
            customer.NormalizedEmail = normalizedEmail;
            customer.FullName = fullName;
            customer.FirstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;
            customer.LastName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "Customer";
            customer.Company = "QA Synthetic";
            customer.Phone = "+1 555 0101";
            customer.PreferredLanguage = "en-US";
            customer.PreferredCurrencyCode = "EUR";
            customer.IsActive = true;
            customer.LastActivityAtUtc = DateTimeOffset.UtcNow;
            customer.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            await this.EnsureCustomerAddressAsync(
                shippingAddressId,
                storeId,
                customer.Id,
                "QA",
                "Customer",
                isDefaultShipping: true,
                isDefaultBilling: billingAddressId is null,
                email,
                cancellationToken);

            if (billingAddressId is { } secondaryAddressId)
            {
                await this.EnsureCustomerAddressAsync(
                    secondaryAddressId,
                    storeId,
                    customer.Id,
                    "QA",
                    "Billing",
                    isDefaultShipping: false,
                    isDefaultBilling: true,
                    email,
                    cancellationToken);
            }

            return customer;
        }

        private async Task EnsureCustomerAddressAsync(
            Guid id,
            Guid storeId,
            Guid customerId,
            string firstName,
            string lastName,
            bool isDefaultShipping,
            bool isDefaultBilling,
            string email,
            CancellationToken cancellationToken)
        {
            var address = await this.dbContext.CommerceCustomerAddresses
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (address is null)
            {
                address = new CommerceCustomerAddress
                {
                    Id = id,
                    PublicId = id,
                    StoreId = storeId,
                    CustomerId = customerId,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                };
                this.dbContext.CommerceCustomerAddresses.Add(address);
            }

            address.StoreId = storeId;
            address.CustomerId = customerId;
            address.FirstName = firstName;
            address.LastName = lastName;
            address.Company = "QA Synthetic";
            address.Address1 = "1 QA Street";
            address.Address2 = isDefaultBilling ? "Billing Suite" : "Shipping Suite";
            address.City = "San Francisco";
            address.StateProvinceCode = "CA";
            address.StateProvinceName = "California";
            address.PostalCode = "94105";
            address.CountryCode = "US";
            address.Phone = "+1 555 0101";
            address.Email = email;
            address.IsDefaultShipping = isDefaultShipping;
            address.IsDefaultBilling = isDefaultBilling;
            address.DeletedAtUtc = null;
            address.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureIsolationCatalogAsync(Guid storeId, CancellationToken cancellationToken)
        {
            if (!await this.dbContext.Categories.AnyAsync(category => category.Id == QaS2CategoryId, cancellationToken))
            {
                this.dbContext.Categories.Add(new Category
                {
                    Id = QaS2CategoryId,
                    StoreId = storeId,
                    Name = "S2 Apparel",
                    Slug = "apparel",
                    Image = "/images/banner-bg.jpg",
                    DisplayOrder = 10,
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow,
                    MetaTitle = "S2 Apparel",
                    MetaDescription = "Second-store category for isolation QA.",
                });
                await this.dbContext.SaveChangesAsync(cancellationToken);
            }

            await this.EnsureQaProductAsync(
                new ProductSeed(
                    QaS2ProductId,
                    "S2 Isolation Product",
                    "qa-simple-product-100",
                    "QA-S2-P1",
                    "Same slug as S1 product but owned by S2 for isolation QA.",
                    100.00m,
                    20,
                    DisplayOrder: 10),
                storeId,
                QaS2CategoryId,
                cancellationToken);
        }

        private async Task EnsureSampleOrderAsync(CommerceStore store, CommerceCustomer customer, CancellationToken cancellationToken)
        {
            var reference = store.StoreKey == DefaultStoreKey ? "QA-CATALOG-SNAPSHOT" : $"QA-{store.StoreKey.ToUpperInvariant()}-SNAPSHOT";
            if (await this.dbContext.Orders.AnyAsync(order => order.Reference == reference, cancellationToken))
            {
                return;
            }

            this.dbContext.Orders.Add(new Order
            {
                StoreId = store.Id,
                StorePublicId = store.PublicId,
                StoreKeySnapshot = store.StoreKey,
                StoreNameSnapshot = store.Name,
                StoreBaseUrlSnapshot = store.BaseUrl,
                StoreCompanyNameSnapshot = store.CompanyName,
                StoreCompanyEmailSnapshot = store.CompanyEmail,
                StoreCompanyPhoneSnapshot = store.CompanyPhone,
                StoreCompanyAddressSnapshot = store.CompanyAddress,
                UserId = customer.AppUserId ?? "qa-seed-user",
                CustomerId = customer.Id,
                Reference = reference,
                OrderStatus = OrderStatuses.Complete,
                PaymentStatus = PaymentStatuses.Paid,
                PaymentMethodKey = PaymentMethodKeys.Cod,
                PaymentAt = DateTime.UtcNow,
                PaymentMetadataJson = JsonSerializer.Serialize(new { handler = PaymentMethodKeys.Cod, mode = "seed" }),
                CurrencyCode = "EUR",
                TotalAmount = 19.99m,
                SubtotalAmount = 19.99m,
                ShippingTotalAmount = 0m,
                TaxTotalAmount = 0m,
                DiscountTotalAmount = 0m,
                GrandTotalAmount = 19.99m,
                BaseCurrencyCode = "EUR",
                BaseTotalAmount = 19.99m,
                BaseSubtotalAmount = 19.99m,
                BaseShippingTotalAmount = 0m,
                BaseTaxTotalAmount = 0m,
                BaseDiscountTotalAmount = 0m,
                BaseGrandTotalAmount = 19.99m,
                CreatedOn = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CustomerName = customer.FullName,
                CustomerEmail = customer.Email,
                ShippingFullName = customer.FullName,
                ShippingEmail = customer.Email,
                ShippingAddress1 = "1 QA Street",
                ShippingCity = "QA City",
                ShippingPostalCode = "10000",
                ShippingCountryCode = "US",
                ShippingStatus = ShippingStatuses.NotYetShipped,
                ShippingMethodKey = "free-standard",
                ShippingProviderSystemName = "internal",
                ShippingMethodCode = "free-standard",
                ShippingMethodName = "Free standard shipping",
                ShippingTotal = 0m,
                ShippingCurrencyCode = "EUR",
                ShippingDeliveryEstimateText = "3-5 business days",
                Lines =
                [
                    new OrderLine
                    {
                        ProductId = TshirtProductId,
                        ProductName = "Catalog QA T-Shirt",
                        Sku = "QA-TSHIRT-RED-M",
                        Image = "/images/banner-bg.jpg",
                        ProductVariantId = TshirtRedMVariantId,
                        VariantAttributesJson = JsonSerializer.Serialize(
                            new[]
                            {
                                new VariantAttributeSeed("Color", "Red"),
                                new VariantAttributeSeed("Size", "M"),
                            }),
                        Quantity = 1,
                        UnitPrice = 19.99m,
                    },
                ],
            });

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private sealed record ProductSeed(
            Guid Id,
            string Name,
            string Slug,
            string Sku,
            string Description,
            decimal Price,
            int Quantity,
            int DisplayOrder,
            string Image = "/images/banner-bg.jpg",
            bool ManageStock = true,
            int MinOrderQuantity = 1,
            int? MaxOrderQuantity = null,
            int QuantityStep = 1,
            bool PurchasingDisabled = false,
            string? PurchasingDisabledReason = null,
            bool HideWhenOutOfStock = false,
            bool ShippingRequired = true,
            bool FreeShipping = false,
            decimal? ShippingSurcharge = null,
            string? DeliveryEstimateText = null,
            bool IsPublished = true,
            DateTime? PublishedOn = null,
            DateTime? AvailableStartUtc = null,
            DateTime? AvailableEndUtc = null,
            decimal? ComparePrice = null,
            string? MetaTitle = null,
            string? MetaDescription = null,
            string? OgTitle = null,
            string? OgDescription = null,
            string? OgImage = null,
            bool RobotsIndex = true,
            bool RobotsFollow = true,
            string? SeoContent = null,
            DateTime? CreatedOn = null);

        private sealed record VariantAttributeSeed(string Name, string Value);
    }
}
