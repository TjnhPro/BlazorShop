namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class ControlPlaneNodeServiceTests
    {
        [Fact]
        public async Task CreateAsync_RejectsInvalidControlApiUrl()
        {
            await using var context = CreateContext();
            var service = new ControlPlaneNodeService(context);

            var result = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "VN Main", null, "not-a-url"));

            Assert.False(result.Success);
            Assert.Equal(ControlPlaneNodeOperationFailure.Validation, result.Failure);
        }

        [Fact]
        public async Task CreateAsync_RejectsDuplicateActiveNodeKey()
        {
            await using var context = CreateContext();
            var service = new ControlPlaneNodeService(context);

            var first = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "VN Main", null, "http://localhost:5180/api/controlpanel"));
            var duplicate = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "VN Duplicate", null, "http://localhost:5280/api/controlpanel"));

            Assert.True(first.Success);
            Assert.False(duplicate.Success);
            Assert.Equal(ControlPlaneNodeOperationFailure.Conflict, duplicate.Failure);
        }

        [Fact]
        public async Task ListAsync_UsesCursorPagination()
        {
            await using var context = CreateContext();
            var service = new ControlPlaneNodeService(context);
            await service.CreateAsync(new CreateControlPlaneNodeRequest("node-a", "Node A", null, "http://localhost:5180/api/controlpanel"));
            await service.CreateAsync(new CreateControlPlaneNodeRequest("node-b", "Node B", null, "http://localhost:5280/api/controlpanel"));

            var firstPage = await service.ListAsync(new ControlPlaneNodeListQuery(Limit: 1));
            var secondPage = await service.ListAsync(new ControlPlaneNodeListQuery(Cursor: firstPage.NextCursor, Limit: 1));

            Assert.Single(firstPage.Items);
            Assert.NotNull(firstPage.NextCursor);
            Assert.Single(secondPage.Items);
            Assert.NotEqual(firstPage.Items[0].PublicId, secondPage.Items[0].PublicId);
        }

        [Fact]
        public async Task DisableAsync_DisablesNodeAndActiveEndpoints()
        {
            await using var context = CreateContext();
            var service = new ControlPlaneNodeService(context);
            var created = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "VN Main", null, "http://localhost:5180/api/controlpanel"));

            var disabled = await service.DisableAsync(created.Payload!.PublicId);

            Assert.True(disabled.Success);
            Assert.Equal("disabled", disabled.Payload!.Status);
            Assert.NotNull(disabled.Payload.DisabledAt);
            Assert.All(disabled.Payload.Endpoints, endpoint => Assert.NotNull(endpoint.DisabledAt));
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-nodes-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }
}
