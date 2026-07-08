namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class ControlPlaneDashboardServiceTests
    {
        [Fact]
        public async Task GetSummaryAsync_CountsPersistedNodesAndStores()
        {
            await using var context = CreateContext();
            var healthyNode = await CreateNodeAsync(context, "node-healthy");
            var warningNode = await CreateNodeAsync(context, "node-warning");
            var downNode = await CreateNodeAsync(context, "node-down");
            var unknownNode = await CreateNodeAsync(context, "node-unknown");
            await CreateStoreAsync(context, healthyNode.PublicId, "main-store");
            await CreateStoreAsync(context, warningNode.PublicId, "backup-store");

            await SetNodeStatusAsync(context, healthyNode.PublicId, "healthy");
            await SetNodeStatusAsync(context, warningNode.PublicId, "warning");
            await SetNodeStatusAsync(context, downNode.PublicId, "down");
            await SetNodeStatusAsync(context, unknownNode.PublicId, "unknown");

            var summary = await new ControlPlaneDashboardService(context).GetSummaryAsync();

            Assert.Equal(4, summary.TotalNodes);
            Assert.Equal(1, summary.HealthyNodes);
            Assert.Equal(1, summary.WarningNodes);
            Assert.Equal(1, summary.DownNodes);
            Assert.Equal(2, summary.TotalStores);
        }

        private static async Task<ControlPlaneNodeDetail> CreateNodeAsync(ControlPlaneDbContext context, string nodeKey)
        {
            var nodeService = new ControlPlaneNodeService(context);
            var created = await nodeService.CreateAsync(new CreateControlPlaneNodeRequest(
                nodeKey,
                nodeKey,
                null,
                $"http://{nodeKey}.example/api/controlpanel"));

            Assert.True(created.Success);
            return created.Payload!;
        }

        private static async Task CreateStoreAsync(ControlPlaneDbContext context, Guid nodePublicId, string storeKey)
        {
            var storeService = new ControlPlaneStoreService(context);
            var created = await storeService.CreateAsync(new CreateControlPlaneStoreRequest(
                storeKey,
                storeKey,
                nodePublicId,
                "{}"));

            Assert.True(created.Success);
        }

        private static async Task SetNodeStatusAsync(ControlPlaneDbContext context, Guid nodePublicId, string status)
        {
            var node = await context.Nodes.SingleAsync(candidate => candidate.PublicId == nodePublicId);
            node.Status = status;
            node.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-dashboard-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }
}
