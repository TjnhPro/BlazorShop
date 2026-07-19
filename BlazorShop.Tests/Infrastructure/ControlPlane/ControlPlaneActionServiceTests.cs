namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Actions;
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class ControlPlaneActionServiceTests
    {
        [Fact]
        public async Task EnqueueAsync_DuplicateIdempotencyKeyReturnsExistingAction()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneActionService(context);

            var first = await service.EnqueueAsync(new EnqueueControlActionRequest(
                node.PublicId,
                "probe_health",
                IdempotencyKey: "probe-node-a",
                CorrelationId: "corr-1"));
            var duplicate = await service.EnqueueAsync(new EnqueueControlActionRequest(
                node.PublicId,
                "probe_health",
                IdempotencyKey: "probe-node-a",
                CorrelationId: "corr-2"));

            Assert.True(first.Success);
            Assert.True(duplicate.Success);
            Assert.True(duplicate.AlreadyExists);
            Assert.Equal(first.Payload!.PublicId, duplicate.Payload!.PublicId);
            Assert.Single(context.Actions);
            Assert.Equal("corr-1", duplicate.Payload.CorrelationId);
        }

        [Fact]
        public async Task EnqueueAsync_RejectsDisabledNode()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            await new ControlPlaneNodeService(context).DisableAsync(node.PublicId);
            var service = new ControlPlaneActionService(context);

            var result = await service.EnqueueAsync(new EnqueueControlActionRequest(node.PublicId, "probe_health"));

            Assert.False(result.Success);
            Assert.Equal(ApplicationErrorKind.Validation, result.Failure);
            Assert.Empty(context.Actions);
        }

        [Fact]
        public async Task EnqueueAsync_LinksPlaceholderStoreSyncToOwningNode()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var store = await CreateStoreAsync(context, node.PublicId, "main-store");
            var service = new ControlPlaneActionService(context);

            var result = await service.EnqueueAsync(new EnqueueControlActionRequest(
                node.PublicId,
                "sync_store_placeholder",
                IdempotencyKey: "sync-main-store",
                StorePublicId: store.PublicId));

            Assert.True(result.Success);
            Assert.Equal(store.PublicId, result.Payload!.StorePublicId);
            Assert.Equal("main-store", result.Payload.StoreKey);
        }

        [Fact]
        public async Task RecordAttemptAsync_AllowsRetryAfterFailure()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneActionService(context);
            var action = await service.EnqueueAsync(new EnqueueControlActionRequest(node.PublicId, "fetch_capabilities"));

            var failed = await service.RecordAttemptAsync(
                action.Payload!.PublicId,
                new RecordControlActionAttemptRequest("failed", HttpStatusCode: 504, DurationMs: 3000, ErrorCode: "timeout", ErrorMessage: "Node timed out."));
            var succeeded = await service.RecordAttemptAsync(
                action.Payload.PublicId,
                new RecordControlActionAttemptRequest("succeeded", HttpStatusCode: 200, DurationMs: 140, ResponseJson: """{"ok":true}"""));

            Assert.True(failed.Success);
            Assert.True(succeeded.Success);
            Assert.Equal("succeeded", succeeded.Payload!.Status);
            Assert.Equal([1, 2], succeeded.Payload.Attempts.Select(attempt => attempt.AttemptNumber));
            Assert.Equal("""{"ok":true}""", succeeded.Payload.ResultJson);
        }

        [Fact]
        public async Task RecordAttemptAsync_TimeoutFailureIncludesSuggestedFix()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneActionService(context);
            var action = await service.EnqueueAsync(new EnqueueControlActionRequest(node.PublicId, "probe_health"));

            var result = await service.RecordAttemptAsync(
                action.Payload!.PublicId,
                new RecordControlActionAttemptRequest("failed", DurationMs: 10000, ErrorCode: "timeout", ErrorMessage: "Request timed out."));

            Assert.True(result.Success);
            Assert.Equal("failed", result.Payload!.Status);
            Assert.Contains("control API URL", result.Payload.SuggestedFix, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("control API URL", result.Payload.Attempts.Single().SuggestedFix, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CancelAsync_PreventsFurtherAttempts()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneActionService(context);
            var action = await service.EnqueueAsync(new EnqueueControlActionRequest(node.PublicId, "probe_health"));

            var cancelled = await service.CancelAsync(action.Payload!.PublicId);
            var attempt = await service.RecordAttemptAsync(
                action.Payload.PublicId,
                new RecordControlActionAttemptRequest("running"));

            Assert.True(cancelled.Success);
            Assert.Equal("cancelled", cancelled.Payload!.Status);
            Assert.False(attempt.Success);
            Assert.Equal(ApplicationErrorKind.Conflict, attempt.Failure);
        }

        private static async Task<ControlPlaneNodeDetail> CreateNodeAsync(ControlPlaneDbContext context, string nodeKey)
        {
            var nodeService = new ControlPlaneNodeService(context);
            var created = await nodeService.CreateAsync(new CreateControlPlaneNodeRequest(
                nodeKey,
                "test-node-secret",
                nodeKey,
                null,
                $"http://{nodeKey}.example/api/controlpanel"));

            Assert.True(created.Success);
            return created.Payload!;
        }

        private static async Task<ControlPlaneStoreDetail> CreateStoreAsync(ControlPlaneDbContext context, Guid nodePublicId, string storeKey)
        {
            var storeService = new ControlPlaneStoreService(context);
            var created = await storeService.CreateAsync(new CreateControlPlaneStoreRequest(
                storeKey,
                storeKey,
                nodePublicId,
                "{}"));

            Assert.True(created.Success);
            return created.Payload!;
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-actions-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }
}
