namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Shipping;
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

    public sealed class StoreShippingSettingsServiceTests
    {
        [Fact]
        public async Task GetAsync_ReturnsDefensiveDefaultsWithoutPersistingRow()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);

            var result = await service.GetAsync();

            Assert.True(result.Success, result.Message);
            Assert.Equal(Guid.Empty, result.Payload!.PublicId);
            Assert.Equal(StoreShippingSurchargePolicies.Sum, result.Payload.SurchargePolicy);
            Assert.Empty(result.Payload.EnabledCountryCodes);
            Assert.Null(result.Payload.DefaultFlatRate);
            Assert.Empty(context.StoreShippingSettings);
        }

        [Fact]
        public async Task UpdateAsync_RejectsInvalidCountryMoneyAndPolicy()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);

            var invalidCountry = await service.UpdateAsync(CreateUpdateRequest(enabledCountryCodes: ["usa"]));
            var invalidFlatRate = await service.UpdateAsync(CreateUpdateRequest(defaultFlatRate: -1m));
            var invalidThreshold = await service.UpdateAsync(CreateUpdateRequest(freeShippingThreshold: -1m));
            var invalidPolicy = await service.UpdateAsync(CreateUpdateRequest(surchargePolicy: "unknown"));

            Assert.Equal(ServiceResponseType.ValidationError, invalidCountry.ResponseType);
            Assert.Equal(ServiceResponseType.ValidationError, invalidFlatRate.ResponseType);
            Assert.Equal(ServiceResponseType.ValidationError, invalidThreshold.ResponseType);
            Assert.Equal(ServiceResponseType.ValidationError, invalidPolicy.ResponseType);
            Assert.Empty(context.StoreShippingSettings);
        }

        [Fact]
        public async Task UpdateAsync_StoresNormalizedSettingsAndAudits()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var auditService = new CapturingAdminAuditService();
            var service = CreateService(context, storeId, auditService);

            var result = await service.UpdateAsync(CreateUpdateRequest(
                enabledCountryCodes: ["us", "CA", "US"],
                defaultFlatRate: 7.5m,
                freeShippingThreshold: 100m,
                surchargePolicy: " Highest ",
                deliveryEstimateText: "3-5 days"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(["US", "CA"], result.Payload!.EnabledCountryCodes);
            Assert.Equal("US", result.Payload.Origin.CountryCode);
            Assert.Equal(StoreShippingSurchargePolicies.Highest, result.Payload.SurchargePolicy);
            Assert.Equal("actor-1", result.Payload.UpdatedByUserId);

            var settings = Assert.Single(context.StoreShippingSettings);
            Assert.Equal("""["US","CA"]""", settings.EnabledCountryCodesJson);
            Assert.Equal(7.5m, settings.DefaultFlatRate);
            Assert.Equal(100m, settings.FreeShippingThreshold);
            Assert.Single(auditService.Logs);
            Assert.Equal("Shipping.SettingsUpdated", auditService.Logs[0].Action);
        }

        [Fact]
        public async Task FlatRateProvider_UsesConfiguredRateAndFreeShippingThreshold()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);
            await service.UpdateAsync(CreateUpdateRequest(defaultFlatRate: 10m, freeShippingThreshold: 50m));
            var provider = new InternalFlatRateShippingProvider(service);

            var paid = await provider.GetOptionsAsync(CreateOptionsRequest(storeId, subtotal: 40m));
            var free = await provider.GetOptionsAsync(CreateOptionsRequest(storeId, subtotal: 50m));

            var paidOption = Assert.Single(paid.Options);
            var freeOption = Assert.Single(free.Options);
            Assert.Equal(10m, paidOption.Rate);
            Assert.Equal("store.flat_rate", paidOption.RuleMatch);
            Assert.Equal(0m, freeOption.Rate);
            Assert.Equal("store.free_shipping_threshold", freeOption.RuleMatch);
        }

        [Fact]
        public async Task FreeStandardProvider_RejectsUnavailableCountry_WhenSettingsRestrictCountries()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId);
            var service = CreateService(context, storeId);
            await service.UpdateAsync(CreateUpdateRequest(enabledCountryCodes: ["CA"]));
            var provider = new InternalFreeStandardShippingProvider(service);

            var result = await provider.GetOptionsAsync(CreateOptionsRequest(storeId, countryCode: "US"));

            Assert.Empty(result.Options);
            Assert.Contains("Shipping is not available", result.Errors.Single());
        }

        private static UpdateStoreShippingSettingsRequest CreateUpdateRequest(
            IReadOnlyList<string>? enabledCountryCodes = null,
            decimal? defaultFlatRate = null,
            decimal? freeShippingThreshold = null,
            string surchargePolicy = StoreShippingSurchargePolicies.Sum,
            string? deliveryEstimateText = null)
        {
            return new UpdateStoreShippingSettingsRequest(
                new StoreShippingOriginDto(
                    FullName: "Fulfillment",
                    Company: "Main Store",
                    Address1: "1 Shipping Way",
                    Address2: null,
                    City: "Austin",
                    StateProvinceCode: "tx",
                    PostalCode: "78701",
                    CountryCode: "us"),
                enabledCountryCodes,
                defaultFlatRate,
                freeShippingThreshold,
                surchargePolicy,
                deliveryEstimateText);
        }

        private static ShippingOptionsRequest CreateOptionsRequest(
            Guid storeId,
            decimal subtotal = 25m,
            string countryCode = "US")
        {
            return new ShippingOptionsRequest(
                storeId,
                CartId: Guid.NewGuid(),
                CartPublicId: Guid.NewGuid(),
                new ShippingAddressSnapshot(
                    FullName: "Jane Customer",
                    Company: null,
                    Address1: "10 Market St",
                    Address2: null,
                    City: "Austin",
                    StateProvinceCode: "TX",
                    PostalCode: "78701",
                    CountryCode: countryCode,
                    Phone: null,
                    Email: null),
                CurrencyCode: "usd",
                subtotal,
                [
                    new ShippingPackageLine(
                        ProductId: Guid.NewGuid(),
                        ProductVariantId: null,
                        Quantity: 1,
                        ShippingRequired: true,
                        FreeShipping: false)
                ]);
        }

        private static StoreShippingSettingsService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            CapturingAdminAuditService? auditService = null)
        {
            return new StoreShippingSettingsService(
                context,
                new StubCommerceStoreContext(storeId),
                new StubCommerceNodeAuditActorAccessor(),
                auditService ?? new CapturingAdminAuditService());
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
                .UseInMemoryDatabase($"store-shipping-settings-{Guid.NewGuid():N}")
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

        private sealed class StubCommerceNodeAuditActorAccessor : ICommerceNodeAuditActorAccessor
        {
            public CommerceNodeAuditActor GetCurrentActor()
            {
                return new CommerceNodeAuditActor("actor-1", "actor@example.test", "action-1", "127.0.0.1", "tests");
            }
        }

        private sealed class CapturingAdminAuditService : IAdminAuditService
        {
            public List<CreateAdminAuditLogDto> Logs { get; } = [];

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
                this.Logs.Add(request);
                return Task.FromResult(new ServiceResponse<AdminAuditLogDto>(true, "Audit logged."));
            }
        }
    }
}
