namespace BlazorShop.Storefront.Runtime
{
    public sealed record StorefrontRuntimeCurrentStore(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps,
        string? CdnHost,
        string? LogoUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string DefaultCurrencyCode,
        string DefaultCulture,
        string? SupportEmail,
        string? SupportPhone,
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage,
        string? HtmlBodyId);

    public sealed record StorefrontRuntimePublicConfiguration(
        StorefrontRuntimeStoreIdentity StoreIdentity,
        StorefrontRuntimeBranding Branding,
        StorefrontRuntimeLocaleOptions LocaleOptions,
        StorefrontRuntimeCurrencyOptions CurrencyOptions,
        StorefrontRuntimeConsentConfiguration Consent,
        StorefrontRuntimeCaptchaConfiguration Captcha,
        StorefrontRuntimeMaintenanceState MaintenanceState,
        StorefrontRuntimeFeatureFlags FeatureFlags,
        IReadOnlyDictionary<string, StorefrontRuntimeCapabilityState> Features,
        IReadOnlyList<StorefrontRuntimePublicPaymentMethod> PaymentMethods,
        StorefrontRuntimeSeoDefaults SeoDefaults);

    public sealed record StorefrontRuntimeStoreIdentity(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps);

    public sealed record StorefrontRuntimeBranding(
        string? CdnHost,
        string? LogoUrl,
        string? CompanyName,
        string? CompanyEmail,
        string? CompanyPhone,
        string? CompanyAddress,
        string? FaviconUrl,
        string? PngIconUrl,
        string? AppleTouchIconUrl,
        string? MsTileImageUrl,
        string? MsTileColor,
        string? SupportEmail,
        string? SupportPhone,
        string? HtmlBodyId);

    public sealed record StorefrontRuntimeLocaleOptions(
        string DefaultCulture,
        IReadOnlyList<string> SupportedCultures);

    public sealed record StorefrontRuntimeCurrencyOptions(
        string DefaultCurrencyCode,
        IReadOnlyList<string> SupportedCurrencyCodes);

    public sealed record StorefrontRuntimeConsentConfiguration(
        bool Enabled,
        bool BannerRequired,
        string CurrentVersion,
        string PolicyPagePath,
        IReadOnlyList<StorefrontRuntimeConsentCategory> Categories,
        int VisitorCookieLifetimeDays);

    public sealed record StorefrontRuntimeConsentCategory(
        string Name,
        bool Required,
        bool DefaultEnabled);

    public sealed record StorefrontRuntimeCaptchaConfiguration(
        bool Enabled,
        string ProviderSystemName,
        string? PublicSiteKey,
        IReadOnlyList<string> EnabledTargets,
        IReadOnlyDictionary<string, string> ActionNames);

    public sealed record StorefrontRuntimeMaintenanceState(
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage);

    public sealed record StorefrontRuntimeFeatureFlags(
        bool CustomerAccountsEnabled,
        bool CartEnabled,
        bool CheckoutEnabled,
        bool PaymentsEnabled,
        bool NewsletterEnabled,
        bool RecommendationsEnabled);

    public sealed record StorefrontRuntimeCapabilityState(
        bool Supported,
        bool Enabled,
        string? Reason);

    public sealed record StorefrontRuntimePublicPaymentMethod(
        Guid Id,
        string Key,
        string Name,
        string? Description,
        string? ShortDisplayText,
        string? IconUrl,
        IReadOnlyList<string> SupportedCurrencyCodes,
        IReadOnlyList<string> SupportedCountryCodes);

    public sealed record StorefrontRuntimeSeoDefaults(
        string? SiteName,
        string? DefaultTitleSuffix,
        string? DefaultMetaDescription,
        string? DefaultOgImage,
        string? BaseCanonicalUrl,
        string? CompanyName,
        string? CompanyLogoUrl,
        string? CompanyPhone,
        string? CompanyEmail,
        string? CompanyAddress,
        string? FacebookUrl,
        string? InstagramUrl,
        string? XUrl);

    public sealed record StorefrontRuntimeCurrencyPreferenceRequest(
        string CurrencyCode);

    public sealed record StorefrontRuntimeCurrencyPreferenceResponse(
        string CurrencyCode,
        string BaseCurrencyCode,
        string? RequestedCurrencyCode,
        bool RequestedCurrencySupported,
        bool CheckoutCurrencyEnabled,
        string Reason);
}
