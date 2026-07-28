extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Microsoft.AspNetCore.Http;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Presentation.Configuration;
    using StorefrontV2::BlazorShop.Storefront.Presentation.Services;
    using StorefrontV2::BlazorShop.Storefront.Presentation.Contracts;

    public sealed class StorefrontDisplayContextProviderTests
    {
        [Fact]
        public async Task GetAsync_WhenCurrentStoreIsUnavailable_ReturnsFallbackContext()
        {
            var provider = CreateProvider(StorefrontCurrentStoreResolution.NotFound());

            var context = await provider.GetAsync();

            Assert.Equal("default", context.StoreKey);
            Assert.Equal("BlazorShop", context.StoreName);
            Assert.Equal("en-US", context.CultureName);
            Assert.Equal("en", context.LanguageCode);
            Assert.Equal("USD", context.CurrencyCode);
        }

        [Theory]
        [InlineData("vi-VN", "vi")]
        [InlineData("en-US", "en")]
        public async Task GetAsync_DerivesLanguageCodeFromDefaultCulture(string cultureName, string languageCode)
        {
            var provider = CreateProvider(StorefrontCurrentStoreResolution.Succeeded(
                CreateStore(defaultCulture: cultureName)));

            var context = await provider.GetAsync();

            Assert.Equal(cultureName, context.CultureName);
            Assert.Equal(languageCode, context.LanguageCode);
        }

        [Fact]
        public async Task GetAsync_WhenCultureAndCurrencyAreInvalid_UsesFallbacks()
        {
            var provider = CreateProvider(StorefrontCurrentStoreResolution.Succeeded(
                CreateStore(defaultCulture: "invalid-culture", defaultCurrencyCode: "USDO")));

            var context = await provider.GetAsync();

            Assert.Equal("en-US", context.CultureName);
            Assert.Equal("en", context.LanguageCode);
            Assert.Equal("USD", context.CurrencyCode);
        }

        [Fact]
        public async Task GetAsync_NormalizesStoreBrandingAndContactFields()
        {
            var provider = CreateProvider(StorefrontCurrentStoreResolution.Succeeded(
                CreateStore(
                    name: " Demo Store ",
                    defaultCurrencyCode: "eur",
                    logoUrl: " /media/logo.png ",
                    companyName: " Demo Co ",
                    supportEmail: " support@example.test ")));

            var context = await provider.GetAsync();

            Assert.Equal("Demo Store", context.StoreName);
            Assert.Equal("EUR", context.CurrencyCode);
            Assert.Equal("/media/logo.png", context.LogoUrl);
            Assert.Equal("Demo Co", context.CompanyName);
            Assert.Equal("support@example.test", context.SupportEmail);
        }

        [Fact]
        public async Task GetAsync_WhenCurrencyCookieIsSupported_UsesWorkingCurrency()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Cookie = $"{StorefrontCookieNames.CurrencyPreference}=eur";
            var apiClient = new StubStoreConfigurationClient(CreatePublicConfiguration(["USD", "EUR"]));
            var provider = new StorefrontDisplayContextProvider(
                new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.Succeeded(CreateStore())),
                apiClient,
                new HttpContextAccessor { HttpContext = httpContext });

            var context = await provider.GetAsync();

            Assert.Equal("USD", context.DefaultCurrencyCode);
            Assert.Equal("EUR", context.CurrencyCode);
            Assert.Equal(["USD", "EUR"], context.SupportedCurrencyCodes);
        }

        private static StorefrontCurrentStore CreateStore(
            string storeKey = "default",
            string name = "Default Store",
            string defaultCurrencyCode = "USD",
            string defaultCulture = "en-US",
            string? logoUrl = null,
            string? companyName = null,
            string? supportEmail = null)
        {
            return new StorefrontCurrentStore(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                storeKey,
                name,
                "active",
                BaseUrl: "https://store.example/",
                PrimaryDomain: "store.example",
                ForceHttps: true,
                CdnHost: null,
                LogoUrl: logoUrl,
                CompanyName: companyName,
                CompanyEmail: null,
                CompanyPhone: null,
                CompanyAddress: null,
                FaviconUrl: null,
                PngIconUrl: null,
                AppleTouchIconUrl: null,
                MsTileImageUrl: null,
                MsTileColor: null,
                DefaultCurrencyCode: defaultCurrencyCode,
                DefaultCulture: defaultCulture,
                SupportEmail: supportEmail,
                SupportPhone: null,
                MaintenanceModeEnabled: false,
                MaintenanceMessage: null,
                HtmlBodyId: null);
        }

        private static StorefrontDisplayContextProvider CreateProvider(
            StorefrontCurrentStoreResolution resolution,
            IStorefrontStoreConfigurationClient? apiClient = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            return new StorefrontDisplayContextProvider(
                new StubCurrentStoreProvider(resolution),
                apiClient ?? new StubStoreConfigurationClient(),
                httpContextAccessor ?? new HttpContextAccessor());
        }

        private sealed class StubCurrentStoreProvider : IStorefrontCurrentStoreProvider
        {
            private readonly StorefrontCurrentStoreResolution _resolution;

            public StubCurrentStoreProvider(StorefrontCurrentStoreResolution resolution)
            {
                _resolution = resolution;
            }

            public Task<StorefrontCurrentStoreResolution> ResolveAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_resolution);
            }
        }

        private sealed class StubStoreConfigurationClient : IStorefrontStoreConfigurationClient
        {
            private readonly StorefrontPublicConfiguration? publicConfiguration;

            public StubStoreConfigurationClient(StorefrontPublicConfiguration? publicConfiguration = null)
            {
                this.publicConfiguration = publicConfiguration;
            }

            public Task<StorefrontApiResult<StorefrontCurrentStore>> GetCurrentStoreAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(StorefrontApiResult<StorefrontCurrentStore>.ServiceUnavailable());
            }

            public Task<StorefrontApiResult<StorefrontPublicConfiguration>> GetPublicConfigurationAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.publicConfiguration is null
                    ? StorefrontApiResult<StorefrontPublicConfiguration>.ServiceUnavailable()
                    : StorefrontApiResult<StorefrontPublicConfiguration>.Success(this.publicConfiguration));
            }

            public Task<StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>> SetCurrencyPreferenceAsync(
                StorefrontCurrencyPreferenceRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(StorefrontSubmitResult<StorefrontCurrencyPreferenceResponse>.Failed("Configuration unavailable."));
            }
        }

        private static StorefrontPublicConfiguration CreatePublicConfiguration(IReadOnlyList<string> supportedCurrencyCodes)
        {
            return new StorefrontPublicConfiguration(
                new StorefrontStoreIdentity(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "default",
                    "Default Store",
                    "active",
                    "https://store.example",
                    "store.example",
                    ForceHttps: true),
                new StorefrontBranding(
                    CdnHost: null,
                    LogoUrl: null,
                    CompanyName: null,
                    CompanyEmail: null,
                    CompanyPhone: null,
                    CompanyAddress: null,
                    FaviconUrl: null,
                    PngIconUrl: null,
                    AppleTouchIconUrl: null,
                    MsTileImageUrl: null,
                    MsTileColor: null,
                    SupportEmail: null,
                    SupportPhone: null,
                    HtmlBodyId: null),
                new StorefrontLocaleOptions("en-US", ["en-US"]),
                new StorefrontCurrencyOptions("USD", supportedCurrencyCodes),
                new StorefrontConsentConfiguration(
                    Enabled: false,
                    BannerRequired: false,
                    CurrentVersion: "v1",
                    PolicyPagePath: "/privacy",
                    Categories: [],
                    VisitorCookieLifetimeDays: 365),
                new StorefrontCaptchaConfiguration(
                    Enabled: false,
                    ProviderSystemName: string.Empty,
                    PublicSiteKey: null,
                    EnabledTargets: [],
                    ActionNames: new Dictionary<string, string>()),
                new StorefrontMaintenanceState(false, null),
                new StorefrontFeatureFlags(
                    CustomerAccountsEnabled: true,
                    CartEnabled: true,
                    CheckoutEnabled: true,
                    PaymentsEnabled: true,
                    NewsletterEnabled: true,
                    RecommendationsEnabled: true),
                new Dictionary<string, StorefrontCapability>(),
                [],
                new StorefrontSeoDefaults(
                    SiteName: null,
                    DefaultTitleSuffix: null,
                    DefaultMetaDescription: null,
                    DefaultOgImage: null,
                    BaseCanonicalUrl: null,
                    CompanyName: null,
                    CompanyLogoUrl: null,
                    CompanyPhone: null,
                    CompanyEmail: null,
                    CompanyAddress: null,
                    FacebookUrl: null,
                    InstagramUrl: null,
                    XUrl: null));
        }
    }
}
