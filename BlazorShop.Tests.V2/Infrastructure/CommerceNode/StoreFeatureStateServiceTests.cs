namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    using Xunit;

    public sealed class StoreFeatureStateServiceTests
    {
        [Fact]
        public async Task GetAsync_ReturnsAllowlistedDefaultsWithoutPersistingRows()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);

            var features = await service.GetAsync();

            Assert.Equal(5, features.Count);
            Assert.All(features, feature => Assert.True(StoreFeatureKeys.All.Contains(feature.FeatureKey)));
            Assert.Contains(features, feature => feature.FeatureKey == StoreFeatureKeys.Reviews && !feature.PubliclyVisible);
            Assert.All(features, feature => Assert.True(feature.Enabled));
            Assert.Empty(context.StoreFeatureStates);
        }

        [Fact]
        public async Task UpdateAsync_RejectsUnknownFeatureKey()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);

            var result = await service.UpdateAsync(
                "unknown",
                new UpdateStoreFeatureStateRequest(Enabled: false, Reason: null));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Empty(context.StoreFeatureStates);
        }

        [Fact]
        public async Task UpdateAsync_StoresStateInvalidatesPublicConfigurationAndAffectsSnapshot()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var publicConfigurationCache = new StorefrontPublicConfigurationCache(context, memoryCache);
            publicConfigurationCache.Set("default", "cached-config");
            var service = CreateService(context, storeId, publicConfigurationCache);

            var result = await service.UpdateAsync(
                StoreFeatureKeys.Checkout,
                new UpdateStoreFeatureStateRequest(Enabled: false, Reason: "Temporarily disabled."));
            var snapshot = await service.ResolveAsync(storeId);

            Assert.True(result.Success, result.Message);
            Assert.False(result.Payload!.Enabled);
            Assert.False(snapshot.CheckoutEnabled);
            Assert.True(snapshot.CustomerAccountsEnabled);
            Assert.False(await service.IsEnabledAsync(storeId, StoreFeatureKeys.Checkout));
            Assert.False(publicConfigurationCache.TryGet<string>("default", out _));
            Assert.Single(context.StoreFeatureStates);
        }

        private static StoreFeatureStateService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            StorefrontPublicConfigurationCache? publicConfigurationCache = null)
        {
            publicConfigurationCache ??= new StorefrontPublicConfigurationCache(
                context,
                new MemoryCache(new MemoryCacheOptions()));

            return new StoreFeatureStateService(
                context,
                new StubCommerceStoreContext(storeId),
                new NoopAdminAuditService(),
                publicConfigurationCache);
        }

        private static void SeedStore(CommerceNodeDbContext context, Guid storeId)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
            });
            context.SaveChanges();
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"store-feature-states-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class StubCommerceStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public StubCommerceStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<ApplicationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<ApplicationResult<Guid>> GetCurrentStoreIdAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ApplicationResult<Guid>(true, "Store resolved.", this.storeId));
            }
        }

        private sealed class NoopAdminAuditService : IAdminAuditService
        {
            public Task<PagedResult<AdminAuditLogDto>> GetAsync(AdminAuditQueryDto query)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> GetByIdAsync(Guid id)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> LogAsync(CreateAdminAuditLogDto request)
            {
                return Task.FromResult(new ServiceResponse<AdminAuditLogDto>(true, "Audit logged."));
            }
        }
    }
}
