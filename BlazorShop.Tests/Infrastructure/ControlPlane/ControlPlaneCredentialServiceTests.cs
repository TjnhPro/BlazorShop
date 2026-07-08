namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using BlazorShop.Application.ControlPlane.Credentials;
    using BlazorShop.Application.ControlPlane.Nodes;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public class ControlPlaneCredentialServiceTests
    {
        [Fact]
        public async Task CreateAsync_ReturnsRawSecretOnceAndPersistsOnlyHash()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = new ControlPlaneCredentialService(context);

            var created = await service.CreateAsync(node.PublicId, actorAdminUserId: 42);
            var listed = await service.ListAsync(node.PublicId);
            var persisted = await context.NodeCredentials.SingleAsync();

            Assert.True(created.Success);
            Assert.StartsWith("bs_cp_", created.Payload!.RawSecret, StringComparison.Ordinal);
            Assert.DoesNotContain(created.Payload.RawSecret, persisted.SecretHash, StringComparison.Ordinal);
            Assert.Equal(created.Payload.Credential.KeyId, persisted.KeyId);
            Assert.Equal("sha256", persisted.HashAlgorithm);
            Assert.Equal(42, persisted.CreatedByAdminUserId);
            Assert.True(listed.Success);
            Assert.Single(listed.Payload!.Items);
        }

        [Fact]
        public async Task VerifyAsync_ReturnsFalseAfterRevoke()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = new ControlPlaneCredentialService(context);
            var created = await service.CreateAsync(node.PublicId);

            var beforeRevoke = await service.VerifyAsync(created.Payload!.Credential.KeyId, created.Payload.RawSecret);
            await service.RevokeAsync(node.PublicId, created.Payload.Credential.KeyId, actorAdminUserId: 42);
            var afterRevoke = await service.VerifyAsync(created.Payload.Credential.KeyId, created.Payload.RawSecret);

            Assert.True(beforeRevoke);
            Assert.False(afterRevoke);
        }

        [Fact]
        public async Task RotateAsync_DisablesOldCredentialAndReturnsNewSecret()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var service = new ControlPlaneCredentialService(context);
            var created = await service.CreateAsync(node.PublicId);

            var rotated = await service.RotateAsync(node.PublicId, created.Payload!.Credential.KeyId, actorAdminUserId: 42);
            var oldCredentialWorks = await service.VerifyAsync(created.Payload.Credential.KeyId, created.Payload.RawSecret);
            var newCredentialWorks = await service.VerifyAsync(rotated.Payload!.Credential.KeyId, rotated.Payload.RawSecret);

            Assert.True(rotated.Success);
            Assert.NotEqual(created.Payload.Credential.KeyId, rotated.Payload.Credential.KeyId);
            Assert.False(oldCredentialWorks);
            Assert.True(newCredentialWorks);
            Assert.Equal(2, await context.NodeCredentials.CountAsync());
            Assert.Contains(await context.NodeCredentials.ToListAsync(), credential => credential.Status == "rotated" && credential.RevokedByAdminUserId == 42);
        }

        [Fact]
        public async Task CreateAsync_RejectsDisabledNode()
        {
            await using var context = CreateContext();
            var node = await CreateNodeAsync(context);
            var nodeService = new ControlPlaneNodeService(context);
            await nodeService.DisableAsync(node.PublicId);
            var service = new ControlPlaneCredentialService(context);

            var result = await service.CreateAsync(node.PublicId);

            Assert.False(result.Success);
            Assert.Equal(ControlPlaneCredentialOperationFailure.Validation, result.Failure);
        }

        private static async Task<ControlPlaneNodeDetail> CreateNodeAsync(ControlPlaneDbContext context)
        {
            var nodeService = new ControlPlaneNodeService(context);
            var nodeKey = ($"node-{Guid.NewGuid():N}")[..12];
            var created = await nodeService.CreateAsync(new CreateControlPlaneNodeRequest(
                nodeKey,
                "Test Node",
                null,
                "http://localhost:5180/api/controlpanel"));

            Assert.True(created.Success);
            return created.Payload!;
        }

        private static ControlPlaneDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ControlPlaneDbContext>()
                .UseInMemoryDatabase($"control-plane-credentials-{Guid.NewGuid():N}")
                .Options;

            return new ControlPlaneDbContext(options);
        }
    }
}
