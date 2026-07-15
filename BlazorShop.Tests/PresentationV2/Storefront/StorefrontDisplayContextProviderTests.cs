extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;

    public sealed class StorefrontDisplayContextProviderTests
    {
        [Fact]
        public async Task GetAsync_WhenCurrentStoreIsUnavailable_ReturnsFallbackContext()
        {
            var provider = new StorefrontDisplayContextProvider(
                new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.NotFound()));

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
            var provider = new StorefrontDisplayContextProvider(
                new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.Succeeded(
                    CreateStore(defaultCulture: cultureName))));

            var context = await provider.GetAsync();

            Assert.Equal(cultureName, context.CultureName);
            Assert.Equal(languageCode, context.LanguageCode);
        }

        [Fact]
        public async Task GetAsync_WhenCultureAndCurrencyAreInvalid_UsesFallbacks()
        {
            var provider = new StorefrontDisplayContextProvider(
                new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.Succeeded(
                    CreateStore(defaultCulture: "invalid-culture", defaultCurrencyCode: "USDO"))));

            var context = await provider.GetAsync();

            Assert.Equal("en-US", context.CultureName);
            Assert.Equal("en", context.LanguageCode);
            Assert.Equal("USD", context.CurrencyCode);
        }

        [Fact]
        public async Task GetAsync_NormalizesStoreBrandingAndContactFields()
        {
            var provider = new StorefrontDisplayContextProvider(
                new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.Succeeded(
                    CreateStore(
                        name: " Demo Store ",
                        defaultCurrencyCode: "eur",
                        logoUrl: " /media/logo.png ",
                        companyName: " Demo Co ",
                        supportEmail: " support@example.test "))));

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
            var apiClient = CreateApiClient(
                """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "storeIdentity": {
                      "publicId": "00000000-0000-0000-0000-000000000001",
                      "storeKey": "default",
                      "name": "Default Store",
                      "status": "active",
                      "baseUrl": "https://store.example",
                      "primaryDomain": "store.example",
                      "forceHttps": true
                    },
                    "branding": {
                      "cdnHost": null,
                      "logoUrl": null,
                      "companyName": null,
                      "companyEmail": null,
                      "companyPhone": null,
                      "companyAddress": null,
                      "faviconUrl": null,
                      "pngIconUrl": null,
                      "appleTouchIconUrl": null,
                      "msTileImageUrl": null,
                      "msTileColor": null,
                      "supportEmail": null,
                      "supportPhone": null,
                      "htmlBodyId": null
                    },
                    "localeOptions": {
                      "defaultCulture": "en-US",
                      "supportedCultures": ["en-US"]
                    },
                    "currencyOptions": {
                      "defaultCurrencyCode": "USD",
                      "supportedCurrencyCodes": ["USD", "EUR"]
                    },
                    "maintenanceState": {
                      "maintenanceModeEnabled": false,
                      "maintenanceMessage": null
                    },
                    "featureFlags": {
                      "customerAccountsEnabled": true,
                      "cartEnabled": true,
                      "checkoutEnabled": true,
                      "paymentsEnabled": true,
                      "newsletterEnabled": true,
                      "recommendationsEnabled": true
                    },
                    "paymentMethods": [],
                    "seoDefaults": {
                      "siteName": null,
                      "defaultTitleSuffix": null,
                      "defaultMetaDescription": null,
                      "defaultOgImage": null,
                      "baseCanonicalUrl": null,
                      "companyName": null,
                      "companyLogoUrl": null,
                      "companyPhone": null,
                      "companyEmail": null,
                      "companyAddress": null,
                      "facebookUrl": null,
                      "instagramUrl": null,
                      "xUrl": null
                    }
                  }
                }
                """);
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

        private static StorefrontApiClient CreateApiClient(string json)
        {
            var handler = new StaticJsonHandler(json);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/default/"),
            };

            return new StorefrontApiClient(
                client,
                Options.Create(new StorefrontApiOptions()));
        }

        private sealed class StaticJsonHandler : HttpMessageHandler
        {
            private readonly string _json;

            public StaticJsonHandler(string json)
            {
                _json = json;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_json, Encoding.UTF8, "application/json"),
                });
            }
        }
    }
}
