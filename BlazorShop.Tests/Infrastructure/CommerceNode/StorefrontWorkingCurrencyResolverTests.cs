namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class StorefrontWorkingCurrencyResolverTests
    {
        [Fact]
        public async Task ResolveAsync_WhenHintUnsupported_ReturnsBaseCurrency()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, "USD");
            var resolver = CreateResolver(context);

            var resolution = await resolver.ResolveAsync(storeId, "JPY");

            Assert.Equal("USD", resolution.CurrencyCode);
            Assert.Equal("USD", resolution.BaseCurrencyCode);
            Assert.Equal("JPY", resolution.RequestedCurrencyCode);
            Assert.False(resolution.RequestedCurrencySupported);
            Assert.True(resolution.CheckoutCurrencyEnabled);
            Assert.Equal("unsupported", resolution.Reason);
        }

        [Fact]
        public async Task ResolveAsync_WhenHintIsSupportedNonBase_ReturnsBaseUntilConversionIsEnabled()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, "USD");
            context.StoreCurrencies.Add(new StoreCurrency
            {
                StoreId = storeId,
                CurrencyCode = "EUR",
                IsEnabled = true,
            });
            await context.SaveChangesAsync();
            var resolver = CreateResolver(context);

            var resolution = await resolver.ResolveAsync(storeId, "eur");

            Assert.Equal("USD", resolution.CurrencyCode);
            Assert.Equal("USD", resolution.BaseCurrencyCode);
            Assert.Equal("EUR", resolution.RequestedCurrencyCode);
            Assert.True(resolution.RequestedCurrencySupported);
            Assert.False(resolution.CheckoutCurrencyEnabled);
            Assert.Equal("conversion_not_enabled", resolution.Reason);
        }

        [Fact]
        public async Task ResolveAsync_WhenHintIsBaseCurrency_AcceptsBaseCurrency()
        {
            var storeId = Guid.NewGuid();
            await using var context = CreateContext();
            SeedStore(context, storeId, "USD");
            var resolver = CreateResolver(context);

            var resolution = await resolver.ResolveAsync(storeId, "usd");

            Assert.Equal("USD", resolution.CurrencyCode);
            Assert.True(resolution.RequestedCurrencySupported);
            Assert.True(resolution.CheckoutCurrencyEnabled);
            Assert.Equal("base", resolution.Reason);
        }

        private static StorefrontWorkingCurrencyResolver CreateResolver(CommerceNodeDbContext context)
        {
            return new StorefrontWorkingCurrencyResolver(
                context,
                new StoreCurrencyResolver(context));
        }

        private static void SeedStore(CommerceNodeDbContext context, Guid storeId, string defaultCurrencyCode)
        {
            context.CommerceStores.Add(new CommerceStore
            {
                Id = storeId,
                StoreKey = "default",
                Name = "Default",
                DefaultCurrencyCode = defaultCurrencyCode,
            });
            context.SaveChanges();
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"storefront-working-currencies-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }
    }
}
