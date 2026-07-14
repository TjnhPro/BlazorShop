extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using Microsoft.Extensions.Options;
    using Xunit;

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
