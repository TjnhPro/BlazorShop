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
        public async Task UpdateLineSnapshotsAsync_IncrementsVersionOnlyWhenSnapshotsChange()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var created = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId));
            var withLine = await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                created.Payload!.Token,
                productId,
                Quantity: 1,
                UnitPriceSnapshot: 10m,
                CurrencyCodeSnapshot: "usd",
                BaseUnitPriceSnapshot: 10m,
                BaseCurrencyCodeSnapshot: "usd"));
            var lineId = withLine.Payload!.Lines.Single().Id;

            var unchanged = await service.UpdateLineSnapshotsAsync(
                storeId,
                created.Payload.Token,
                [
                    new StorefrontCartLineSnapshotUpdate(
                        lineId,
                        UnitPriceSnapshot: 10m,
                        CurrencyCodeSnapshot: "usd",
                        BaseUnitPriceSnapshot: 10m,
                        BaseCurrencyCodeSnapshot: "usd",
                        ExchangeRateSnapshot: null,
                        ExchangeRateProviderKey: null,
                        ExchangeRateSource: null,
                        ExchangeRateEffectiveAtUtc: null,
                        ExchangeRateExpiresAtUtc: null),
                ]);
            var changed = await service.UpdateLineSnapshotsAsync(
                storeId,
                created.Payload.Token,
                [
                    new StorefrontCartLineSnapshotUpdate(
                        lineId,
                        UnitPriceSnapshot: 12m,
                        CurrencyCodeSnapshot: "eur",
                        BaseUnitPriceSnapshot: 10m,
                        BaseCurrencyCodeSnapshot: "usd",
                        ExchangeRateSnapshot: 1.2m,
                        ExchangeRateProviderKey: "manual",
                        ExchangeRateSource: "test",
                        ExchangeRateEffectiveAtUtc: DateTimeOffset.Parse("2026-07-16T00:00:00Z"),
                        ExchangeRateExpiresAtUtc: null),
                ]);

            Assert.True(unchanged.Success);
            Assert.Equal(2, unchanged.Payload!.Version);
            Assert.True(changed.Success);
            Assert.Equal(3, changed.Payload!.Version);
            var line = Assert.Single(changed.Payload.Lines);
            Assert.Equal(12m, line.UnitPriceSnapshot);
            Assert.Equal("EUR", line.CurrencyCodeSnapshot);
            Assert.Equal(10m, line.BaseUnitPriceSnapshot);
            Assert.Equal("USD", line.BaseCurrencyCodeSnapshot);
            Assert.Equal(1.2m, line.ExchangeRateSnapshot);
            Assert.Equal("manual", line.ExchangeRateProviderKey);
            Assert.Equal("test", line.ExchangeRateSource);
        }

        [Fact]
        public async Task AttachOrMergeCurrentCustomerAsync_WhenNoCustomerCart_AttachesCurrentTokenCart()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();
            var created = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId));

            var result = await service.AttachOrMergeCurrentCustomerAsync(
                new StorefrontCartAttachCurrentCustomerRequest(
                    storeId,
                    created.Payload!.Token,
                    AppUserId: "user-1"));

            Assert.True(result.Success);
            Assert.Equal("user-1", result.Payload!.AppUserId);
            Assert.Equal(CartSessionStates.Active, result.Payload.State);
            Assert.Equal(2, result.Payload.Version);
        }

        [Fact]
        public async Task AttachOrMergeCurrentCustomerAsync_WhenCustomerCartExists_MergesIntoCurrentTokenCart()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();
            var sameProductId = Guid.NewGuid();
            var guest = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId));
            var customer = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId, AppUserId: "user-1"));
            await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                guest.Payload!.Token,
                sameProductId,
                Quantity: 1,
                UnitPriceSnapshot: 10m,
                CurrencyCodeSnapshot: "usd"));
            await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                customer.Payload!.Token,
                sameProductId,
                Quantity: 2,
                UnitPriceSnapshot: 9m,
                CurrencyCodeSnapshot: "usd"));
            await service.AddOrUpdateLineAsync(new StorefrontCartLineMutationRequest(
                storeId,
                customer.Payload.Token,
                sameProductId,
                SelectedAttributesJson: """[{"name":"Color","value":"Red"}]""",
                Quantity: 1,
                UnitPriceSnapshot: 11m,
                CurrencyCodeSnapshot: "usd"));

            var merged = await service.AttachOrMergeCurrentCustomerAsync(
                new StorefrontCartAttachCurrentCustomerRequest(
                    storeId,
                    guest.Payload.Token,
                    AppUserId: "user-1"));

            Assert.True(merged.Success);
            Assert.Equal(2, merged.Payload!.Lines.Count);
            Assert.Equal("user-1", merged.Payload.AppUserId);
            Assert.Contains(merged.Payload.Lines, line => line.ProductId == sameProductId && line.Quantity == 3 && line.SelectedAttributesJson is null);
            Assert.Contains(merged.Payload.Lines, line => line.ProductId == sameProductId && line.Quantity == 1 && line.SelectedAttributesJson is not null);
            var oldCustomerCart = await context.CartSessions.SingleAsync(cart => cart.Id == customer.Payload.Id);
            Assert.Equal(CartSessionStates.Merged, oldCustomerCart.State);
            Assert.Equal(guest.Payload.Id, oldCustomerCart.MergedIntoCartId);

            var oldCustomerTokenResult = await service.ResolveAsync(storeId, customer.Payload.Token);
            Assert.False(oldCustomerTokenResult.Success);
            Assert.Equal(ServiceResponseType.Conflict, oldCustomerTokenResult.ResponseType);
        }

        [Fact]
        public async Task AttachOrMergeCurrentCustomerAsync_WhenTokenBelongsToAnotherCustomer_ReturnsConflict()
        {
            await using var context = CreateContext();
            var service = new StorefrontCartSessionService(context);
            var storeId = Guid.NewGuid();
            var cart = await service.CreateAsync(new StorefrontCartSessionCreateRequest(storeId, AppUserId: "user-1"));

            var result = await service.AttachOrMergeCurrentCustomerAsync(
                new StorefrontCartAttachCurrentCustomerRequest(
                    storeId,
                    cart.Payload!.Token,
                    AppUserId: "user-2"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Equal("user-1", (await context.CartSessions.SingleAsync()).AppUserId);
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
