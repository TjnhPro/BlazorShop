extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using BlazorShop.Storefront.Client;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    using Xunit;

    using GeneratedStorefrontConfigurationClient = StorefrontV2::BlazorShop.Storefront.Services.GeneratedStorefrontConfigurationClient;
    using StorefrontApiOptions = StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions;
    using StorefrontCurrencyPreferenceRequest = StorefrontV2::BlazorShop.Storefront.Services.StorefrontCurrencyPreferenceRequest;

    public sealed class StorefrontGeneratedConfigurationClientTests
    {
        [Fact]
        public async Task GetCurrentStoreAsync_UsesGeneratedStoreClientAndMapsStoreBootstrapFields()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/store/current", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "publicId": "00000000-0000-0000-0000-000000000001",
                        "storeKey": "default",
                        "name": "Default Store",
                        "status": "active",
                        "baseUrl": "https://store.example",
                        "primaryDomain": "store.example",
                        "forceHttps": true,
                        "cdnHost": "cdn.example",
                        "logoUrl": "/logo.png",
                        "companyName": "Example Co",
                        "companyEmail": "support@example.com",
                        "companyPhone": "555-0100",
                        "companyAddress": "1 Commerce Way",
                        "faviconUrl": "/favicon.ico",
                        "pngIconUrl": "/icon.png",
                        "appleTouchIconUrl": "/apple-touch-icon.png",
                        "msTileImageUrl": "/mstile.png",
                        "msTileColor": "#ffffff",
                        "defaultCurrencyCode": "GBP",
                        "defaultCulture": "en-GB",
                        "supportEmail": "help@example.com",
                        "supportPhone": "555-0101",
                        "maintenanceModeEnabled": true,
                        "maintenanceMessage": "Back soon.",
                        "htmlBodyId": "default-store"
                      }
                    }
                    """);
            });
            var client = CreateClient(handler);

            var result = await client.GetCurrentStoreAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("default", result.Value!.StoreKey);
            Assert.Equal("GBP", result.Value.DefaultCurrencyCode);
            Assert.Equal("en-GB", result.Value.DefaultCulture);
            Assert.True(result.Value.MaintenanceModeEnabled);
            Assert.Equal("Back soon.", result.Value.MaintenanceMessage);
            Assert.Equal(["/api/storefront/stores/default/store/current"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublicConfigurationAsync_UsesGeneratedConfigurationClientAndMapsCapabilities()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/configuration", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
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
                          "cdnHost": "cdn.example",
                          "logoUrl": "/logo.png",
                          "companyName": "Example Co",
                          "companyEmail": "support@example.com",
                          "companyPhone": "555-0100",
                          "companyAddress": "1 Commerce Way",
                          "faviconUrl": "/favicon.ico",
                          "pngIconUrl": "/icon.png",
                          "appleTouchIconUrl": "/apple-touch-icon.png",
                          "msTileImageUrl": "/mstile.png",
                          "msTileColor": "#ffffff",
                          "supportEmail": "help@example.com",
                          "supportPhone": "555-0101",
                          "htmlBodyId": "default-store"
                        },
                        "localeOptions": {
                          "defaultCulture": "en-US",
                          "supportedCultures": ["en-US", "fr-FR"]
                        },
                        "currencyOptions": {
                          "defaultCurrencyCode": "USD",
                          "supportedCurrencyCodes": ["USD", "EUR"]
                        },
                        "consent": {
                          "enabled": true,
                          "bannerRequired": true,
                          "currentVersion": "v1",
                          "policyPagePath": "/privacy",
                          "visitorCookieLifetimeDays": 180,
                          "categories": [
                            { "name": "analytics", "required": false, "defaultEnabled": false }
                          ]
                        },
                        "captcha": {
                          "enabled": true,
                          "providerSystemName": "turnstile",
                          "publicSiteKey": "public-key",
                          "enabledTargets": ["contact"],
                          "actionNames": { "contact": "storefront_contact" }
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
                          "newsletterEnabled": false,
                          "recommendationsEnabled": false
                        },
                        "features": {
                          "checkout": { "supported": true, "enabled": true },
                          "wishlist": { "supported": false, "enabled": false, "reason": "not_installed" }
                        },
                        "paymentMethods": [
                          {
                            "id": "00000000-0000-0000-0000-000000000010",
                            "key": "cod",
                            "name": "Cash on Delivery",
                            "description": "Pay on delivery.",
                            "shortDisplayText": "Pay on delivery",
                            "iconUrl": "/cod.svg",
                            "supportedCurrencyCodes": ["USD"],
                            "supportedCountryCodes": ["US"]
                          }
                        ],
                        "seoDefaults": {
                          "siteName": "Default Store",
                          "defaultTitleSuffix": "| Default Store",
                          "defaultMetaDescription": "Default description.",
                          "defaultOgImage": null,
                          "baseCanonicalUrl": "https://store.example",
                          "companyName": "Example Co",
                          "companyLogoUrl": "/logo.png",
                          "companyPhone": "555-0100",
                          "companyEmail": "support@example.com",
                          "companyAddress": "1 Commerce Way",
                          "facebookUrl": null,
                          "instagramUrl": null,
                          "xUrl": null
                        }
                      }
                    }
                    """);
            });
            var client = CreateClient(handler);

            var result = await client.GetPublicConfigurationAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("default", result.Value!.StoreIdentity.StoreKey);
            Assert.Equal(["USD", "EUR"], result.Value.CurrencyOptions.SupportedCurrencyCodes);
            Assert.True(result.Value.FeatureFlags.CheckoutEnabled);
            Assert.True(result.Value.Features["checkout"].Enabled);
            Assert.False(result.Value.Features["wishlist"].Supported);
            Assert.Equal("not_installed", result.Value.Features["wishlist"].Reason);
            Assert.Equal("cod", Assert.Single(result.Value.PaymentMethods).Key);
            Assert.Equal("Default Store", result.Value.SeoDefaults.SiteName);
            Assert.Equal(["/api/storefront/stores/default/configuration"], handler.RequestPaths);
        }

        [Fact]
        public void ConsentAndCapabilityContracts_DoNotUseBackendDtos()
        {
            var source = string.Join(
                Environment.NewLine,
                new[]
                {
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/ConsentContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/IStorefrontConsentClient.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Consent.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontConsentEndpoints.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/Contracts/ConfigurationContracts.cs",
                    "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/GeneratedStorefrontConfigurationClient.cs",
                }.Select(ReadRepositoryFile));

            Assert.Contains("ResolveConsentVisitorKey(httpContext, createIfMissing: true)", source, StringComparison.Ordinal);
            Assert.Contains("NewsletterEnabled", source, StringComparison.Ordinal);
            Assert.Contains("RecommendationsEnabled", source, StringComparison.Ordinal);
            Assert.Contains("StorefrontCaptchaConfiguration", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Application.DTOs", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Application.CommerceNode", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models", source, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SetCurrencyPreferenceAsync_UsesGeneratedCurrencyClient()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/storefront/stores/default/currency/preference", request.RequestUri?.AbsolutePath);
                var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
                Assert.Contains("\"currencyCode\":\"EUR\"", body, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "currencyCode": "EUR",
                        "baseCurrencyCode": "USD",
                        "requestedCurrencyCode": "EUR",
                        "requestedCurrencySupported": true,
                        "checkoutCurrencyEnabled": true,
                        "reason": "supported"
                      }
                    }
                    """);
            });
            var client = CreateClient(handler);

            var result = await client.SetCurrencyPreferenceAsync(new StorefrontCurrencyPreferenceRequest
            {
                CurrencyCode = "EUR",
            });

            Assert.True(result.Success);
            Assert.Equal("EUR", result.Data?.CurrencyCode);
            Assert.True(result.Data?.RequestedCurrencySupported);
            Assert.Equal(["/api/storefront/stores/default/currency/preference"], handler.RequestPaths);
        }

        [Fact]
        public void StorefrontDi_UsesGeneratedConfigurationAdapterForStoreBootstrap()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");

            Assert.Contains("GeneratedStorefrontConfigurationClient", source, StringComparison.Ordinal);
            Assert.Contains("ResolveCommerceNodeBaseAddress", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontStoreConfigurationClient>", source, StringComparison.Ordinal);
            Assert.Contains("GetRequiredService<GeneratedStorefrontConfigurationClient>", source, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IStorefrontStoreConfigurationClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>())", source, StringComparison.Ordinal);
        }

        private static GeneratedStorefrontConfigurationClient CreateClient(RecordingHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce.example/"),
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Api:StoreKey"] = "default",
                })
                .Build();

            return new GeneratedStorefrontConfigurationClient(
                new StorefrontStoreClient(string.Empty, httpClient),
                new StorefrontConfigurationClient(string.Empty, httpClient),
                new StorefrontCurrencyClient(string.Empty, httpClient),
                configuration,
                Options.Create(new StorefrontApiOptions()));
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate BlazorShop.sln.");
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

            public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            {
                _responseFactory = responseFactory;
            }

            public List<string> RequestPaths { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
                return Task.FromResult(_responseFactory(request));
            }
        }
    }
}
