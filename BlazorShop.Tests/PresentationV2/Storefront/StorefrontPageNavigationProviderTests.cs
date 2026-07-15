extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using Microsoft.Extensions.Options;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontPageNavigationProviderTests
    {
        [Fact]
        public async Task GetLinksByLocationAsync_FiltersOrdersAndCachesWithinScope()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, """
                {
                  "success": true,
                  "message": "ok",
                  "data": [
                    {
                      "pageKey": "cookie_information",
                      "slug": "cookies",
                      "title": "Cookie information",
                      "navigationLocation": "footer_legal",
                      "displayOrder": 320
                    },
                    {
                      "pageKey": "terms_conditions",
                      "slug": "terms",
                      "title": "Terms and conditions",
                      "navigationLocation": "footer_legal",
                      "displayOrder": 300
                    },
                    {
                      "pageKey": "about",
                      "slug": "about-us",
                      "title": "About us",
                      "navigationLocation": "footer_company",
                      "displayOrder": 100
                    }
                  ]
                }
                """);
            using var client = CreateClient(handler);
            var provider = new StorefrontPageNavigationProvider(CreateApiClient(client));

            var first = await provider.GetLinksByLocationAsync("footer_legal");
            var second = await provider.GetLinksByLocationAsync("footer_legal");

            Assert.Equal(["terms", "cookies"], first.Select(link => link.Slug).ToArray());
            Assert.Equal(["terms", "cookies"], second.Select(link => link.Slug).ToArray());
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task GetLinksAsync_WhenNavigationEndpointUnavailable_ReturnsEmptyList()
        {
            var handler = new RecordingHandler(
                HttpStatusCode.ServiceUnavailable,
                """{"success":false,"message":"down","data":null}""");
            using var client = CreateClient(handler);
            var provider = new StorefrontPageNavigationProvider(CreateApiClient(client));

            var links = await provider.GetLinksAsync();

            Assert.Empty(links);
            Assert.Equal(1, handler.RequestCount);
        }

        private static StorefrontApiClient CreateApiClient(HttpClient client)
        {
            return new StorefrontApiClient(
                client,
                Options.Create(new StorefrontApiOptions()));
        }

        private static HttpClient CreateClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/default/"),
            };
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode statusCode;
            private readonly string json;

            public RecordingHandler(HttpStatusCode statusCode, string json)
            {
                this.statusCode = statusCode;
                this.json = json;
            }

            public int RequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Assert.Equal("/api/storefront/stores/default/pages/navigation", request.RequestUri?.AbsolutePath);
                this.RequestCount++;

                return Task.FromResult(new HttpResponseMessage(this.statusCode)
                {
                    Content = new StringContent(this.json, Encoding.UTF8, "application/json"),
                });
            }
        }
    }
}
