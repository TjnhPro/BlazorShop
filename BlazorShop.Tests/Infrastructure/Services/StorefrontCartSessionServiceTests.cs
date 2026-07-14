namespace BlazorShop.Tests.Infrastructure.Services
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class StorefrontCartSessionServiceTests
    {
        [Fact]
        public async Task CreateAsync_ReturnsOpaqueToken_AndStoresOnlyHash()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();

            var result = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId));

            Assert.True(result.Success);
            Assert.NotNull(result.Payload);
            Assert.False(string.IsNullOrWhiteSpace(result.Payload!.Token));

            var stored = await context.CartSessions.SingleAsync();
            Assert.Equal(storeId, stored.StoreId);
            Assert.NotEqual(result.Payload.Token, stored.TokenHash);
            Assert.Equal(64, stored.TokenHash.Length);
            Assert.Equal(CartSessionStates.Active, stored.State);
            Assert.Equal(1, stored.Version);
        }

        [Fact]
        public async Task ResolveAsync_ReturnsNotFound_WhenTokenBelongsToDifferentStore()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var created = await service.CreateAsync(new StorefrontCartSessionCreateRequest(Guid.NewGuid()));

            var result = await service.ResolveAsync(Guid.NewGuid(), created.Payload!.Token);

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.NotFound, result.ResponseType);
        }

        [Fact]
        public async Task AddOrUpdateLineAsync_MergesMatchingLine_AndIncrementsVersion()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var created = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId));

            var first = await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                created.Payload!.Token,
                productId,
                Quantity: 1,
                UnitPriceSnapshot: 10m,
                CurrencyCodeSnapshot: "usd"));
            var second = await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                created.Payload.Token,
                productId,
                Quantity: 2,
                UnitPriceSnapshot: 11m,
                CurrencyCodeSnapshot: "eur"));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Single(second.Payload!.Lines);
            Assert.Equal(3, second.Payload.Lines[0].Quantity);
            Assert.Equal(11m, second.Payload.Lines[0].UnitPriceSnapshot);
            Assert.Equal("EUR", second.Payload.Lines[0].CurrencyCodeSnapshot);
            Assert.Equal(3, second.Payload.Version);
            Assert.Equal(1, await context.CartLines.CountAsync());
        }

        [Fact]
        public async Task UpdateAndRemoveLineAsync_MutateLine_AndIncrementVersion()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();
            var created = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId));
            var withLine = await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                created.Payload!.Token,
                Guid.NewGuid(),
                Quantity: 1));
            var lineId = withLine.Payload!.Lines.Single().Id;

            var updated = await service.UpdateLineQuantityAsync(storeId, created.Payload.Token, lineId, 5);
            var removed = await service.RemoveLineAsync(storeId, created.Payload.Token, lineId);

            Assert.True(updated.Success);
            Assert.Equal(5, updated.Payload!.Lines.Single().Quantity);
            Assert.Equal(3, updated.Payload.Version);
            Assert.True(removed.Success);
            Assert.Empty(removed.Payload!.Lines);
            Assert.Equal(4, removed.Payload.Version);
            Assert.Equal(0, await context.CartLines.CountAsync());
        }

        [Fact]
        public async Task AddOrUpdateLineAsync_RejectsExpiredCart_AndMarksSessionExpired()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();
            var created = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId));
            var session = await context.CartSessions.SingleAsync();
            session.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();

            var result = await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                created.Payload!.Token,
                Guid.NewGuid(),
                Quantity: 1));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal(CartSessionStates.Expired, (await context.CartSessions.SingleAsync()).State);
            Assert.Equal(0, await context.CartLines.CountAsync());
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-cart-session-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
