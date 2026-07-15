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

            public List<HttpRequestMessage> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.Requests.Add(CloneRequest(request));

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(EmptyPagedEnvelope, Encoding.UTF8, "application/json")
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
