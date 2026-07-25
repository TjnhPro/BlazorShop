namespace BlazorShop.Storefront.Services
{
    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Services.Contracts;

    public sealed class GeneratedStorefrontConfigurationClient : IStorefrontStoreConfigurationClient
    {
        private const string CurrencyPreferenceUnavailableMessage = "Unable to update currency preference right now.";

        private readonly IStorefrontRuntimeConfigurationFacade configurationFacade;

        public GeneratedStorefrontConfigurationClient(IStorefrontRuntimeConfigurationFacade configurationFacade)
        {
            this.configurationFacade = configurationFacade;
        }

        public async Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.configurationFacade.GetCurrentStoreAsync(cancellationToken);
            return result.Success && result.Value is not null
                ? StorefrontApiResult<StorefrontCurrentStore>.Success(MapCurrentStore(result.Value))
                : MapApiFailure<StorefrontCurrentStore>(result.Error);
        }

        public async Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.configurationFacade.GetPublicConfigurationAsync(cancellationToken);
            return result.Success && result.Value is not null
                ? StorefrontApiResult<StorefrontPublicConfiguration>.Success(MapPublicConfiguration(result.Value))
                : MapApiFailure<StorefrontPublicConfiguration>(result.Error);
        }

        public async Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
            StorefrontCurrencyPreferenceRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await this.configurationFacade.SetCurrencyPreferenceAsync(
                new StorefrontRuntimeCurrencyPreferenceRequest(request.CurrencyCode),
                cancellationToken);

            return result.Success
                ? StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Succeeded(
                    result.Value is null ? null : MapCurrencyPreference(result.Value),
                    "Request completed.")
                : StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Failed(result.Error?.Message ?? CurrencyPreferenceUnavailableMessage);
        }

        private static StorefrontApiResult<T> MapApiFailure<T>(StorefrontRuntimeError? error)
        {
            return error?.Status == StorefrontRuntimeStatusCodes.NotFound
                ? StorefrontApiResult<T>.NotFound()
                : StorefrontApiResult<T>.ServiceUnavailable();
        }

        private static StorefrontCurrentStore MapCurrentStore(StorefrontRuntimeCurrentStore source)
        {
            return new StorefrontCurrentStore(
                source.PublicId,
                source.StoreKey,
                source.Name,
                source.Status,
                source.BaseUrl,
                source.PrimaryDomain,
                source.ForceHttps,
                source.CdnHost,
                source.LogoUrl,
                source.CompanyName,
                source.CompanyEmail,
                source.CompanyPhone,
                source.CompanyAddress,
                source.FaviconUrl,
                source.PngIconUrl,
                source.AppleTouchIconUrl,
                source.MsTileImageUrl,
                source.MsTileColor,
                source.DefaultCurrencyCode,
                source.DefaultCulture,
                source.SupportEmail,
                source.SupportPhone,
                source.MaintenanceModeEnabled,
                source.MaintenanceMessage,
                source.HtmlBodyId);
        }

        private static StorefrontPublicConfiguration MapPublicConfiguration(StorefrontRuntimePublicConfiguration source)
        {
            return new StorefrontPublicConfiguration(
                MapStoreIdentity(source.StoreIdentity),
                MapBranding(source.Branding),
                MapLocaleOptions(source.LocaleOptions),
                MapCurrencyOptions(source.CurrencyOptions),
                MapConsent(source.Consent),
                MapCaptcha(source.Captcha),
                MapMaintenance(source.MaintenanceState),
                MapFeatureFlags(source.FeatureFlags),
                source.Features.ToDictionary(
                    pair => pair.Key,
                    pair => new StorefrontCapability(pair.Value.Supported, pair.Value.Enabled, pair.Value.Reason),
                    StringComparer.Ordinal),
                source.PaymentMethods.Select(MapPaymentMethod).ToArray(),
                MapSeoDefaults(source.SeoDefaults));
        }

        private static StorefrontStoreIdentity MapStoreIdentity(StorefrontRuntimeStoreIdentity source)
        {
            return new StorefrontStoreIdentity(
                source.PublicId,
                source.StoreKey,
                source.Name,
                source.Status,
                source.BaseUrl,
                source.PrimaryDomain,
                source.ForceHttps);
        }

        private static StorefrontBranding MapBranding(StorefrontRuntimeBranding source)
        {
            return new StorefrontBranding(
                source.CdnHost,
                source.LogoUrl,
                source.CompanyName,
                source.CompanyEmail,
                source.CompanyPhone,
                source.CompanyAddress,
                source.FaviconUrl,
                source.PngIconUrl,
                source.AppleTouchIconUrl,
                source.MsTileImageUrl,
                source.MsTileColor,
                source.SupportEmail,
                source.SupportPhone,
                source.HtmlBodyId);
        }

        private static StorefrontLocaleOptions MapLocaleOptions(StorefrontRuntimeLocaleOptions source)
        {
            return new StorefrontLocaleOptions(source.DefaultCulture, source.SupportedCultures);
        }

        private static StorefrontCurrencyOptions MapCurrencyOptions(StorefrontRuntimeCurrencyOptions source)
        {
            return new StorefrontCurrencyOptions(source.DefaultCurrencyCode, source.SupportedCurrencyCodes);
        }

        private static StorefrontConsentConfiguration MapConsent(StorefrontRuntimeConsentConfiguration source)
        {
            return new StorefrontConsentConfiguration(
                source.Enabled,
                source.BannerRequired,
                source.CurrentVersion,
                source.PolicyPagePath,
                source.Categories.Select(category => new StorefrontConsentCategory(
                    category.Name,
                    category.Required,
                    category.DefaultEnabled)).ToArray(),
                source.VisitorCookieLifetimeDays);
        }

        private static StorefrontCaptchaConfiguration MapCaptcha(StorefrontRuntimeCaptchaConfiguration source)
        {
            return new StorefrontCaptchaConfiguration(
                source.Enabled,
                source.ProviderSystemName,
                source.PublicSiteKey,
                source.EnabledTargets,
                source.ActionNames);
        }

        private static StorefrontMaintenanceState MapMaintenance(StorefrontRuntimeMaintenanceState source)
        {
            return new StorefrontMaintenanceState(source.MaintenanceModeEnabled, source.MaintenanceMessage);
        }

        private static StorefrontFeatureFlags MapFeatureFlags(StorefrontRuntimeFeatureFlags source)
        {
            return new StorefrontFeatureFlags(
                source.CustomerAccountsEnabled,
                source.CartEnabled,
                source.CheckoutEnabled,
                source.PaymentsEnabled,
                source.NewsletterEnabled,
                source.RecommendationsEnabled);
        }

        private static StorefrontPublicPaymentMethod MapPaymentMethod(StorefrontRuntimePublicPaymentMethod source)
        {
            return new StorefrontPublicPaymentMethod(
                source.Id,
                source.Key,
                source.Name,
                source.Description,
                source.ShortDisplayText,
                source.IconUrl,
                source.SupportedCurrencyCodes,
                source.SupportedCountryCodes);
        }

        private static StorefrontSeoDefaults MapSeoDefaults(StorefrontRuntimeSeoDefaults source)
        {
            return new StorefrontSeoDefaults(
                source.SiteName,
                source.DefaultTitleSuffix,
                source.DefaultMetaDescription,
                source.DefaultOgImage,
                source.BaseCanonicalUrl,
                source.CompanyName,
                source.CompanyLogoUrl,
                source.CompanyPhone,
                source.CompanyEmail,
                source.CompanyAddress,
                source.FacebookUrl,
                source.InstagramUrl,
                source.XUrl);
        }

        private static StorefrontCurrencyPreferenceResponse MapCurrencyPreference(StorefrontRuntimeCurrencyPreferenceResponse source)
        {
            return new StorefrontCurrencyPreferenceResponse(
                source.CurrencyCode,
                source.BaseCurrencyCode,
                source.RequestedCurrencyCode,
                source.RequestedCurrencySupported,
                source.CheckoutCurrencyEnabled,
                source.Reason);
        }
    }
}
