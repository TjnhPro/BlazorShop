namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    using Xunit;

    public sealed class CommerceNodePaymentMethodServiceCacheTests
    {
        [Fact]
        public async Task UpdateAsync_InvalidatesPublicConfigurationCache()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
            });
            context.PaymentMethods.Add(new PaymentMethod
            {
                Id = Guid.NewGuid(),
                Key = PaymentMethodKeys.Cod,
                Name = "Cash on Delivery",
            });
            context.StorePaymentMethods.Add(new StorePaymentMethod
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                PaymentMethodKey = PaymentMethodKeys.Cod,
                Enabled = true,
                DisplayName = "Cash on Delivery",
                DisplayOrder = 10,
            });
            await context.SaveChangesAsync();

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var publicConfigurationCache = new StorefrontPublicConfigurationCache(context, cache);
            var service = new CommerceNodePaymentMethodService(
                context,
                new StubCommerceStoreContext(storeId),
                new NoopAdminAuditService(),
                publicConfigurationCache,
                new PaymentProviderCapabilityRegistry([new CodStorefrontPaymentProvider()]));

            publicConfigurationCache.Set("default", "cached-config");

            var result = await service.UpdateAsync(
                PaymentMethodKeys.Cod,
                new UpdateStorePaymentMethodRequest(
                    Enabled: true,
                    DisplayName: "COD",
                    Description: "Pay on delivery.",
                    DisplayOrder: 1,
                    SettingsJson: null));

            Assert.True(result.Success, result.Message);
            Assert.False(publicConfigurationCache.TryGet<string>("default", out _));
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"payment-method-cache-{Guid.NewGuid():N}")
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
