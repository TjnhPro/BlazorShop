namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    using Xunit;

    public sealed class StoreCurrencyExchangeRateProviderServiceTests
    {
        [Fact]
        public async Task GetProvidersAsync_ReturnsSafeProviderStatus()
        {
            await using var context = CreateContext();
            var service = CreateService(
                context,
                Guid.NewGuid(),
                new ConfigurationExchangeRateProviderOptions
                {
                    Enabled = true,
                    Source = "safe-source",
                });

            var providers = await service.GetProvidersAsync();

            Assert.Contains(providers, provider => provider.ProviderKey == "manual"
                && provider.Enabled
                && !provider.SecretsConfigured);
            Assert.Contains(providers, provider => provider.ProviderKey == "configuration"
                && provider.Enabled
                && !provider.SecretsConfigured
                && provider.Source == "safe-source");
        }

        [Fact]
        public async Task FetchAsync_WhenConfigurationProviderHasRate_CreatesProviderRate()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            SeedStoreCurrency(context, storeId, "EUR");
            var service = CreateService(
                context,
                storeId,
                new ConfigurationExchangeRateProviderOptions
                {
                    Enabled = true,
                    Source = "unit-test-provider",
                    Rates =
                    [
                        new ConfigurationExchangeRateEntry
                        {
                            BaseCurrencyCode = "USD",
                            TargetCurrencyCode = "EUR",
                            Rate = 0.91m,
                        },
                    ],
                });

            var result = await service.FetchAsync(new FetchStoreCurrencyExchangeRatesRequest(
                "configuration",
                ["eur"]));

            Assert.True(result.Success, result.Message);
            Assert.Equal("configuration", result.Payload!.ProviderKey);
            Assert.Equal(1, result.Payload.UpdatedCount);
            var rate = Assert.Single(context.StoreCurrencyExchangeRates);
            Assert.Equal("configuration", rate.ProviderKey);
            Assert.False(rate.IsManual);
            Assert.Equal("USD", rate.BaseCurrencyCode);
            Assert.Equal("EUR", rate.TargetCurrencyCode);
            Assert.Equal(0.91m, rate.Rate);
            Assert.Equal("unit-test-provider", rate.Source);
            Assert.True(rate.IsEnabled);
        }

        [Fact]
        public async Task FetchForStoreAsync_WhenConfigurationProviderHasRate_InvalidatesPublicConfigurationCache()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            SeedStoreCurrency(context, storeId, "EUR");
            using var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var publicConfigurationCache = new StorefrontPublicConfigurationCache(context, memoryCache);
            publicConfigurationCache.Set("default", "cached-config");
            var service = CreateService(
                context,
                Guid.NewGuid(),
                new ConfigurationExchangeRateProviderOptions
                {
                    Enabled = true,
                    Rates =
                    [
                        new ConfigurationExchangeRateEntry
                        {
                            BaseCurrencyCode = "USD",
                            TargetCurrencyCode = "EUR",
                            Rate = 0.91m,
                        },
                    ],
                },
                publicConfigurationCache: publicConfigurationCache);

            var result = await service.FetchForStoreAsync(
                storeId,
                new FetchStoreCurrencyExchangeRatesRequest("configuration", ["EUR"]));

            Assert.True(result.Success, result.Message);
            Assert.False(publicConfigurationCache.TryGet<string>("default", out _));
        }

        [Fact]
        public async Task QueueUpdateAsync_WhenProviderAndTargetsAreValid_QueuesCommerceTask()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            SeedStoreCurrency(context, storeId, "EUR");
            var service = CreateService(
                context,
                storeId,
                new ConfigurationExchangeRateProviderOptions
                {
                    Enabled = true,
                });

            var result = await service.QueueUpdateAsync(new QueueStoreCurrencyExchangeRateUpdateRequest(
                "configuration",
                ["eur"],
                IdempotencyKey: "currency-sync",
                CorrelationId: "unit-test"));

            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Payload);
            var task = Assert.Single(context.CommerceTasks);
            Assert.Equal(CurrencyExchangeRateTaskTypes.Update, task.TaskType);
            Assert.Equal($"currency-exchange-rate:{storeId:D}:configuration", task.LockKey);
            Assert.Equal("currency-sync", task.IdempotencyKey);
            Assert.Equal("unit-test", task.CorrelationId);
            Assert.Equal(3, task.MaxAttempts);
            Assert.Contains("\"storeId\"", task.PayloadJson);
            Assert.Contains(storeId.ToString("D"), task.PayloadJson);
            Assert.Contains("\"providerKey\":\"configuration\"", task.PayloadJson);
            Assert.Contains("\"targetCurrencyCodes\":[\"EUR\"]", task.PayloadJson);
        }

        [Fact]
        public async Task FetchAsync_WhenConfigurationRateIsStale_ReturnsConflict()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            SeedStoreCurrency(context, storeId, "EUR");
            var service = CreateService(
                context,
                storeId,
                new ConfigurationExchangeRateProviderOptions
                {
                    Enabled = true,
                    MaxRateAgeHours = 1,
                    Rates =
                    [
                        new ConfigurationExchangeRateEntry
                        {
                            BaseCurrencyCode = "USD",
                            TargetCurrencyCode = "EUR",
                            Rate = 0.91m,
                            EffectiveAt = DateTimeOffset.UtcNow.AddHours(-2),
                        },
                    ],
                });

            var result = await service.FetchAsync(new FetchStoreCurrencyExchangeRatesRequest(
                "configuration",
                ["EUR"]));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
            Assert.Contains("stale", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(context.StoreCurrencyExchangeRates);
        }

        private static StoreCurrencyExchangeRateProviderService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            ConfigurationExchangeRateProviderOptions options,
            ICommerceTaskService? taskService = null,
            IStorefrontPublicConfigurationCache? publicConfigurationCache = null)
        {
            IExchangeRateProvider[] providers =
            [
                new ManualExchangeRateProvider(),
                new ConfigurationExchangeRateProvider(new StaticOptionsMonitor<ConfigurationExchangeRateProviderOptions>(options)),
            ];

            return new StoreCurrencyExchangeRateProviderService(
                context,
                new StubCommerceStoreContext(storeId),
                new NoopAdminAuditService(),
                taskService ?? new CommerceTaskService(context),
                publicConfigurationCache ?? new StorefrontPublicConfigurationCache(context, new MemoryCache(new MemoryCacheOptions())),
                providers);
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
                .UseInMemoryDatabase($"store-currency-provider-rates-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class StaticOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
        {
            private readonly TOptions value;

            public StaticOptionsMonitor(TOptions value)
            {
                this.value = value;
            }

            public TOptions CurrentValue => this.value;

            public TOptions Get(string? name)
            {
                return this.value;
            }

            public IDisposable? OnChange(Action<TOptions, string?> listener)
            {
                return null;
            }
        }

        private sealed class StubCommerceStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public StubCommerceStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Store resolved.", this.storeId));
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
