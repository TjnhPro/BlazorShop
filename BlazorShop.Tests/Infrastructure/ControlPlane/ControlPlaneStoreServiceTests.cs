namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Application.ControlPlane.Stores;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class ControlPlaneStoreServiceTests
    {
        [Fact]
        public async Task CreateAsync_LinksStoreToActiveNode()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneStoreService(context);

            var result = await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Main Store", node.PublicId, "{}"));

            Assert.True(result.Success);
            Assert.Equal(node.PublicId, result.Payload!.NodePublicId);
            Assert.Equal("main-store", result.Payload.StoreKey);
        }

        [Fact]
        public async Task CreateAsync_RejectsDisabledNode()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            await new ControlPlaneNodeService(context).DisableAsync(node.PublicId);
            var service = new ControlPlaneStoreService(context);

            var result = await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Main Store", node.PublicId, "{}"));

            Assert.False(result.Success);
            Assert.Equal(ControlPlaneStoreOperationFailure.Validation, result.Failure);
        }

        [Fact]
        public async Task CreateAsync_RejectsDuplicateActiveStoreKey()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneStoreService(context);
            await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Main Store", node.PublicId, "{}"));

            var duplicate = await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Duplicate", node.PublicId, "{}"));

            Assert.False(duplicate.Success);
            Assert.Equal(ControlPlaneStoreOperationFailure.Conflict, duplicate.Failure);
        }

        [Fact]
        public async Task AddDomainAsync_EnforcesActiveDomainUniqueness()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneStoreService(context);
            var first = await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Main Store", node.PublicId, "{}"));
            var second = await service.CreateAsync(new CreateControlPlaneStoreRequest("other-store", "Other Store", node.PublicId, "{}"));
            await service.AddDomainAsync(first.Payload!.PublicId, new CreateControlPlaneStoreDomainRequest("Store.Example.com"));

            var duplicate = await service.AddDomainAsync(second.Payload!.PublicId, new CreateControlPlaneStoreDomainRequest("https://store.example.com/path"));

            Assert.False(duplicate.Success);
            Assert.Equal(ControlPlaneStoreOperationFailure.Conflict, duplicate.Failure);
        }

        [Fact]
        public async Task UpdateAsync_ReassignsStoreToAnotherActiveNode()
        {
            await using var context = CreateContext();
            var firstNode = await CreateNodeAsync(context, "node-a");
            var secondNode = await CreateNodeAsync(context, "node-b");
            var service = new ControlPlaneStoreService(context);
            var store = await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Main Store", firstNode.PublicId, "{}"));

            var updated = await service.UpdateAsync(
                store.Payload!.PublicId,
                new UpdateControlPlaneStoreRequest("Renamed Store", secondNode.PublicId, """{"tier":"gold"}"""));

            Assert.True(updated.Success);
            Assert.Equal("Renamed Store", updated.Payload!.Name);
            Assert.Equal(secondNode.PublicId, updated.Payload.NodePublicId);
            Assert.Equal("node-b", updated.Payload.NodeKey);
            Assert.Equal("""{"tier":"gold"}""", updated.Payload.MetadataJson);
        }

        [Fact]
        public async Task DomainPlaceholderFlow_CanVerifyAndDisableDomain()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneStoreService(context);
            var store = await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Main Store", node.PublicId, "{}"));
            var withDomain = await service.AddDomainAsync(store.Payload!.PublicId, new CreateControlPlaneStoreDomainRequest("store.example.com"));
            var domainId = withDomain.Payload!.Domains.Single().Id;

            var verified = await service.VerifyDomainAsync(store.Payload.PublicId, domainId);
            var disabled = await service.DisableDomainAsync(store.Payload.PublicId, domainId);

            Assert.Equal("verified", verified.Payload!.Domains.Single().Status);
            Assert.Equal("disabled", disabled.Payload!.Domains.Single().Status);
            Assert.NotNull(disabled.Payload.Domains.Single().DisabledAt);
        }

        [Fact]
        public async Task ArchiveAsync_HidesStoreFromActiveSelectors()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context, "node-a");
            var service = new ControlPlaneStoreService(context);
            var store = await service.CreateAsync(new CreateControlPlaneStoreRequest("main-store", "Main Store", node.PublicId, "{}"));

            var archived = await service.ArchiveAsync(store.Payload!.PublicId);
            var activeStores = await service.ListAsync(new ControlPlaneStoreListQuery(Status: "active"));

            Assert.Equal("archived", archived.Payload!.Status);
            Assert.Empty(activeStores.Items);
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

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-stores-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }
}
