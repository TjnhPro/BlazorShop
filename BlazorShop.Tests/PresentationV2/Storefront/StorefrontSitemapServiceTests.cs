extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Xml.Linq;

    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Web.SharedV2.Models.Discovery;

    using Microsoft.Extensions.Options;

    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontSitemapServiceTests
    {
        [Fact]
        public async Task GenerateAsync_NormalizesCanonicalPathsAndExcludesNoIndexRoutes()
        {
            var sitemap = new GetPublicCatalogSitemap
            {
                Categories =
                [
                    new GetCategorySitemapEntry { Slug = "apparel", LastModifiedUtc = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc) },
                ],
                Products =
                [
                    new GetProductSitemapEntry { Slug = "qa-shirt", LastModifiedUtc = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc) },
                ],
                Pages =
                [
                    new GetPageSitemapEntry { Slug = "privacy", LastModifiedUtc = new DateTime(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc) },
                ],
            };
            var service = CreateService(sitemap);

            var result = await service.GenerateAsync();

            Assert.False(result.IsServiceUnavailable);
            var locations = ReadLocations(result.Content!);
            Assert.Contains("https://shop.example.com/category/apparel", locations);
            Assert.Contains("https://shop.example.com/product/qa-shirt", locations);
            Assert.Contains("https://shop.example.com/pages/privacy", locations);
            Assert.All(locations, location => Assert.DoesNotContain("?", location, StringComparison.Ordinal));
        }

        private static StorefrontSitemapService CreateService(GetPublicCatalogSitemap sitemap)
        {
            var apiClient = new StorefrontApiClient(
                new HttpClient(new SitemapHttpMessageHandler(sitemap))
                {
                    BaseAddress = new Uri("https://commerce.example/api/storefront/stores/default/"),
                },
                Options.Create(new StorefrontApiOptions
                {
                    EnableLegacyFallback = false,
                }));

            return new StorefrontSitemapService(
                apiClient,
                new StubPublicUrlResolver(),
                new StubSeoSettingsProvider());
        }

        private static IReadOnlyList<string> ReadLocations(string xml)
        {
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            return XDocument
                .Parse(xml)
                .Descendants(ns + "loc")
                .Select(element => element.Value)
                .ToList();
        }

        private sealed class SitemapHttpMessageHandler : HttpMessageHandler
        {
            private readonly GetPublicCatalogSitemap sitemap;

            public SitemapHttpMessageHandler(GetPublicCatalogSitemap sitemap)
            {
                this.sitemap = sitemap;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(this.sitemap),
                    RequestMessage = request,
                });
            }
        }

        private sealed class StubPublicUrlResolver : IStorefrontPublicUrlResolver
        {
            public string? ResolveBaseUrl(string? configuredBaseUrl = null)
            {
                return configuredBaseUrl ?? "https://shop.example.com/";
            }

            public string? ResolveAbsoluteUrl(string? relativeOrAbsoluteUrl, string? configuredBaseUrl = null)
            {
                if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
                {
                    return null;
                }

                return new Uri(new Uri(this.ResolveBaseUrl(configuredBaseUrl)!, UriKind.Absolute), relativeOrAbsoluteUrl.TrimStart('/')).ToString();
            }
        }

        private sealed class StubSeoSettingsProvider : IStorefrontSeoSettingsProvider
        {
            public Task<SeoSettingsDto?> GetAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<SeoSettingsDto?>(new SeoSettingsDto
                {
                    BaseCanonicalUrl = "https://shop.example.com",
                });
            }
        }
    }
}
