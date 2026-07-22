extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    public sealed class StorefrontCurrentStoreProviderTests
    {
        [Fact]
        public async Task ResolveAsync_WhenCurrentStoreSucceeds_CachesResultPerRequest()
        {
            var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, CurrentStoreEnvelope()));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/default/"),
            };

            var apiClient = new StorefrontApiClient(
                httpClient,
                Options.Create(new StorefrontApiOptions()));
            var accessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext(),
            };
            var provider = new StorefrontCurrentStoreProvider(
                apiClient,
                accessor,
                NullLogger<StorefrontCurrentStoreProvider>.Instance);

            var first = await provider.ResolveAsync();
            var second = await provider.ResolveAsync();

            Assert.Equal(StorefrontCurrentStoreResolutionStatus.Success, first.Status);
            Assert.Same(first, second);
            Assert.Equal("default", first.Store?.StoreKey);
            Assert.Equal(["/api/storefront/stores/default/store/current"], handler.RequestPaths);
        }

        [Fact]
        public async Task ResolveAsync_WhenCurrentStoreIsMissing_ReturnsNotFound()
        {
            var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.NotFound, """{"success":false,"message":"missing","data":null}"""));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/missing/"),
            };

            var apiClient = new StorefrontApiClient(
                httpClient,
                Options.Create(new StorefrontApiOptions()));
            var provider = new StorefrontCurrentStoreProvider(
                apiClient,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                NullLogger<StorefrontCurrentStoreProvider>.Instance);

            var result = await provider.ResolveAsync();

            Assert.Equal(StorefrontCurrentStoreResolutionStatus.NotFound, result.Status);
            Assert.Null(result.Store);
            Assert.Equal(["/api/storefront/stores/missing/store/current"], handler.RequestPaths);
        }

        [Fact]
        public async Task ResolveAsync_WhenCurrentStoreIsInMaintenance_ReturnsMaintenance()
        {
            var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, CurrentStoreEnvelope(maintenanceModeEnabled: true)));
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/default/"),
            };

            var apiClient = new StorefrontApiClient(
                httpClient,
                Options.Create(new StorefrontApiOptions()));
            var provider = new StorefrontCurrentStoreProvider(
                apiClient,
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                NullLogger<StorefrontCurrentStoreProvider>.Instance);

            var result = await provider.ResolveAsync();

            Assert.Equal(StorefrontCurrentStoreResolutionStatus.Maintenance, result.Status);
            Assert.Equal("Maintenance window.", result.Message);
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private static string CurrentStoreEnvelope(bool maintenanceModeEnabled = false)
        {
            var maintenance = maintenanceModeEnabled ? "true" : "false";
            return $$"""
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "publicId": "11111111-1111-1111-1111-111111111111",
                    "storeKey": "default",
                    "name": "Default Store",
                    "status": "Active",
                    "baseUrl": "https://store.example/",
                    "primaryDomain": "store.example",
                    "forceHttps": true,
                    "cdnHost": null,
                    "logoUrl": null,
                    "faviconUrl": null,
                    "pngIconUrl": null,
                    "appleTouchIconUrl": null,
                    "msTileImageUrl": null,
                    "msTileColor": null,
                    "defaultCurrencyCode": "USD",
                    "defaultCulture": "en-US",
                    "supportEmail": null,
                    "supportPhone": null,
                    "maintenanceModeEnabled": {{maintenance}},
                    "maintenanceMessage": "Maintenance window.",
                    "htmlBodyId": null
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
                _requestPaths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
                return Task.FromResult(_handler(request));
            }
        }
    }
}
