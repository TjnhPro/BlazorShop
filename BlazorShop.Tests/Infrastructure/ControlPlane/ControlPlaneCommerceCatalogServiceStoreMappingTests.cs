namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using System.Net;
    using System.Text;

    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class ControlPlaneCommerceCatalogServiceStoreMappingTests
    {
        [Fact]
        public async Task QueryProductsAsync_AppendsStoreKeyAndNodeCredentialsToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler();
            var service = new ControlPlaneCommerceCatalogService(context, new HttpClient(handler));

            var result = await service.QueryProductsAsync(store.PublicId, new ProductCatalogQuery { SearchTerm = "bag" });

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal("https://node.example.test/api/commerce/admin/products/query", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("searchTerm=bag", request.RequestUri.Query);
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
            Assert.Equal("node-a", request.Headers.GetValues("X-Node-Key").Single());
            Assert.Equal("test-node-secret", request.Headers.GetValues("X-Node-Secret").Single());
        }

        [Fact]
        public async Task ListCategoriesAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler();
            var service = new ControlPlaneCommerceCatalogService(context, new HttpClient(handler));

            var result = await service.ListCategoriesAsync(store.PublicId);

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal("https://node.example.test/api/commerce/admin/categories", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("pageNumber=1", request.RequestUri.Query);
            Assert.Contains("pageSize=25", request.RequestUri.Query);
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
        }

        [Fact]
        public async Task ListStorefrontPagesAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler();
            var service = new ControlPlaneCommerceCatalogService(context, new HttpClient(handler));

            var result = await service.ListStorefrontPagesAsync(
                store.PublicId,
                new StorefrontPageListQuery(PageNumber: 2, PageSize: 10, Search: "about"));

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal("https://node.example.test/api/commerce/admin/pages", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("pageNumber=2", request.RequestUri.Query);
            Assert.Contains("pageSize=10", request.RequestUri.Query);
            Assert.Contains("search=about", request.RequestUri.Query);
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
        }

        [Fact]
        public async Task GetStorefrontPageTemplateStatusAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(RecordingHandler.EmptyArrayEnvelope);
            var service = new ControlPlaneCommerceCatalogService(context, new HttpClient(handler));

            var result = await service.GetStorefrontPageTemplateStatusAsync(store.PublicId);

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal("https://node.example.test/api/commerce/admin/pages/template-status", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
            Assert.Equal("node-a", request.Headers.GetValues("X-Node-Key").Single());
            Assert.Equal("test-node-secret", request.Headers.GetValues("X-Node-Secret").Single());
        }

        [Fact]
        public async Task CreateStorefrontPageDraftFromTemplateAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(RecordingHandler.EmptyPageEnvelope);
            var service = new ControlPlaneCommerceCatalogService(context, new HttpClient(handler));

            var result = await service.CreateStorefrontPageDraftFromTemplateAsync(
                store.PublicId,
                "about",
                new CreatePageFromTemplateRequest());

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://node.example.test/api/commerce/admin/pages/templates/about/draft", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
        }

        [Fact]
        public async Task UpdateStorefrontPageNavigationAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(RecordingHandler.EmptyPageEnvelope);
            var service = new ControlPlaneCommerceCatalogService(context, new HttpClient(handler));
            var pageId = Guid.Parse("7a89bf2d-3177-4923-9806-902ab6625c72");

            var result = await service.UpdateStorefrontPageNavigationAsync(
                store.PublicId,
                pageId,
                new UpdatePageNavigationRequest(100, true, StorefrontPageContentRules.FooterCompany));

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal($"https://node.example.test/api/commerce/admin/pages/{pageId:D}/navigation", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
        }

        private static async Task<ControlPlaneStoreDetail> CreateStoreAsync(ControlPlaneDbContext context)
        {
            var nodeService = new ControlPlaneNodeService(context);
            var node = await nodeService.CreateAsync(new CreateControlPlaneNodeRequest(
                "node-a",
                "test-node-secret",
                "Node A",
                null,
                "https://node.example.test"));

            Assert.True(node.Success);

            var storeService = new ControlPlaneStoreService(context);
            var store = await storeService.CreateAsync(new CreateControlPlaneStoreRequest(
                "main-store",
                "Main Store",
                node.Payload!.PublicId,
                null));

            Assert.True(store.Success);
            return store.Payload!;
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-commerce-catalog-store-mapping-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private const string EmptyPagedEnvelope = """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "items": [],
                    "pageNumber": 1,
                    "pageSize": 25,
                    "totalCount": 0,
                    "totalPages": 0
                  }
                }
                """;

            public const string EmptyArrayEnvelope = """
                {
                  "success": true,
                  "message": "ok",
                  "data": []
                }
                """;

            public const string EmptyPageEnvelope = """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "id": "00000000-0000-0000-0000-000000000001",
                    "publicId": "00000000-0000-0000-0000-000000000002",
                    "storeId": "00000000-0000-0000-0000-000000000003",
                    "slug": "about-us",
                    "title": "About us",
                    "intro": null,
                    "bodyHtml": "<p>About</p>",
                    "isPublished": false,
                    "includeInSitemap": false,
                    "seo": {
                      "metaTitle": null,
                      "metaDescription": null,
                      "canonicalUrl": null,
                      "ogTitle": null,
                      "ogDescription": null,
                      "ogImage": null,
                      "robotsIndex": true,
                      "robotsFollow": true
                    },
                    "createdAt": "2026-07-15T00:00:00+00:00",
                    "updatedAt": "2026-07-15T00:00:00+00:00",
                    "pageKey": "about",
                    "displayOrder": 100,
                    "includeInNavigation": false,
                    "navigationLocation": null
                  }
                }
                """;

            private readonly string responseBody;

            public RecordingHandler()
                : this(EmptyPagedEnvelope)
            {
            }

            public RecordingHandler(string responseBody)
            {
                this.responseBody = responseBody;
            }

            public List<HttpRequestMessage> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.Requests.Add(CloneRequest(request));

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(this.responseBody, Encoding.UTF8, "application/json")
                });
            }

            private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
            {
                var clone = new HttpRequestMessage(request.Method, request.RequestUri);
                foreach (var header in request.Headers)
                {
                    clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                return clone;
            }
        }
    }
}
