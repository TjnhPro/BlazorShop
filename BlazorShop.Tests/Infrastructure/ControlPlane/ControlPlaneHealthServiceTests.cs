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
        public async Task ProbeAsync_PersistsHealthySnapshotAndCapability()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = CreateService(context, request =>
                request.RequestUri!.AbsolutePath.EndsWith("/health", StringComparison.Ordinal)
                    ? JsonResponse("""{"status":"healthy","dependencies":{"postgres":"healthy"}}""")
                    : JsonResponse("""{"schemaVersion":"v1","features":["stores","health"]}"""));

            var result = await service.ProbeAsync(node.PublicId);
            var persistedNode = await context.Nodes.SingleAsync();

            Assert.True(result.Success);
            Assert.Equal("healthy", result.Payload!.Health.Status);
            Assert.True(result.Payload.CapabilityChanged);
            Assert.Equal("healthy", persistedNode.Status);
            Assert.NotNull(persistedNode.LastSeenAt);
            Assert.Equal(1, await context.NodeHealthSnapshots.CountAsync());
            Assert.Equal(1, await context.NodeCapabilitySnapshots.CountAsync());
        }

        [Fact]
        public async Task ProbeAsync_DoesNotDuplicateUnchangedCapabilitySnapshot()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = CreateService(context, request =>
                request.RequestUri!.AbsolutePath.EndsWith("/health", StringComparison.Ordinal)
                    ? JsonResponse("""{"status":"healthy"}""")
                    : JsonResponse("""{"schemaVersion":"v1","features":["health"]}"""));

            var first = await service.ProbeAsync(node.PublicId);
            var second = await service.ProbeAsync(node.PublicId);

            Assert.True(first.Payload!.CapabilityChanged);
            Assert.False(second.Payload!.CapabilityChanged);
            Assert.Equal(2, await context.NodeHealthSnapshots.CountAsync());
            Assert.Equal(1, await context.NodeCapabilitySnapshots.CountAsync());
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
            var service = CreateService(context, _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            var result = await service.ProbeAsync(node.PublicId);

            Assert.True(result.Success);
            Assert.Equal("down", result.Payload!.Health.Status);
            Assert.Equal(503, result.Payload.Health.HttpStatusCode);
            Assert.Equal("http_status", result.Payload.Health.ErrorCode);
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
                "Test Node",
                null,
                "http://node.example/api/controlpanel"));

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
