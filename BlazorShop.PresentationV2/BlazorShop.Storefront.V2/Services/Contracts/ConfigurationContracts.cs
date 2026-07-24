namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public sealed record StorefrontCurrentStore(
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

    public sealed record StorefrontPublicConfiguration(
        StorefrontStoreIdentity StoreIdentity,
        StorefrontBranding Branding,
        StorefrontLocaleOptions LocaleOptions,
        StorefrontCurrencyOptions CurrencyOptions,
        StorefrontConsentConfiguration Consent,
        StorefrontCaptchaConfiguration Captcha,
        StorefrontMaintenanceState MaintenanceState,
        StorefrontFeatureFlags FeatureFlags,
        IReadOnlyDictionary<string, StorefrontCapability> Features,
        IReadOnlyList<StorefrontPublicPaymentMethod> PaymentMethods,
        StorefrontSeoDefaults SeoDefaults);

    public sealed record StorefrontStoreIdentity(
        Guid PublicId,
        string StoreKey,
        string Name,
        string Status,
        string? BaseUrl,
        string? PrimaryDomain,
        bool ForceHttps);

    public sealed record StorefrontBranding(
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

    public sealed record StorefrontLocaleOptions(
        string DefaultCulture,
        IReadOnlyList<string> SupportedCultures);

    public sealed record StorefrontCaptchaConfiguration(
        bool Enabled,
        string ProviderSystemName,
        string? PublicSiteKey,
        IReadOnlyList<string> EnabledTargets,
        IReadOnlyDictionary<string, string> ActionNames);

    public sealed record StorefrontMaintenanceState(
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage);

    public sealed record StorefrontFeatureFlags(
        bool CustomerAccountsEnabled,
        bool CartEnabled,
        bool CheckoutEnabled,
        bool PaymentsEnabled,
        bool NewsletterEnabled,
        bool RecommendationsEnabled);

    public sealed record StorefrontCapability(
        bool Supported,
        bool Enabled,
        string? Reason);
}
