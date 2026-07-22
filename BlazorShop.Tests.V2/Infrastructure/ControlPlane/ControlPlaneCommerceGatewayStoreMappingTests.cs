namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using System.Net;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class ControlPlaneCommerceGatewayStoreMappingTests
    {
        [Fact]
        public async Task QueryProductsAsync_AppendsStoreKeyAndNodeCredentialsToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler();
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Products.QueryProductsAsync(store.PublicId, new ProductCatalogQuery { SearchTerm = "bag" });

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
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Categories.ListCategoriesAsync(store.PublicId);

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
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Content.ListStorefrontPagesAsync(
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
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Content.GetStorefrontPageTemplateStatusAsync(store.PublicId);

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
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Content.CreateStorefrontPageDraftFromTemplateAsync(
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
            var gateways = CreateGateways(context, handler);
            var pageId = Guid.Parse("7a89bf2d-3177-4923-9806-902ab6625c72");

            var result = await gateways.Content.UpdateStorefrontPageNavigationAsync(
                store.PublicId,
                pageId,
                new UpdatePageNavigationRequest(100, true, StorefrontPageContentRules.FooterCompany));

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal($"https://node.example.test/api/commerce/admin/pages/{pageId:D}/navigation", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
        }

        [Fact]
        public async Task ListNavigationMenusAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(RecordingHandler.EmptyArrayEnvelope);
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Navigation.ListNavigationMenusAsync(store.PublicId);

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://node.example.test/api/commerce/admin/navigation/menus", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
            Assert.Equal("node-a", request.Headers.GetValues("X-Node-Key").Single());
            Assert.Equal("test-node-secret", request.Headers.GetValues("X-Node-Secret").Single());
        }

        [Fact]
        public async Task CreateNavigationItemAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(RecordingHandler.NavigationMenuEnvelope);
            var gateways = CreateGateways(context, handler);
            var menuPublicId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var result = await gateways.Navigation.CreateNavigationItemAsync(
                store.PublicId,
                menuPublicId,
                new CreateStoreNavigationMenuItemRequest(
                    null,
                    "Home",
                    StoreNavigationTargetTypes.System,
                    StoreNavigationSystemTargets.Home,
                    null,
                    null));

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"https://node.example.test/api/commerce/admin/navigation/menus/{menuPublicId:D}/items", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
        }

        [Fact]
        public async Task GetShippingSettingsAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(RecordingHandler.ShippingSettingsEnvelope);
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Shipping.GetShippingSettingsAsync(store.PublicId);

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://node.example.test/api/commerce/admin/shipping/settings", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
            Assert.Equal("node-a", request.Headers.GetValues("X-Node-Key").Single());
            Assert.Equal("test-node-secret", request.Headers.GetValues("X-Node-Secret").Single());
        }

        [Fact]
        public async Task UpdateShippingSettingsAsync_AppendsStoreKeyToCommerceNodeRequest()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(RecordingHandler.ShippingSettingsEnvelope);
            var gateways = CreateGateways(context, handler);

            var result = await gateways.Shipping.UpdateShippingSettingsAsync(
                store.PublicId,
                new UpdateStoreShippingSettingsRequest(
                    new StoreShippingOriginDto(
                        "Fulfillment",
                        "Main Store",
                        "1 Shipping Way",
                        null,
                        "Austin",
                        "TX",
                        "78701",
                        "US"),
                    ["US"],
                    7.5m,
                    100m,
                    StoreShippingSurchargePolicies.Sum,
                    "3-5 days"));

            Assert.True(result.Success);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("https://node.example.test/api/commerce/admin/shipping/settings", request.RequestUri!.GetLeftPart(UriPartial.Path));
            Assert.Contains("storeKey=main-store", request.RequestUri.Query);
        }

        [Fact]
        public void LegacyCatalogFacade_IsRemovedFromInfrastructure()
        {
            var root = FindRepositoryRoot().FullName;
            var path = Path.Combine(root, "BlazorShop.Infrastructure", "Data", "ControlPlane", "ControlPlaneCommerce" + "CatalogService.cs");

            Assert.False(File.Exists(path));
        }

        [Fact]
        public async Task Transport_ReturnsValidationFailureForArchivedStore()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var entity = await context.Stores.SingleAsync(item => item.PublicId == store.PublicId);
            entity.Status = "archived";
            await context.SaveChangesAsync();
            var handler = new RecordingHandler();
            var transport = CreateTransport(context, handler);

            var result = await transport.SendAsync<object>(
                store.PublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/query",
                null);

            Assert.False(result.Success);
            Assert.Equal(CommerceNodeAdminGatewayFailure.Validation, result.Failure);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task Transport_MapsMalformedRemoteResponse()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler("{not-json}");
            var transport = CreateTransport(context, handler);

            var result = await transport.SendAsync<object>(
                store.PublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/query",
                null);

            Assert.False(result.Success);
            Assert.Equal(CommerceNodeAdminGatewayFailure.RemoteFailure, result.Failure);
            Assert.Equal("Commerce Node returned malformed JSON.", result.Message);
        }

        [Fact]
        public async Task Transport_MapsEmptyRemoteResponse()
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(string.Empty);
            var transport = CreateTransport(context, handler);

            var result = await transport.SendAsync<object>(
                store.PublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/query",
                null);

            Assert.False(result.Success);
            Assert.Equal(CommerceNodeAdminGatewayFailure.RemoteFailure, result.Failure);
            Assert.Equal("Commerce Node returned an empty response.", result.Message);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, CommerceNodeAdminGatewayFailure.Validation)]
        [InlineData(HttpStatusCode.NotFound, CommerceNodeAdminGatewayFailure.NotFound)]
        [InlineData(HttpStatusCode.Conflict, CommerceNodeAdminGatewayFailure.Validation)]
        [InlineData(HttpStatusCode.InternalServerError, CommerceNodeAdminGatewayFailure.RemoteFailure)]
        public async Task Transport_MapsRemoteFailureStatusCodes(
            HttpStatusCode statusCode,
            CommerceNodeAdminGatewayFailure expectedFailure)
        {
            await using var context = CreateContext();
            var store = await CreateStoreAsync(context);
            var handler = new RecordingHandler(
                """
                {
                  "success": false,
                  "message": "remote failed",
                  "data": null
                }
                """,
                statusCode);
            var transport = CreateTransport(context, handler);

            var result = await transport.SendAsync<object>(
                store.PublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/query",
                null);

            Assert.False(result.Success);
            Assert.Equal(expectedFailure, result.Failure);
            Assert.Equal((int)statusCode, result.HttpStatusCode);
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

        private static GatewaySet CreateGateways(
            ControlPlaneDbContext context,
            RecordingHandler handler)
        {
            var transport = CreateTransport(context, handler);
            return new GatewaySet(
                new ControlPlaneProductGateway(transport),
                new ControlPlaneCategoryGateway(transport),
                new ControlPlaneContentGateway(transport),
                new ControlPlaneNavigationGateway(transport),
                new ControlPlaneShippingGateway(transport));
        }

        private sealed record GatewaySet(
            ControlPlaneProductGateway Products,
            ControlPlaneCategoryGateway Categories,
            ControlPlaneContentGateway Content,
            ControlPlaneNavigationGateway Navigation,
            ControlPlaneShippingGateway Shipping);

        private static CommerceNodeAdminGatewayTransport CreateTransport(
            ControlPlaneDbContext context,
            RecordingHandler handler)
        {
            return new CommerceNodeAdminGatewayTransport(context, new HttpClient(handler));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, relativePath));
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "BlazorShop.sln")))
            {
                current = current.Parent;
            }

            Assert.NotNull(current);
            return current!;
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

            public const string NavigationMenuEnvelope = """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "publicId": "11111111-1111-1111-1111-111111111111",
                    "systemName": "main",
                    "displayName": "Main",
                    "isEnabled": true,
                    "updatedAt": "2026-07-15T00:00:00+00:00",
                    "items": []
                  }
                }
                """;

            public const string ShippingSettingsEnvelope = """
                {
                  "success": true,
                  "message": "ok",
                  "data": {
                    "publicId": "22222222-2222-2222-2222-222222222222",
                    "origin": {
                      "fullName": "Fulfillment",
                      "company": "Main Store",
                      "address1": "1 Shipping Way",
                      "address2": null,
                      "city": "Austin",
                      "stateProvinceCode": "TX",
                      "postalCode": "78701",
                      "countryCode": "US"
                    },
                    "enabledCountryCodes": ["US"],
                    "defaultFlatRate": 7.5,
                    "freeShippingThreshold": 100,
                    "surchargePolicy": "sum",
                    "defaultDeliveryEstimateText": "3-5 days",
                    "createdAt": "2026-07-17T00:00:00+00:00",
                    "updatedAt": "2026-07-17T00:00:00+00:00",
                    "updatedByUserId": "actor-1"
                  }
                }
                """;

            private readonly string responseBody;
            private readonly HttpStatusCode statusCode;

            public RecordingHandler()
                : this(EmptyPagedEnvelope)
            {
            }

            public RecordingHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                this.responseBody = responseBody;
                this.statusCode = statusCode;
            }

            public List<HttpRequestMessage> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.Requests.Add(CloneRequest(request));

                return Task.FromResult(new HttpResponseMessage(this.statusCode)
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

