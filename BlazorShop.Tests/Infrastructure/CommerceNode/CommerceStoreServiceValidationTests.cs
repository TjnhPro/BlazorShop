namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    using Xunit;

    public sealed class CommerceStoreServiceValidationTests
    {
        [Fact]
        public async Task CreateAsync_AcceptsSafeAbsoluteAndRootRelativeAssetUrls()
        {
            await using var context = CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new CommerceStoreService(context, cache);

            var result = await service.CreateAsync(CreateRequest(
                logoUrl: "https://cdn.example.test/logo.png",
                faviconUrl: "/media/assets/favicon.ico",
                pngIconUrl: "/media/assets/icon-192.png",
                appleTouchIconUrl: "https://cdn.example.test/apple.png",
                msTileImageUrl: "/media/assets/tile.png",
                msTileColor: "#0f766e",
                cdnHost: "cdn.example.test"));

            Assert.True(result.Success, result.Message);
            Assert.Equal("https://cdn.example.test/logo.png", result.Payload?.LogoUrl);
            Assert.Equal("/media/assets/favicon.ico", result.Payload?.FaviconUrl);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("data:image/svg+xml;base64,PHN2Zy8+")]
        [InlineData("//cdn.example.test/logo.png")]
        [InlineData("relative/logo.png")]
        [InlineData("/media\\logo.png")]
        public async Task CreateAsync_RejectsUnsafeAssetUrls(string logoUrl)
        {
            await using var context = CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new CommerceStoreService(context, cache);

            var result = await service.CreateAsync(CreateRequest(logoUrl: logoUrl));

            Assert.False(result.Success);
            Assert.Equal(CommerceStoreOperationFailure.Validation, result.Failure);
            Assert.Contains("Logo URL", result.Message);
        }

        [Theory]
        [InlineData("expression(alert(1))")]
        [InlineData("red")]
        [InlineData("#12")]
        [InlineData("#xyzxyz")]
        public async Task CreateAsync_RejectsUnsafeMsTileColor(string msTileColor)
        {
            await using var context = CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new CommerceStoreService(context, cache);

            var result = await service.CreateAsync(CreateRequest(msTileColor: msTileColor));

            Assert.False(result.Success);
            Assert.Equal(CommerceStoreOperationFailure.Validation, result.Failure);
            Assert.Contains("MS tile color", result.Message);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("cdn example.test")]
        [InlineData("cdn.example.test:99999")]
        [InlineData("https://")]
        public async Task CreateAsync_RejectsMalformedCdnHost(string cdnHost)
        {
            await using var context = CreateContext();
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new CommerceStoreService(context, cache);

            var result = await service.CreateAsync(CreateRequest(cdnHost: cdnHost));

            Assert.False(result.Success);
            Assert.Equal(CommerceStoreOperationFailure.Validation, result.Failure);
            Assert.Contains("CDN host", result.Message);
        }

        private static CreateCommerceStoreRequest CreateRequest(
            string storeKey = "demo-store",
            string? cdnHost = null,
            string? logoUrl = null,
            string? faviconUrl = null,
            string? pngIconUrl = null,
            string? appleTouchIconUrl = null,
            string? msTileImageUrl = null,
            string? msTileColor = null)
        {
            return new CreateCommerceStoreRequest(
                StoreKey: $"{storeKey}-{Guid.NewGuid():N}"[..24],
                Name: "Demo Store",
                BaseUrl: "https://store.example.test",
                CdnHost: cdnHost,
                LogoUrl: logoUrl,
                FaviconUrl: faviconUrl,
                PngIconUrl: pngIconUrl,
                AppleTouchIconUrl: appleTouchIconUrl,
                MsTileImageUrl: msTileImageUrl,
                MsTileColor: msTileColor,
                DefaultCurrencyCode: "USD",
                DefaultCulture: "en-US");
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"commerce-store-validation-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
