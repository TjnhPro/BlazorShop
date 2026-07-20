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
        public async Task GetProductFilterMetadataAsync_ReadsFilterMetadataContract()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/catalog/product-filter-metadata", request.RequestUri?.AbsolutePath);
                Assert.Contains("categorySlug=shoes", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("searchTerm=runner", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("currencyCode=EUR", request.RequestUri?.Query, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "pageSizes": [12, 24, 48],
                        "sortOptions": [
                          { "value": "displayOrder", "label": "Featured", "displayOrder": 10 }
                        ],
                        "facets": [
                          {
                            "key": "category",
                            "label": "Category",
                            "type": "choice",
                            "displayOrder": 10,
                            "maxChoices": 50,
                            "minimumHitCount": 0,
                            "choices": [
                              { "value": "shoes", "label": "Shoes", "displayOrder": 1, "hitCount": null, "selected": true }
                            ]
                          }
                        ],
                        "priceRange": { "minPrice": 10.50, "maxPrice": 25.00, "currencyCode": "EUR", "displayOrder": 30 },
                        "minimumSearchTermLength": 2
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetProductFilterMetadataAsync("shoes", "runner", "eur");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal([12, 24, 48], result.Value!.PageSizes);
            Assert.Equal("displayOrder", result.Value.SortOptions[0].Value);
            Assert.Equal("category", result.Value.Facets[0].Key);
            Assert.True(result.Value.Facets[0].Choices[0].Selected);
            Assert.Equal(10.50m, result.Value.PriceRange.MinPrice);
            Assert.Equal("EUR", result.Value.PriceRange.CurrencyCode);
            Assert.Equal(2, result.Value.MinimumSearchTermLength);
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_ReadsSuggestionContract()
        {
            var productId = Guid.Parse("00000000-0000-0000-0000-000000000123");
            var mediaId = Guid.Parse("00000000-0000-0000-0000-000000000456");
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/catalog/search-suggestions", request.RequestUri?.AbsolutePath);
                Assert.Contains("searchTerm=runner", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("categorySlug=shoes", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("limit=6", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("currencyCode=EUR", request.RequestUri?.Query, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    $$"""
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "searchTerm": "runner",
                        "minimumSearchTermLength": 2,
                        "limit": 6,
                        "items": [
                          {
                            "id": "{{productId}}",
                            "slug": "runner-shoe",
                            "name": "Runner Shoe",
                            "sku": "RUN-1",
                            "image": "/media/products/test",
                            "primaryMediaPublicId": "{{mediaId}}",
                            "hasPrimaryMedia": true,
                            "price": 12.50,
                            "displayPrice": 10.00,
                            "displayCurrencyCode": "EUR",
                            "categoryName": "Shoes",
                            "categorySlug": "shoes",
                            "inStock": true,
                            "url": "/product/runner-shoe"
                          }
                        ]
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetSearchSuggestionsAsync("runner", "shoes", 6, "eur");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("runner", result.Value!.SearchTerm);
            Assert.Equal(6, result.Value.Limit);
            Assert.Single(result.Value.Items);
            Assert.Equal(productId, result.Value.Items[0].Id);
            Assert.Equal(mediaId, result.Value.Items[0].PrimaryMediaPublicId);
            Assert.Equal("/product/runner-shoe", result.Value.Items[0].Url);
            Assert.Equal(10.00m, result.Value.Items[0].DisplayPrice);
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
                        "mediaGallery": [
                          {
                            "publicId": "00000000-0000-0000-0000-000000000010",
                            "imageUrl": "/media/products/00000000-0000-0000-0000-000000000010?w=1000&h=1000&fit=contain&format=webp&v=2",
                            "thumbnailUrl": "/media/products/00000000-0000-0000-0000-000000000010?w=600&h=600&fit=contain&format=webp&v=2",
                            "fullSizeUrl": "/media/products/00000000-0000-0000-0000-000000000010?w=1000&h=1000&fit=contain&format=webp&v=2",
                            "altText": "Primary product angle",
                            "sortOrder": 0,
                            "isPrimary": true,
                            "width": 1200,
                            "height": 900,
                            "version": 2
                          }
                        ],
                        "variationTemplate": {
                          "name": "Size",
                          "slug": "size",
                          "options": [
                            {
                              "name": "Size",
                              "controlType": "radio",
                              "isRequired": true,
                              "values": [
                                { "value": "M", "colorHex": null }
                              ]
                            }
                          ]
                        },
                        "robotsIndex": true,
                        "robotsFollow": true,
                        "createdOn": "2026-07-16T00:00:00Z",
                        "updatedAt": "2026-07-16T00:00:00Z",
                        "variants": [
                          {
                            "id": "00000000-0000-0000-0000-000000000002",
                            "productId": "00000000-0000-0000-0000-000000000001",
                            "sku": "TEST-1-M",
                            "attributes": [
                              { "name": "Size", "value": "M" }
                            ],
                            "attributeSignature": "size:m",
                            "displayName": "Medium",
                            "sizeScale": 1,
                            "sizeValue": "M",
                            "price": 13.50,
                            "effectivePrice": 13.50,
                            "stock": 3,
                            "color": null,
                            "isActive": true,
                            "isDefault": true
                          }
                        ]
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
            Assert.Equal("/images/test.png", result.Value.Image);
            Assert.True(result.Value.ShippingRequired);
            Assert.True(result.Value.FreeShipping);
            Assert.Equal("Ships in 2 days", result.Value.DeliveryEstimateText);
            var galleryImage = Assert.Single(result.Value.MediaGallery);
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000010"), galleryImage.PublicId);
            Assert.True(galleryImage.IsPrimary);
            Assert.Equal("Primary product angle", galleryImage.AltText);
            Assert.Contains("/media/products/", galleryImage.ImageUrl, StringComparison.Ordinal);
            Assert.Equal(2, galleryImage.Version);
            var variant = Assert.Single(result.Value.Variants);
            Assert.Equal("TEST-1-M", variant.Sku);
            Assert.Equal("Medium", variant.DisplayName);
            Assert.True(variant.IsDefault);
            var attribute = Assert.Single(variant.Attributes);
            Assert.Equal("Size", attribute.Name);
            Assert.Equal("M", attribute.Value);
            Assert.NotNull(result.Value.VariationTemplate);
            Assert.Equal("Size", result.Value.VariationTemplate!.Name);
            var option = Assert.Single(result.Value.VariationTemplate.Options);
            Assert.Equal("radio", option.ControlType);
            Assert.True(option.IsRequired);
            var value = Assert.Single(option.Values);
            Assert.Equal("M", value.Value);
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

        [Fact]
        public async Task RecalculateCartAsync_PostsStoreScopedCartCommand()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/storefront/stores/default/cart/recalculate", request.RequestUri?.AbsolutePath);
                Assert.True(request.Headers.TryGetValues("X-Cart-Token", out var values));
                Assert.Equal("cart-token", Assert.Single(values));

                var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
                Assert.Contains("\"expectedVersion\":3", body, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "cartId": "00000000-0000-0000-0000-000000000001",
                        "state": "active",
                        "version": 4,
                        "lastActivityAtUtc": "2026-07-16T00:00:00Z",
                        "expiresAtUtc": "2026-07-17T00:00:00Z",
                        "lines": []
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.RecalculateCartAsync(
                "cart-token",
                new StorefrontCartRecalculateRequest { ExpectedVersion = 3 });

            Assert.True(result.Success);
            Assert.Equal(4, result.Data?.Version);
            Assert.Equal(["/api/storefront/stores/default/cart/recalculate"], handler.RequestPaths);
        }

        [Fact]
        public async Task MergeCurrentCustomerCartAsync_PostsBearerProtectedCartCommandWithoutBody()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/storefront/stores/default/cart/merge-current-customer", request.RequestUri?.AbsolutePath);
                Assert.True(request.Headers.TryGetValues("X-Cart-Token", out var values));
                Assert.Equal("cart-token", Assert.Single(values));
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);
                Assert.Null(request.Content);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "cartId": "00000000-0000-0000-0000-000000000001",
                        "state": "active",
                        "version": 3,
                        "lastActivityAtUtc": "2026-07-16T00:00:00Z",
                        "expiresAtUtc": "2026-07-17T00:00:00Z",
                        "lines": []
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.MergeCurrentCustomerCartAsync("cart-token", "access-token");

            Assert.True(result.Success);
            Assert.Equal(3, result.Data?.Version);
            Assert.Equal(["/api/storefront/stores/default/cart/merge-current-customer"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetAddressCountriesAsync_ReadsStoreScopedLookup()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/address/countries", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": [
                        { "code": "US", "name": "United States", "postalCodeRequired": true, "stateProvinceRequired": true }
                      ]
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetAddressCountriesAsync();

            Assert.True(result.IsSuccess);
            var country = Assert.Single(result.Value!);
            Assert.Equal("US", country.Code);
            Assert.True(country.StateProvinceRequired);
            Assert.Equal(["/api/storefront/stores/default/address/countries"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetAddressStatesAsync_NormalizesCountryAndReadsStoreScopedLookup()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/address/countries/US/states", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": [
                        { "code": "NY", "name": "New York" }
                      ]
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetAddressStatesAsync(" us ");

            Assert.True(result.IsSuccess);
            var state = Assert.Single(result.Value!);
            Assert.Equal("NY", state.Code);
            Assert.Equal(["/api/storefront/stores/default/address/countries/US/states"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetAddressConfigurationAsync_ReadsFieldConfiguration()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/address/configuration", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "companyEnabled": true,
                        "phoneEnabled": true,
                        "phoneRequired": false,
                        "postalCodeRequired": true,
                        "billingAddressEnabled": true,
                        "useShippingAddressAsBillingDefault": true,
                        "firstNameMaxLength": 120,
                        "lastNameMaxLength": 120,
                        "companyMaxLength": 160,
                        "addressLineMaxLength": 240,
                        "cityMaxLength": 120,
                        "postalCodeMaxLength": 32,
                        "stateProvinceCodeMaxLength": 64,
                        "stateProvinceNameMaxLength": 120,
                        "phoneMaxLength": 32,
                        "emailMaxLength": 256,
                        "stateProvinceRequiredCountryCodes": ["US"]
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetAddressConfigurationAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.True(result.Value!.BillingAddressEnabled);
            Assert.Equal(["US"], result.Value.StateProvinceRequiredCountryCodes);
        }

        [Fact]
        public async Task GetCustomerAddressesAsync_SendsBearerAndReadsAddressBook()
        {
            var addressId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/customer/addresses", request.RequestUri?.AbsolutePath);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

                return JsonResponse(
                    HttpStatusCode.OK,
                    $$"""
                    {
                      "success": true,
                      "message": "ok",
                      "data": [
                        {
                          "publicId": "{{addressId}}",
                          "firstName": "Customer",
                          "lastName": "One",
                          "company": null,
                          "address1": "1 Test Street",
                          "address2": null,
                          "city": "New York",
                          "postalCode": "10000",
                          "countryCode": "US",
                          "stateProvinceCode": "NY",
                          "stateProvinceName": "New York",
                          "phone": "5550100",
                          "email": "customer@example.test",
                          "isDefaultShipping": true,
                          "isDefaultBilling": true,
                          "createdAtUtc": "2026-07-17T00:00:00Z",
                          "updatedAtUtc": "2026-07-17T00:00:00Z"
                        }
                      ]
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetCustomerAddressesAsync("access-token");

            Assert.True(result.Success);
            var address = Assert.Single(result.Data!);
            Assert.Equal(addressId, address.PublicId);
            Assert.Equal("Customer One", address.FullName);
            Assert.True(address.IsDefaultShipping);
            Assert.Equal(["/api/storefront/stores/default/customer/addresses"], handler.RequestPaths);
        }

        [Fact]
        public async Task CustomerAddressCommands_SendBearerAndUseScopedRoutes()
        {
            var addressId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var requests = new List<string>();
            var handler = new RecordingHandler(request =>
            {
                requests.Add($"{request.Method.Method} {request.RequestUri?.AbsolutePath}");
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

                if (request.Content is not null)
                {
                    var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Assert.DoesNotContain("customerId", body, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("userId", body, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("storeId", body, StringComparison.OrdinalIgnoreCase);
                }

                return JsonResponse(
                    HttpStatusCode.OK,
                    $$"""
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "publicId": "{{addressId}}",
                        "firstName": "Customer",
                        "lastName": "One",
                        "company": null,
                        "address1": "1 Test Street",
                        "address2": null,
                        "city": "New York",
                        "postalCode": "10000",
                        "countryCode": "US",
                        "stateProvinceCode": "NY",
                        "stateProvinceName": "New York",
                        "phone": "5550100",
                        "email": "customer@example.test",
                        "isDefaultShipping": true,
                        "isDefaultBilling": true,
                        "createdAtUtc": "2026-07-17T00:00:00Z",
                        "updatedAtUtc": "2026-07-17T00:00:00Z"
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);
            var request = new StorefrontCustomerAddressRequest
            {
                FirstName = "Customer",
                LastName = "One",
                Address1 = "1 Test Street",
                City = "New York",
                PostalCode = "10000",
                CountryCode = "US",
            };

            Assert.True((await apiClient.CreateCustomerAddressAsync("access-token", request)).Success);
            Assert.True((await apiClient.UpdateCustomerAddressAsync("access-token", addressId, request)).Success);
            Assert.True((await apiClient.SetDefaultShippingAddressAsync("access-token", addressId)).Success);
            Assert.True((await apiClient.SetDefaultBillingAddressAsync("access-token", addressId)).Success);
            Assert.True((await apiClient.DeleteCustomerAddressAsync("access-token", addressId)).Success);

            Assert.Equal(
                [
                    "POST /api/storefront/stores/default/customer/addresses",
                    "PUT /api/storefront/stores/default/customer/addresses/11111111-1111-1111-1111-111111111111",
                    "POST /api/storefront/stores/default/customer/addresses/11111111-1111-1111-1111-111111111111/default-shipping",
                    "POST /api/storefront/stores/default/customer/addresses/11111111-1111-1111-1111-111111111111/default-billing",
                    "DELETE /api/storefront/stores/default/customer/addresses/11111111-1111-1111-1111-111111111111",
                ],
                requests);
        }

        [Fact]
        public async Task CustomerOrdersAsync_SendsBearerAndReadsPagedOrders()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/orders/current-user", request.RequestUri?.AbsolutePath);
                Assert.Equal("?pageNumber=2&pageSize=5", request.RequestUri?.Query);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "items": [
                          {
                            "reference": "ORD-1",
                            "createdOn": "2026-07-17T00:00:00Z",
                            "orderStatus": "processing",
                            "paymentStatus": "paid",
                            "shippingStatus": "not_yet_shipped",
                            "currencyCode": "USD",
                            "totalAmount": 25.00,
                            "itemCount": 2,
                            "trackingSummary": {
                              "shippingCarrier": null,
                              "trackingNumber": null,
                              "trackingUrl": null,
                              "shippedOn": null,
                              "deliveredOn": null,
                              "lastTrackingEventAtUtc": null
                            }
                          }
                        ],
                        "pageNumber": 2,
                        "pageSize": 5,
                        "totalCount": 1,
                        "totalPages": 1
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetCustomerOrdersAsync("access-token", pageNumber: 2, pageSize: 5);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.PageNumber);
            Assert.Equal("ORD-1", Assert.Single(result.Data.Items).Reference);
        }

        [Fact]
        public async Task CustomerOrderDetailAsync_SendsBearerAndReadsSafeDetail()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/orders/current-user/ORD-1", request.RequestUri?.AbsolutePath);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

                return JsonResponse(HttpStatusCode.OK, CreateOrderDetailEnvelope(receiptMode: false));
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetCustomerOrderAsync("access-token", "ORD-1");

            Assert.True(result.Success);
            Assert.Equal("ORD-1", result.Data!.Reference);
            Assert.False(result.Data.ReceiptMode);
            Assert.Equal(25m, result.Data.TotalAmount);
        }

        [Fact]
        public async Task CustomerOrderReceiptAsync_SendsBearerToReceiptRoute()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/orders/current-user/ORD-1/receipt", request.RequestUri?.AbsolutePath);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

                return JsonResponse(HttpStatusCode.OK, CreateOrderDetailEnvelope(receiptMode: true));
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var result = await apiClient.GetCustomerOrderReceiptAsync("access-token", "ORD-1");

            Assert.True(result.Success);
            Assert.True(result.Data!.ReceiptMode);
        }

        [Fact]
        public async Task CustomerProfileAsync_SendsBearerAndUsesSafePayload()
        {
            var profileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal("/api/storefront/stores/default/customer/profile", request.RequestUri?.AbsolutePath);
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.Equal("access-token", request.Headers.Authorization?.Parameter);

                return JsonResponse(
                    HttpStatusCode.OK,
                    $$"""
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "customerPublicId": "{{profileId}}",
                        "email": "customer@example.test",
                        "fullName": "Customer One",
                        "firstName": "Customer",
                        "lastName": "One",
                        "company": "Example LLC",
                        "phoneNumber": "5550100",
                        "preferredLanguage": "en",
                        "preferredCurrencyCode": "USD",
                        "createdAtUtc": "2026-07-17T00:00:00Z",
                        "lastActivityAtUtc": "2026-07-17T01:00:00Z"
                      }
                    }
                    """);
            });
            using var client = CreateClient(handler);
            var apiClient = CreateApiClient(client);

            var getResult = await apiClient.GetCustomerProfileAsync("access-token");
            var updateResult = await apiClient.UpdateCustomerProfileAsync(
                "access-token",
                new StorefrontCustomerProfileUpdateRequest
                {
                    FullName = "Customer One",
                    Email = "customer@example.test",
                    FirstName = "Customer",
                    LastName = "One",
                    PreferredCurrencyCode = "USD",
                });

            Assert.True(getResult.Success);
            Assert.True(updateResult.Success);
            Assert.Equal(profileId, getResult.Data?.CustomerPublicId);
            Assert.Equal("Customer One", updateResult.Data?.FullName);
            Assert.Equal(
                ["/api/storefront/stores/default/customer/profile", "/api/storefront/stores/default/customer/profile"],
                handler.RequestPaths);
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

        private static string CreateOrderDetailEnvelope(bool receiptMode)
        {
            return $$"""
            {
              "success": true,
              "message": "ok",
              "data": {
                "reference": "ORD-1",
                "status": "processing",
                "orderStatus": "processing",
                "paymentStatus": "paid",
                "paymentMethodKey": "cod",
                "paymentAt": null,
                "paymentSummary": {
                  "paymentStatus": "paid",
                  "paymentMethodKey": "cod",
                  "attemptState": "captured",
                  "amount": 25.00,
                  "currencyCode": "USD",
                  "paymentAt": null,
                  "updatedAtUtc": "2026-07-17T00:00:00Z"
                },
                "storeSnapshot": null,
                "currencyCode": "USD",
                "totalAmount": 25.00,
                "totalBreakdown": {
                  "subtotal": 20.00,
                  "shippingTotal": 5.00,
                  "taxTotal": 0,
                  "discountTotal": 0,
                  "grandTotal": 25.00
                },
                "baseCurrencyCode": "USD",
                "baseTotalAmount": 25.00,
                "baseTotalBreakdown": null,
                "exchangeRate": null,
                "exchangeRateProviderKey": null,
                "exchangeRateSource": null,
                "exchangeRateEffectiveAtUtc": null,
                "exchangeRateExpiresAtUtc": null,
                "createdOn": "2026-07-17T00:00:00Z",
                "shippingStatus": "not_yet_shipped",
                "shippingCarrier": null,
                "trackingNumber": null,
                "trackingUrl": null,
                "shippedOn": null,
                "deliveredOn": null,
                "customerName": "Customer One",
                "customerEmail": "customer@example.test",
                "billingAddress": null,
                "shippingAddressSnapshot": null,
                "shippingAddress": {
                  "fullName": "Customer One",
                  "email": "customer@example.test",
                  "phone": null,
                  "address1": "1 Test Street",
                  "address2": null,
                  "city": "New York",
                  "state": "NY",
                  "postalCode": "10000",
                  "countryCode": "US"
                },
                "shippingMethod": null,
                "completedAt": null,
                "cancelledAt": null,
                "trackingEvents": [],
                "historyEntries": [],
                "lines": [
                  {
                    "productId": "11111111-1111-1111-1111-111111111111",
                    "productName": "Test Product",
                    "sku": "SKU-1",
                    "image": null,
                    "productVariantId": null,
                    "variantAttributes": [],
                    "quantity": 2,
                    "unitPrice": 10.00,
                    "lineTotal": 20.00
                  }
                ],
                "actions": {
                  "canRetryPayment": false,
                  "canReorder": false,
                  "canRequestReturn": false,
                  "hasDownloads": false
                },
                "receiptMode": {{receiptMode.ToString().ToLowerInvariant()}}
              }
            }
            """;
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
