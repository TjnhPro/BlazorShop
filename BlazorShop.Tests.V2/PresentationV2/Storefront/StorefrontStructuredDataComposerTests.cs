extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Text.Json.Nodes;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Models;
    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontStructuredDataComposerTests
    {
        [Fact]
        public async Task ComposeHomePageAsync_PrefersRuntimeStoreProfileForOrganization()
        {
            var composer = CreateComposer(CreateStore(
                name: "Runtime Store",
                logoUrl: "/media/store-logo.png",
                companyName: "Runtime Co",
                companyEmail: "store@example.test",
                companyPhone: "+1 555 0100",
                companyAddress: "12 Market Street"));

            var result = await composer.ComposeHomePageAsync();

            var organization = GetNode(result, "Organization");
            var website = GetNode(result, "WebSite");

            Assert.Equal("Runtime Co", organization["name"]?.GetValue<string>());
            Assert.Equal("https://store.example/media/store-logo.png", organization["logo"]?.GetValue<string>());
            Assert.Equal("store@example.test", organization["email"]?.GetValue<string>());
            Assert.Equal("+1 555 0100", organization["telephone"]?.GetValue<string>());
            Assert.Equal("12 Market Street", organization["address"]?.GetValue<string>());
            Assert.Equal("Runtime Store", website["name"]?.GetValue<string>());
        }

        [Fact]
        public async Task ComposeHomePageAsync_OmitsAddressWhenStoreAndSeoAddressAreEmpty()
        {
            var composer = CreateComposer(CreateStore(companyAddress: null), settingsAddress: null);

            var result = await composer.ComposeHomePageAsync();
            var organization = GetNode(result, "Organization");

            Assert.False(organization.ContainsKey("address"));
        }

        [Fact]
        public async Task ComposeProductPageAsync_AddsSafeProductIdentifiers()
        {
            var composer = CreateComposer(CreateStore());

            var result = await composer.ComposeProductPageAsync(new GetProduct
            {
                Name = "Structured Product",
                Slug = "structured-product",
                Description = "Structured product description",
                Price = 19.99m,
                Sku = "SKU-1",
                Gtin = "0123456789012",
                ManufacturerPartNumber = "MPN-1",
                Condition = "refurbished",
            });

            var product = GetNode(result, "Product");

            Assert.Equal("SKU-1", product["sku"]?.GetValue<string>());
            Assert.Equal("0123456789012", product["gtin"]?.GetValue<string>());
            Assert.Equal("MPN-1", product["mpn"]?.GetValue<string>());
            Assert.Equal("https://schema.org/RefurbishedCondition", product["itemCondition"]?.GetValue<string>());
            Assert.False(product.ContainsKey("weight"));
            Assert.False(product.ContainsKey("height"));
        }

        private static IStorefrontStructuredDataComposer CreateComposer(
            StorefrontCurrentStore store,
            string? settingsAddress = "SEO Address")
        {
            return new StorefrontStructuredDataComposer(
                new StubPublicUrlResolver("https://store.example/"),
                new StubSeoSettingsProvider(new SeoSettingsDto
                {
                    SiteName = "SEO Site",
                    CompanyName = "SEO Co",
                    CompanyLogoUrl = "/seo-logo.png",
                    CompanyEmail = "seo@example.test",
                    CompanyPhone = "+1 555 9999",
                    CompanyAddress = settingsAddress,
                    BaseCanonicalUrl = "https://store.example",
                }),
                new StubCurrentStoreProvider(StorefrontCurrentStoreResolution.Succeeded(store)));
        }

        private static StorefrontCurrentStore CreateStore(
            string name = "Runtime Store",
            string? logoUrl = null,
            string? companyName = null,
            string? companyEmail = null,
            string? companyPhone = null,
            string? companyAddress = null)
        {
            return new StorefrontCurrentStore(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "demo",
                name,
                "active",
                BaseUrl: "https://store.example/",
                PrimaryDomain: "store.example",
                ForceHttps: true,
                CdnHost: null,
                LogoUrl: logoUrl,
                CompanyName: companyName,
                CompanyEmail: companyEmail,
                CompanyPhone: companyPhone,
                CompanyAddress: companyAddress,
                FaviconUrl: null,
                PngIconUrl: null,
                AppleTouchIconUrl: null,
                MsTileImageUrl: null,
                MsTileColor: null,
                DefaultCurrencyCode: "USD",
                DefaultCulture: "en-US",
                SupportEmail: null,
                SupportPhone: null,
                MaintenanceModeEnabled: false,
                MaintenanceMessage: null,
                HtmlBodyId: null);
        }

        private static JsonObject GetNode(StorefrontStructuredDataDocument document, string schemaType)
        {
            var graph = Assert.IsType<JsonArray>(document.Payload["@graph"]);

            return Assert.IsType<JsonObject>(graph.Single(node => string.Equals(node?["@type"]?.GetValue<string>(), schemaType, StringComparison.Ordinal)));
        }

        private sealed class StubSeoSettingsProvider : IStorefrontSeoSettingsProvider
        {
            private readonly SeoSettingsDto _settings;

            public StubSeoSettingsProvider(SeoSettingsDto settings)
            {
                _settings = settings;
            }

            public Task<SeoSettingsDto?> GetAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<SeoSettingsDto?>(_settings);
            }
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

        private sealed class StubPublicUrlResolver : IStorefrontPublicUrlResolver
        {
            private readonly string _baseUrl;

            public StubPublicUrlResolver(string baseUrl)
            {
                _baseUrl = baseUrl;
            }

            public string? ResolveBaseUrl(string? configuredBaseUrl = null)
            {
                return string.IsNullOrWhiteSpace(configuredBaseUrl) ? _baseUrl : configuredBaseUrl;
            }

            public string? ResolveAbsoluteUrl(string? relativeOrAbsoluteUrl, string? configuredBaseUrl = null)
            {
                if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
                {
                    return null;
                }

                if (Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out var absoluteUri))
                {
                    return absoluteUri.ToString();
                }

                return new Uri(new Uri(ResolveBaseUrl(configuredBaseUrl)!, UriKind.Absolute), relativeOrAbsoluteUrl).ToString();
            }
        }
    }
}
