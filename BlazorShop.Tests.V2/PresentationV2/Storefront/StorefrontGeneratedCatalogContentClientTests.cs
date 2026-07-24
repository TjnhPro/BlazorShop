extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using BlazorShop.Storefront.Client;
    using Microsoft.Extensions.Configuration;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Models;
    using GeneratedStorefrontCatalogContentClient = StorefrontV2::BlazorShop.Storefront.Services.GeneratedStorefrontCatalogContentClient;

    public sealed class StorefrontGeneratedCatalogContentClientTests
    {
        [Fact]
        public async Task GetPublishedCatalogPageAsync_UsesGeneratedCatalogClient()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/catalog/products", request.RequestUri?.AbsolutePath);
                Assert.Contains("PageNumber=2", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("PageSize=12", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("SearchTerm=shirt", request.RequestUri?.Query, StringComparison.Ordinal);
                Assert.Contains("CurrencyCode=EUR", request.RequestUri?.Query, StringComparison.Ordinal);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "items": [
                          {
                            "id": "00000000-0000-0000-0000-000000000001",
                            "slug": "test-product",
                            "name": "Test Product",
                            "description": "Description",
                            "price": 12.50,
                            "displayPrice": 11.00,
                            "displayCurrencyCode": "EUR",
                            "image": "/product.png",
                            "createdOn": "2026-07-24T00:00:00Z",
                            "updatedAt": "2026-07-24T00:00:00Z",
                            "displayOrder": 1,
                            "inStock": true,
                            "quantity": 5,
                            "stockStatus": "In stock",
                            "minOrderQuantity": 1,
                            "quantityStep": 1,
                            "purchasable": true
                          }
                        ],
                        "pageNumber": 2,
                        "pageSize": 12,
                        "totalCount": 1
                      }
                    }
                    """);
            });
            var client = CreateClient(handler);

            var result = await client.GetPublishedCatalogPageAsync(
                new ProductCatalogQuery
                {
                    PageNumber = 2,
                    PageSize = 12,
                    SearchTerm = "shirt",
                    SortBy = ProductCatalogSortBy.PriceLowToHigh,
                },
                "EUR");

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value!.PageNumber);
            Assert.Equal("Test Product", Assert.Single(result.Value.Items).Name);
            Assert.Equal("EUR", result.Value.Items[0].DisplayCurrencyCode);
            Assert.Equal(["/api/storefront/stores/default/catalog/products"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetPublishedPageBySlugAsync_UsesGeneratedPagesClient()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/pages/privacy", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "slug": "privacy",
                        "title": "Privacy",
                        "intro": "Intro",
                        "bodyHtml": "<p>Body</p>",
                        "seo": {
                          "metaTitle": "Privacy SEO",
                          "robotsIndex": true,
                          "robotsFollow": true
                        },
                        "updatedAt": "2026-07-24T00:00:00Z",
                        "pageKey": "privacy_policy"
                      }
                    }
                    """);
            });
            var client = CreateClient(handler);

            var result = await client.GetPublishedPageBySlugAsync("privacy");

            Assert.True(result.IsSuccess);
            Assert.Equal("Privacy", result.Value?.Title);
            Assert.Equal("privacy_policy", result.Value?.PageKey);
            Assert.Equal(["/api/storefront/stores/default/pages/privacy"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetNavigationMenuAsync_UsesGeneratedNavigationClient()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/navigation/main", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "systemName": "main",
                        "generatedAt": "2026-07-24T00:00:00Z",
                        "items": [
                          { "label": "Home", "href": "/", "targetType": "url", "opensInNewTab": false, "children": [] }
                        ]
                      }
                    }
                    """);
            });
            var client = CreateClient(handler);

            var result = await client.GetNavigationMenuAsync("main");

            Assert.True(result.IsSuccess);
            Assert.Equal("main", result.Value?.SystemName);
            Assert.Equal("Home", Assert.Single(result.Value!.Items).Label);
            Assert.Equal(["/api/storefront/stores/default/navigation/main"], handler.RequestPaths);
        }

        [Fact]
        public async Task GetSeoSettingsAsync_UsesGeneratedSeoClient()
        {
            var handler = new RecordingHandler(request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/storefront/stores/default/seo/settings", request.RequestUri?.AbsolutePath);

                return JsonResponse(
                    HttpStatusCode.OK,
                    """
                    {
                      "success": true,
                      "message": "ok",
                      "data": {
                        "id": "00000000-0000-0000-0000-000000000001",
                        "siteName": "Default Store",
                        "defaultTitleSuffix": "| Default Store",
                        "defaultMetaDescription": "Default description.",
                        "baseCanonicalUrl": "https://store.example"
                      }
                    }
                    """);
            });
            var client = CreateClient(handler);

            var result = await client.GetSeoSettingsAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal("Default Store", result.Value?.SiteName);
            Assert.Equal("| Default Store", result.Value?.DefaultTitleSuffix);
            Assert.Equal(["/api/storefront/stores/default/seo/settings"], handler.RequestPaths);
        }

        [Fact]
        public void StorefrontDi_UsesGeneratedCatalogContentAdapterForCatalogContentNavigationAndSeo()
        {
            var source = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs");

            Assert.Contains("GeneratedStorefrontCatalogContentClient", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontCatalogClient>", source, StringComparison.Ordinal);
            Assert.Contains("AddScoped<IStorefrontContentClient>", source, StringComparison.Ordinal);
            Assert.Contains("GetRequiredService<GeneratedStorefrontCatalogContentClient>", source, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IStorefrontCatalogClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>())", source, StringComparison.Ordinal);
            Assert.DoesNotContain("AddScoped<IStorefrontContentClient>(serviceProvider => serviceProvider.GetRequiredService<StorefrontApiClient>())", source, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2_CatalogContentNavigationAndSeoUseStorefrontOwnedModels()
        {
            var source = ReadStorefrontV2Source();

            Assert.DoesNotContain("using BlazorShop.Application.CommerceNode.Navigation;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using BlazorShop.Application.CommerceNode.StorefrontPages;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using BlazorShop.Application.CommerceNode.Catalog;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using BlazorShop.Application.DTOs.Category;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("using BlazorShop.Application.DTOs.Seo;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models.Category", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models.Discovery", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models.Pages", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models.Product", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BlazorShop.Web.SharedV2.Models.Seo", source, StringComparison.Ordinal);
        }

        private static GeneratedStorefrontCatalogContentClient CreateClient(RecordingHandler handler)
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

            return new GeneratedStorefrontCatalogContentClient(
                new StorefrontCatalogClient(string.Empty, httpClient),
                new StorefrontPagesClient(string.Empty, httpClient),
                new StorefrontNavigationClient(string.Empty, httpClient),
                new StorefrontSeoClient(string.Empty, httpClient),
                configuration);
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

        private static string ReadStorefrontV2Source()
        {
            var root = Path.Combine(
                RepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.Storefront.V2");

            var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal);

            return string.Join(Environment.NewLine, files.Select(File.ReadAllText));
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
