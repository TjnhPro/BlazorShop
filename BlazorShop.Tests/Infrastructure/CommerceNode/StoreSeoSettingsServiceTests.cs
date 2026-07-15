namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Validations;
    using BlazorShop.Application.Validations.Seo;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    using Xunit;

    public sealed class StoreSeoSettingsServiceTests
    {
        [Fact]
        public async Task ResolveAsync_WhenNoStoreOverride_ReturnsGlobalSettingsAndCachesResult()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var global = new StubSeoSettingsService(new SeoSettingsDto { SiteName = "Global SEO" });
            var service = CreateService(context, storeId, global, cache);

            var first = await service.ResolveAsync();
            var second = await service.ResolveAsync();

            Assert.Equal("Global SEO", first.SiteName);
            Assert.Equal("Global SEO", second.SiteName);
            Assert.Equal(1, global.GetCurrentCallCount);
        }

        [Fact]
        public async Task SaveOverrideAsync_ValidatesAndResolvedOverrideWinsOverGlobalFallback()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var global = new StubSeoSettingsService(new SeoSettingsDto { SiteName = "Global SEO" });
            var service = CreateService(context, storeId, global, cache);

            var invalid = await service.SaveOverrideAsync(new UpdateSeoSettingsDto
            {
                BaseCanonicalUrl = "not-a-url",
            });

            Assert.False(invalid.Success);
            Assert.Equal(ServiceResponseType.ValidationError, invalid.ResponseType);

            var saved = await service.SaveOverrideAsync(new UpdateSeoSettingsDto
            {
                SiteName = " Store SEO ",
                BaseCanonicalUrl = "https://store.example",
            });
            var resolved = await service.ResolveAsync();

            Assert.True(saved.Success);
            Assert.Equal("Store SEO", saved.Payload?.SiteName);
            Assert.Equal("Store SEO", resolved.SiteName);
            Assert.Equal("https://store.example", resolved.BaseCanonicalUrl);
        }

        [Fact]
        public async Task SaveOverrideAsync_InvalidatesCachedStoreSettings()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = CreateService(context, storeId, new StubSeoSettingsService(new SeoSettingsDto()), cache);

            await service.SaveOverrideAsync(new UpdateSeoSettingsDto { SiteName = "Before" });
            var before = await service.ResolveAsync();

            await service.SaveOverrideAsync(new UpdateSeoSettingsDto { SiteName = "After" });
            var after = await service.ResolveAsync();

            Assert.Equal("Before", before.SiteName);
            Assert.Equal("After", after.SiteName);
        }

        private static IStoreSeoSettingsService CreateService(
            CommerceNodeDbContext context,
            Guid storeId,
            ISeoSettingsService globalSeoSettingsService,
            IMemoryCache cache)
        {
            return new StoreSeoSettingsService(
                context,
                new StubCommerceStoreContext(storeId),
                globalSeoSettingsService,
                new ValidationService(),
                new UpdateSeoSettingsDtoValidator(),
                cache);
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"store-seo-settings-{Guid.NewGuid():N}")
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

        private sealed class StubSeoSettingsService : ISeoSettingsService
        {
            private readonly SeoSettingsDto settings;

            public StubSeoSettingsService(SeoSettingsDto settings)
            {
                this.settings = settings;
            }

            public int GetCurrentCallCount { get; private set; }

            public Task<SeoSettingsDto> GetCurrentAsync()
            {
                this.GetCurrentCallCount++;
                return Task.FromResult(this.settings);
            }

            public Task<ServiceResponse<SeoSettingsDto>> UpdateAsync(UpdateSeoSettingsDto request)
            {
                throw new NotSupportedException();
            }
        }
    }
}
