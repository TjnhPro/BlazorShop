namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    using Xunit;

    public sealed class StoreCurrencyServiceTests
    {
        [Fact]
        public async Task GetAsync_CreatesBaseCurrencyRowFromStoreDefault()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, defaultCurrencyCode: "EUR", defaultCulture: "en-GB");
            var service = CreateService(context, storeId);

            var currencies = await service.GetAsync();

            var currency = Assert.Single(currencies);
            Assert.Equal("EUR", currency.CurrencyCode);
            Assert.True(currency.IsBaseCurrency);
            Assert.True(currency.IsEnabled);
            Assert.True(currency.IsDefaultDisplayCurrency);
            Assert.Equal("en-GB", currency.CultureName);
            Assert.Single(context.StoreCurrencies);
        }

        [Fact]
        public async Task UpdateAsync_KeepsBaseCurrencyEnabledAndInvalidatesPublicConfiguration()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, defaultCurrencyCode: "USD", defaultCulture: "en-US");
            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var publicConfigurationCache = new StorefrontPublicConfigurationCache(context, memoryCache);
            publicConfigurationCache.Set("default", "cached-config");
            var service = CreateService(context, storeId, publicConfigurationCache);

            var result = await service.UpdateAsync(
                "usd",
                new UpdateStoreCurrencyRequest(
                    IsEnabled: false,
                    IsDefaultDisplayCurrency: false,
                    DecimalDigits: 2),
                CancellationToken.None);

            Assert.True(result.Success, result.Message);
            Assert.True(result.Payload!.IsEnabled);
            Assert.True(result.Payload.IsDefaultDisplayCurrency);
            Assert.True(result.Payload.IsBaseCurrency);
            Assert.False(publicConfigurationCache.TryGet<string>("default", out _));
        }

        [Fact]
        public async Task ResolveSupportedCurrencyCodesAsync_ReturnsEnabledCurrenciesWithBaseFirst()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, defaultCurrencyCode: "USD", defaultCulture: "en-US");
            context.StoreCurrencies.AddRange(
                new StoreCurrency
                {
                    StoreId = storeId,
                    CurrencyCode = "EUR",
                    IsEnabled = true,
                    DisplayOrder = 20,
                },
                new StoreCurrency
                {
                    StoreId = storeId,
                    CurrencyCode = "JPY",
                    IsEnabled = false,
                    DisplayOrder = 10,
                });
            await context.SaveChangesAsync();
            var service = CreateService(context, storeId);

            var codes = await service.ResolveSupportedCurrencyCodesAsync(storeId);

            Assert.Equal(["USD", "EUR"], codes);
        }

        private static StoreCurrencyService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            StorefrontPublicConfigurationCache? publicConfigurationCache = null)
        {
            publicConfigurationCache ??= new StorefrontPublicConfigurationCache(
                context,
                new MemoryCache(new MemoryCacheOptions()));

            return new StoreCurrencyService(
                context,
                new StubCommerceStoreContext(storeId),
                new NoopAdminAuditService(),
                publicConfigurationCache);
        }

        private static void SeedStore(
            CommerceNodeDbContext context,
            Guid storeId,
            string defaultCurrencyCode,
            string defaultCulture)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
                DefaultCurrencyCode = defaultCurrencyCode,
                DefaultCulture = defaultCulture,
            });
            context.SaveChanges();
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"store-currencies-{Guid.NewGuid():N}")
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
