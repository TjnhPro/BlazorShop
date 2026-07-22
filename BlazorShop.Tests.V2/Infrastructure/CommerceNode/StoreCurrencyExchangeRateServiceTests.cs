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

    using Xunit;

    public sealed class StoreCurrencyExchangeRateServiceTests
    {
        [Fact]
        public async Task UpsertAsync_WhenTargetCurrencyIsEnabled_CreatesManualRate()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            SeedStoreCurrency(context, storeId, "EUR");
            var service = CreateService(context, storeId);

            var result = await service.UpsertAsync(
                "eur",
                new UpsertStoreCurrencyExchangeRateRequest(0.9m, Source: "manual desk"));

            Assert.True(result.Success, result.Message);
            Assert.Equal("USD", result.Payload!.BaseCurrencyCode);
            Assert.Equal("EUR", result.Payload.TargetCurrencyCode);
            Assert.Equal(0.9m, result.Payload.Rate);
            Assert.Equal("manual", result.Payload.ProviderKey);
            Assert.True(result.Payload.IsManual);
            Assert.True(result.Payload.IsEnabled);
            Assert.True(result.Payload.IsActive);
            Assert.Single(context.StoreCurrencyExchangeRates);
        }

        [Fact]
        public async Task UpsertAsync_WhenTargetCurrencyIsNotEnabled_ReturnsValidationError()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);

            var result = await service.UpsertAsync(
                "EUR",
                new UpsertStoreCurrencyExchangeRateRequest(0.9m));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Contains("must be enabled", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.StoreCurrencyExchangeRates);
        }

        [Fact]
        public async Task ConvertFromBaseAsync_WhenActiveRateExists_ConvertsAndRoundsAmount()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            SeedStoreCurrency(context, storeId, "EUR");
            var service = CreateService(context, storeId);
            await service.UpsertAsync(
                "EUR",
                new UpsertStoreCurrencyExchangeRateRequest(0.9234m));

            var result = await service.ConvertFromBaseAsync(storeId, 10.005m, "EUR");

            Assert.True(result.Success, result.Message);
            Assert.Equal("USD", result.Payload!.SourceCurrencyCode);
            Assert.Equal("EUR", result.Payload.TargetCurrencyCode);
            Assert.Equal(0.9234m, result.Payload.Rate);
            Assert.Equal(9.24m, result.Payload.ConvertedAmount);
        }

        [Fact]
        public async Task ConvertFromBaseAsync_WhenTargetIsBaseCurrency_UsesRateOneWithoutRow()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);

            var result = await service.ConvertFromBaseAsync(storeId, 10.005m, "USD");

            Assert.True(result.Success, result.Message);
            Assert.Equal(1m, result.Payload!.Rate);
            Assert.Equal("USD", result.Payload.TargetCurrencyCode);
            Assert.Equal(10.01m, result.Payload.ConvertedAmount);
            Assert.Empty(context.StoreCurrencyExchangeRates);
        }

        [Fact]
        public async Task ConvertFromBaseAsync_WhenRateIsDisabled_ReturnsConflict()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            SeedStoreCurrency(context, storeId, "EUR");
            var service = CreateService(context, storeId);
            await service.UpsertAsync(
                "EUR",
                new UpsertStoreCurrencyExchangeRateRequest(0.9m));

            var disableResult = await service.DisableAsync("EUR");
            var conversionResult = await service.ConvertFromBaseAsync(storeId, 10m, "EUR");

            Assert.True(disableResult.Success, disableResult.Message);
            Assert.False(conversionResult.Success);
            Assert.Equal(ServiceResponseType.Conflict, conversionResult.ResponseType);
            Assert.Contains("No active exchange rate", conversionResult.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static StoreCurrencyExchangeRateService CreateService(
            CommerceNodeDbContext context,
            Guid storeId)
        {
            var metadataService = new CurrencyMetadataService();
            return new StoreCurrencyExchangeRateService(
                context,
                new StubCommerceStoreContext(storeId),
                new NoopAdminAuditService(),
                new MoneyRoundingService(metadataService));
        }

        private static void SeedStore(CommerceNodeDbContext context, Guid storeId)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
                DefaultCurrencyCode = "USD",
                DefaultCulture = "en-US",
            });
            context.SaveChanges();
        }

        private static void SeedStoreCurrency(CommerceNodeDbContext context, Guid storeId, string currencyCode)
        {
            context.StoreCurrencies.Add(new StoreCurrency
            {
                StoreId = storeId,
                CurrencyCode = currencyCode,
                IsEnabled = true,
                DecimalDigits = 2,
            });
            context.SaveChanges();
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"store-currency-rates-{Guid.NewGuid():N}")
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
