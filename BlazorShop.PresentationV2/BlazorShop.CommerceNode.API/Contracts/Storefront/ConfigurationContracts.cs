namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed record StorefrontPublicConfigurationResponse(
        StorefrontStoreIdentityResponse StoreIdentity,
        StorefrontBrandingResponse Branding,
        StorefrontLocaleOptionsResponse LocaleOptions,
        StorefrontCurrencyOptionsResponse CurrencyOptions,
        StorefrontConsentConfigurationResponse Consent,
        StorefrontCaptchaConfigurationResponse Captcha,
        StorefrontMaintenanceStateResponse MaintenanceState,
        StorefrontFeatureFlagsResponse FeatureFlags,
        IReadOnlyDictionary<string, StorefrontCapabilityResponse> Features,
        IReadOnlyList<StorefrontPaymentMethodResponse> PaymentMethods,
        StorefrontSeoDefaultsResponse SeoDefaults);

    public sealed record StorefrontLocaleOptionsResponse(
        string DefaultCulture,
        IReadOnlyList<string> SupportedCultures);

    public sealed record StorefrontCaptchaConfigurationResponse(
        bool Enabled,
        string ProviderSystemName,
        string? PublicSiteKey,
        IReadOnlyList<string> EnabledTargets,
        IReadOnlyDictionary<string, string> ActionNames);

    public sealed record StorefrontMaintenanceStateResponse(
        bool MaintenanceModeEnabled,
        string? MaintenanceMessage);

    public sealed record StorefrontFeatureFlagsResponse(
        bool CustomerAccountsEnabled,
        bool CartEnabled,
        bool CheckoutEnabled,
        bool PaymentsEnabled,
        bool NewsletterEnabled,
        bool RecommendationsEnabled);

    public sealed record StorefrontCapabilityResponse(
        bool Supported,
        bool Enabled,
        string? Reason = null);
}
