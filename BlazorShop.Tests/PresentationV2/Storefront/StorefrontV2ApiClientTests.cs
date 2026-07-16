extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using Microsoft.Extensions.Options;
    using Xunit;

    using BlazorShop.Web.SharedV2.Models.Product;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontV2ApiClientTests
    {
        [Fact]
        public async Task GetPublishedCategoriesAsync_ReadsCommerceNodeEnvelope()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/catalog/categories", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"ok","data":[]}""");
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetPublishedCategoriesAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
            Assert.Equal(["/api/storefront/stores/default/catalog/categories"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublishedCategoriesAsync_DoesNotCallLegacyFallback_WhenFallbackDisabled()
        {
            var handler = new RecordingHandler(request =>
            {
                if (request.RequestUri?.AbsolutePath == "/api/storefront/stores/default/catalog/categories")
                {
                    return JsonResponse(HttpStatusCode.ServiceUnavailable, """{"success":false,"message":"down","data":null}""");
                }

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"legacy","data":[{"id":"00000000-0000-0000-0000-000000000001","name":"Legacy","slug":"legacy"}]}""");
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client, enableLegacyFallback: false);

            var result = await apiClient.GetPublishedCategoriesAsync();

            Assert.True(result.IsServiceUnavailable);
            Assert.Equal(["/api/storefront/stores/default/catalog/categories"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublishedCategoriesAsync_CanUseLegacyFallback_WhenExplicitlyEnabled()
        {
            var handler = new RecordingHandler(request =>
            {
                if (request.RequestUri?.AbsolutePath == "/api/storefront/stores/default/catalog/categories")
                {
                    return JsonResponse(HttpStatusCode.ServiceUnavailable, """{"success":false,"message":"down","data":null}""");
                }

                return JsonResponse(HttpStatusCode.OK, """{"success":true,"message":"legacy","data":[]}""");
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client, enableLegacyFallback: true);

            var result = await apiClient.GetPublishedCategoriesAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(["/api/storefront/stores/default/catalog/categories", "/api/public/catalog/categories"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublishedCatalogPageAsync_UsesNamedSortValue()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/catalog/products", request.RequestUri?.AbsolutePath);
                Assert.Contains("sortBy=displayOrder", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.DoesNotContain("sortBy=DisplayOrder", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.DoesNotContain("sortBy=6", request.RequestUri?.Query, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"ok","data":{"items":[],"pageNumber":1,"pageSize":24,"totalCount":0,"totalPages":0}}""");
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetPublishedCatalogPageAsync(new ProductCatalogQuery
            {
                SortBy = ProductCatalogSortBy.DisplayOrder,
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(["/api/storefront/stores/default/catalog/products"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublishedCatalogPageAsync_WhenCurrencyCodeProvided_AddsCurrencyQuery()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/catalog/products", request.RequestUri?.AbsolutePath);
                Assert.Contains("currencyCode=EUR", request.RequestUri?.Query, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"ok","data":{"items":[],"pageNumber":1,"pageSize":24,"totalCount":0,"totalPages":0}}""");
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetPublishedCatalogPageAsync(
                new ProductCatalogQuery { SortBy = ProductCatalogSortBy.DisplayOrder },
                "eur");

            Assert.True(result.IsSuccess);
            Assert.Equal(["/api/storefront/stores/default/catalog/products"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublishedCatalogPageAsync_WhenIncludeSubcategoriesProvided_AddsQueryFlag()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/catalog/products", request.RequestUri?.AbsolutePath);
                Assert.Contains("categorySlug=shoes", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("includeSubcategories=true", request.RequestUri?.Query, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """{"success":true,"message":"ok","data":{"items":[],"pageNumber":1,"pageSize":24,"totalCount":0,"totalPages":0}}""");
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetPublishedCatalogPageAsync(new ProductCatalogQuery
            {
                CategorySlug = "shoes",
                IncludeSubcategories = true,
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(["/api/storefront/stores/default/catalog/products"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublishedProductBySlugAsync_ReadsDeliveryMetadata()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/catalog/products/slug/test-product", request.RequestUri?.AbsolutePath);
                Assert.Contains("currencyCode=USD", request.RequestUri?.Query, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "id": "00000000-0000-0000-0000-000000000001",
                        "slug": "test-product",
                        "name": "Test product",
                        "description": "Test description",
                        "sku": "TEST-1",
                        "price": 12.50,
                        "comparePrice": null,
                        "weight": 1.25,
                        "length": 10.5,
                        "width": 5.25,
                        "height": 2.75,
                        "image": "/images/test.png",
                        "quantity": 5,
                        "purchasable": true,
                        "purchaseBlockReasons": [],
                        "stockStatus": "in_stock",
                        "availableQuantity": 5,
                        "minOrderQuantity": 1,
                        "maxOrderQuantity": null,
                        "quantityStep": 1,
                        "manageStock": true,
                        "shippingRequired": true,
                        "freeShipping": true,
                        "deliveryEstimateText": "Ships in 2 days",
                        "displayOrder": 1,
                        "inStock": true,
                        "productType": "simple",
                        "robotsIndex": true,
                        "robotsFollow": true,
                        "createdOn": "2026-07-16T00:00:00Z",
                        "updatedAt": "2026-07-16T00:00:00Z",
                        "variants": []
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetPublishedProductBySlugAsync("test-product", "USD");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(1.25m, result.Value!.Weight);
            Assert.Equal(10.5m, result.Value.Length);
            Assert.Equal(5.25m, result.Value.Width);
            Assert.Equal(2.75m, result.Value.Height);
            Assert.True(result.Value.ShippingRequired);
            Assert.True(result.Value.FreeShipping);
            Assert.Equal("Ships in 2 days", result.Value.DeliveryEstimateText);
            Assert.Equal(["/api/storefront/stores/default/catalog/products/slug/test-product"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublicConfigurationAsync_ReadsStoreScopedConfiguration()
        {
            var handler = new RecordingHandler(request =>
            {
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
                          "supportedCultures": ["en-US"]
                        },
                        "currencyOptions": {
                          "defaultCurrencyCode": "USD",
                          "supportedCurrencyCodes": ["USD"]
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
                        "paymentMethods": [
                          {
                            "id": "00000000-0000-0000-0000-000000000010",
                            "key": "cod",
                            "name": "Cash on Delivery",
                            "description": "Pay on delivery."
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
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetPublicConfigurationAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("default", result.Value!.StoreIdentity.StoreKey);
            Assert.Equal("USD", result.Value.CurrencyOptions.DefaultCurrencyCode);
            Assert.True(result.Value.FeatureFlags.CheckoutEnabled);
            Assert.Single(result.Value.PaymentMethods);
            Assert.Equal("cod", result.Value.PaymentMethods[0].Key);
            Assert.Equal(["/api/storefront/stores/default/configuration"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPageNavigationLinksAsync_ReadsPublishedContentNavigation()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/pages/navigation", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": [
                        {
                          "pageKey": "privacy_policy",
                          "slug": "privacy",
                          "title": "Privacy policy",
                          "navigationLocation": "footer_legal",
                          "displayOrder": 310
                        }
                      ]
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetPageNavigationLinksAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            var link = Assert.Single(result.Value!);
            Assert.Equal("privacy_policy", link.PageKey);
            Assert.Equal("privacy", link.Slug);
            Assert.Equal("footer_legal", link.NavigationLocation);
            Assert.Equal(["/api/storefront/stores/default/pages/navigation"], handler.RequestPaths);
        }

        [Fact]
        public async Task SetCurrencyPreferenceAsync_PostsStoreScopedCurrencyCommand()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/storefront/stores/default/currency/preference", request.RequestUri?.AbsolutePath);

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
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.SetCurrencyPreferenceAsync(new StorefrontCurrencyPreferenceRequest
            {
                CurrencyCode = "EUR",
            });

            Assert.True(result.Success);
            Assert.Equal("EUR", result.Data?.CurrencyCode);
            Assert.Equal(["/api/storefront/stores/default/currency/preference"], handler.RequestPaths);
        }

        private static StorefrontApiClient CreateApiClient(HttpClient client, bool enableLegacyFallback = false)
        {
            return new StorefrontApiClient(
                client,
                Options.Create(new StorefrontApiOptions
                {
                    EnableLegacyFallback = enableLegacyFallback,
                }));
        }

        private static HttpClient CreateClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/default/"),
            };
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
            private readonly List<string> _requestPaths = [];

            public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            public IReadOnlyList<string> RequestPaths => _requestPaths;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
                Assert.DoesNotContain("/api/internal", requestPath, StringComparison.OrdinalIgnoreCase);
                _requestPaths.Add(requestPath);

                return Task.FromResult(_handler(request));
            }
        }
    }
}
