namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using System.Net;
    using System.Net.Http;

    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class ControlPlaneHealthServiceTests
    {
        [Fact]
        public async Task ProbeAsync_PersistsHealthySnapshot()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = CreateService(context, request =>
            {
                Assert.Equal("/api/commerce/healthz", request.RequestUri!.AbsolutePath);
                Assert.True(request.Headers.TryGetValues("X-Node-Key", out var nodeKeyValues));
                Assert.Equal(node.NodeKey, Assert.Single(nodeKeyValues));
                Assert.True(request.Headers.TryGetValues("X-Node-Secret", out var nodeSecretValues));
                Assert.Equal("test-node-secret", Assert.Single(nodeSecretValues));
                return JsonResponse("""{"success":true,"message":"Commerce Node is healthy.","data":{"status":"healthy","dependencies":{"postgres":"healthy"}}}""");
            });

            var result = await service.ProbeAsync(node.PublicId);
            var persistedNode = await context.Nodes.SingleAsync();

            Assert.True(result.Success);
            Assert.Equal("healthy", result.Payload!.Health.Status);
            Assert.False(result.Payload.CapabilityChanged);
            Assert.Equal("healthy", persistedNode.Status);
            Assert.NotNull(persistedNode.LastSeenAt);
            Assert.Equal(1, await context.NodeHealthSnapshots.CountAsync());
            Assert.Equal(0, await context.NodeCapabilitySnapshots.CountAsync());
        }

        [Fact]
        public async Task ProbeAsync_DoesNotCreateCapabilitySnapshotForMvpHealthz()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = CreateService(context, _ => JsonResponse("""{"success":true,"message":"ok","data":{"status":"healthy"}}"""));

            var first = await service.ProbeAsync(node.PublicId);
            var second = await service.ProbeAsync(node.PublicId);

            Assert.False(first.Payload!.CapabilityChanged);
            Assert.False(second.Payload!.CapabilityChanged);
            Assert.Equal(2, await context.NodeHealthSnapshots.CountAsync());
            Assert.Equal(0, await context.NodeCapabilitySnapshots.CountAsync());
        }

        [Fact]
        public async Task ProbeAsync_PersistsMalformedHealthSnapshot()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = CreateService(context, _ => JsonResponse("not-json"));

            var result = await service.ProbeAsync(node.PublicId);
            var persistedNode = await context.Nodes.SingleAsync();

            Assert.True(result.Success);
            Assert.Equal("malformed", result.Payload!.Health.Status);
            Assert.Equal("down", persistedNode.Status);
            Assert.Equal("malformed_payload", result.Payload.Health.ErrorCode);
            Assert.Equal(0, await context.NodeCapabilitySnapshots.CountAsync());
        }

        [Fact]
        public async Task ProbeAsync_PersistsDownSnapshotWhenHealthEndpointFails()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = CreateService(context, _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"success":false,"message":"Invalid Commerce Node credential.","data":null}""", System.Text.Encoding.UTF8, "application/json")
            });

            var result = await service.ProbeAsync(node.PublicId);

            Assert.True(result.Success);
            Assert.Equal("down", result.Payload!.Health.Status);
            Assert.Equal(401, result.Payload.Health.HttpStatusCode);
            Assert.Equal("invalid_credentials", result.Payload.Health.ErrorCode);
        }

        [Fact]
        public async Task ProbeAsync_RejectsDisabledNode()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var nodeService = new ControlPlaneNodeService(context);
            await nodeService.DisableAsync(node.PublicId);
            var service = CreateService(context, _ => JsonResponse("""{"status":"healthy"}"""));

            var result = await service.ProbeAsync(node.PublicId);

            Assert.False(result.Success);
        }

        private static ControlPlaneHealthService CreateService(
            ControlPlaneDbContext context,
            Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            var client = new HttpClient(new StubHttpMessageHandler(responseFactory));
            var controlClient = new CommerceNodeControlClient(client);
            return new ControlPlaneHealthService(context, controlClient);
        }

        private static async Task<ControlPlaneNodeDetail> CreateNodeAsync(ControlPlaneDbContext context)
        {
            var nodeService = new ControlPlaneNodeService(context);
            var nodeKey = ($"node-{Guid.NewGuid():N}")[..12];
            var created = await nodeService.CreateAsync(new CreateControlPlaneNodeRequest(
                nodeKey,
                "test-node-secret",
                "Test Node",
                null,
                "http://node.example"));

            Assert.True(created.Success);
            return created.Payload!;
        }

        private static HttpResponseMessage JsonResponse(string content)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-health-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

            public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            {
                this.responseFactory = responseFactory;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(this.responseFactory(request));
            }
        }
    }
}
