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

            return;
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

            return;
        }

        private async Task EnsureShippingSettingsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var settings = await this.dbContext.StoreShippingSettings
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            if (settings is not null)
            {
                return;
            }

            settings = new StoreShippingSettings
            {
                StoreId = storeId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            this.dbContext.StoreShippingSettings.Add(settings);

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

        private async Task EnsureStoreEmailSettingsAsync(
            Guid storeId,
            string fromEmail,
            CancellationToken cancellationToken)
        {
            var settings = await this.dbContext.StoreEmailSettings
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            if (settings is not null)
            {
                return;
            }

            settings = new StoreEmailSettings
            {
                StoreId = storeId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            this.dbContext.StoreEmailSettings.Add(settings);

            settings.Enabled = true;
            settings.DeliveryMode = StoreEmailDeliveryModes.Capture;
            settings.SmtpHost = "localhost";
            settings.SmtpPort = 1025;
            settings.UseSsl = false;
            settings.Username = null;
            settings.ProtectedPassword = null;
            settings.PasswordUpdatedAtUtc = null;
            settings.FromEmail = fromEmail;
            settings.FromDisplayName = "BlazorShop QA Store";
            settings.ReplyToEmail = "support@example.local";
            settings.CaptureRedirectToEmail = null;
            settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
            settings.UpdatedByUserId = "development-qa-seed";
        }

        private async Task EnsureSecurityPrivacySettingsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var settings = await this.dbContext.StoreSecurityPrivacySettings
                .FirstOrDefaultAsync(item => item.StoreId == storeId, cancellationToken);
            if (settings is not null)
            {
                return;
            }

            settings = new StoreSecurityPrivacySettings
            {
                StoreId = storeId,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            this.dbContext.StoreSecurityPrivacySettings.Add(settings);

            settings.ConsentEnabled = true;
            settings.ConsentVersion = "2026-07-qa";
            settings.ConsentBannerRequired = true;
            settings.OptionalCategoriesDefaultEnabled = false;
            settings.PolicyPagePath = "/pages/cookies";
            settings.CaptchaEnabled = false;
            settings.CaptchaProviderSystemName = "none";
            settings.RegistrationMode = "standard";
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            settings.UpdatedByUserId = "development-qa-seed";
        }
    }
}
