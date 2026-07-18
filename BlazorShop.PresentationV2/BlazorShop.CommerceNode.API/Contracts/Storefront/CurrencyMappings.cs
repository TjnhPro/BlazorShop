namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Domain.Contracts;
    public static partial class StorefrontContractMappings
    {
        public static StorefrontCurrencyPreferenceResponse ToStorefrontContract(
            this StorefrontWorkingCurrencyResolution resolution)
        {
            return new StorefrontCurrencyPreferenceResponse(
                resolution.CurrencyCode,
                resolution.BaseCurrencyCode,
                resolution.RequestedCurrencyCode,
                resolution.RequestedCurrencySupported,
                resolution.CheckoutCurrencyEnabled,
                resolution.Reason);
        }
        public static StorefrontPublicConfigurationResponse ToPublicConfigurationContract(
            this CommerceCurrentStore store,
            IReadOnlyList<StorefrontPaymentMethodResponse> paymentMethods,
            SeoSettingsDto seoDefaults,
            StoreFeatureStateSnapshot featureStates,
            StorefrontConsentOptions consentOptions,
            CaptchaOptions captchaOptions,
            IReadOnlyList<string>? supportedCurrencyCodes = null)
        {
            var currencyCodes = NormalizeSupportedCurrencyCodes(store.DefaultCurrencyCode, supportedCurrencyCodes);
            return new StorefrontPublicConfigurationResponse(
                new StorefrontStoreIdentityResponse(
                    store.PublicId,
                    store.StoreKey,
                    store.Name,
                    store.Status,
                    store.BaseUrl,
                    store.PrimaryDomain,
                    store.ForceHttps),
                new StorefrontBrandingResponse(
                    store.CdnHost,
                    store.LogoUrl,
                    store.CompanyName,
                    store.CompanyEmail,
                    store.CompanyPhone,
                    store.CompanyAddress,
                    store.FaviconUrl,
                    store.PngIconUrl,
                    store.AppleTouchIconUrl,
                    store.MsTileImageUrl,
                    store.MsTileColor,
                    store.SupportEmail,
                    store.SupportPhone,
                    store.HtmlBodyId),
                new StorefrontLocaleOptionsResponse(
                    store.DefaultCulture,
                    [store.DefaultCulture]),
                new StorefrontCurrencyOptionsResponse(
                    store.DefaultCurrencyCode,
                    currencyCodes),
                ToStorefrontContract(consentOptions),
                ToStorefrontContract(captchaOptions),
                new StorefrontMaintenanceStateResponse(
                    store.MaintenanceModeEnabled,
                    store.MaintenanceMessage),
                new StorefrontFeatureFlagsResponse(
                    CustomerAccountsEnabled: featureStates.CustomerAccountsEnabled,
                    CartEnabled: true,
                    CheckoutEnabled: featureStates.CheckoutEnabled,
                    PaymentsEnabled: true,
                    NewsletterEnabled: featureStates.NewsletterEnabled,
                    RecommendationsEnabled: featureStates.RecommendationsEnabled),
                paymentMethods,
                new StorefrontSeoDefaultsResponse(
                    seoDefaults.SiteName,
                    seoDefaults.DefaultTitleSuffix,
                    seoDefaults.DefaultMetaDescription,
                    seoDefaults.DefaultOgImage,
                    seoDefaults.BaseCanonicalUrl,
                    seoDefaults.CompanyName,
                    seoDefaults.CompanyLogoUrl,
                    seoDefaults.CompanyPhone,
                    seoDefaults.CompanyEmail,
                    seoDefaults.CompanyAddress,
                    seoDefaults.FacebookUrl,
                    seoDefaults.InstagramUrl,
                    seoDefaults.XUrl));
        }
        private static IReadOnlyList<string> NormalizeSupportedCurrencyCodes(
            string defaultCurrencyCode,
            IReadOnlyList<string>? supportedCurrencyCodes)
        {
            var baseCurrencyCode = NormalizeCurrencyCode(defaultCurrencyCode) ?? "USD";
            return (supportedCurrencyCodes ?? [])
                .Prepend(baseCurrencyCode)
                .Select(code => NormalizeCurrencyCode(code) ?? baseCurrencyCode)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } ? normalized : null;
        }
    }
}
