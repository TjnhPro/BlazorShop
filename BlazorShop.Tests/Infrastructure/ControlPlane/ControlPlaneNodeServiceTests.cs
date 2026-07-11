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

            var result = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "test-node-secret", "VN Main", null, "not-a-url"));

            Assert.False(result.Success);
            Assert.Equal(ControlPlaneNodeOperationFailure.Validation, result.Failure);
        }

        [Fact]
        public async Task CreateAsync_RejectsDuplicateActiveNodeKey()
        {
            await using var context = CreateContext();
            var service = new ControlPlaneNodeService(context);

            var first = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "test-node-secret", "VN Main", null, "http://localhost:5180/api/controlpanel"));
            var duplicate = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "test-node-secret", "VN Duplicate", null, "http://localhost:5280/api/controlpanel"));

            Assert.True(first.Success);
            Assert.False(duplicate.Success);
            Assert.Equal(ControlPlaneNodeOperationFailure.Conflict, duplicate.Failure);
        }

        [Fact]
        public async Task ListAsync_UsesPagePagination()
        {
            await using var context = CreateContext();
            var service = new ControlPlaneNodeService(context);
            await service.CreateAsync(new CreateControlPlaneNodeRequest("node-a", "test-node-secret", "Node A", null, "http://localhost:5180/api/controlpanel"));
            await service.CreateAsync(new CreateControlPlaneNodeRequest("node-b", "test-node-secret", "Node B", null, "http://localhost:5280/api/controlpanel"));

            var firstPage = await service.ListAsync(new ControlPlaneNodeListQuery(PageNumber: 1, PageSize: 1));
            var secondPage = await service.ListAsync(new ControlPlaneNodeListQuery(PageNumber: 2, PageSize: 1));

            Assert.Single(firstPage.Items);
            Assert.Equal(2, firstPage.TotalCount);
            Assert.Equal(1, firstPage.PageNumber);
            Assert.Equal(2, firstPage.TotalPages);
            Assert.Single(secondPage.Items);
            Assert.Equal(2, secondPage.PageNumber);
            Assert.NotEqual(firstPage.Items[0].PublicId, secondPage.Items[0].PublicId);
        }

        [Fact]
        public async Task DisableAsync_DisablesNodeAndActiveEndpoints()
        {
            await using var context = CreateContext();
            var service = new ControlPlaneNodeService(context);
            var created = await service.CreateAsync(new CreateControlPlaneNodeRequest("vn-main", "test-node-secret", "VN Main", null, "http://localhost:5180/api/controlpanel"));

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
