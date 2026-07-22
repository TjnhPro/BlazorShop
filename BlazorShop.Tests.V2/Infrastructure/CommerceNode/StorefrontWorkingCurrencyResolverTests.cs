namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.DTOs;
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
        public async Task ResolveAsync_WhenHintIsSupportedNonBaseWithoutRate_ReturnsBase()
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
            Assert.Equal("conversion_not_configured", resolution.Reason);
        }

        [Fact]
        public async Task ResolveAsync_WhenHintIsSupportedNonBaseWithRate_AcceptsWorkingCurrency()
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
            var resolver = CreateResolver(context, enabledTargetCurrencyCode: "EUR");

            var resolution = await resolver.ResolveAsync(storeId, "eur");

            Assert.Equal("EUR", resolution.CurrencyCode);
            Assert.Equal("USD", resolution.BaseCurrencyCode);
            Assert.Equal("EUR", resolution.RequestedCurrencyCode);
            Assert.True(resolution.RequestedCurrencySupported);
            Assert.True(resolution.CheckoutCurrencyEnabled);
            Assert.Equal("conversion_enabled", resolution.Reason);
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

        private static StorefrontWorkingCurrencyResolver CreateResolver(
            CommerceNodeDbContext context,
            string? enabledTargetCurrencyCode = null)
        {
            return new StorefrontWorkingCurrencyResolver(
                context,
                new StoreCurrencyResolver(context),
                new FakeMoneyConversionService(enabledTargetCurrencyCode));
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

        private sealed class FakeMoneyConversionService : IMoneyConversionService
        {
            private readonly string? enabledTargetCurrencyCode;

            public FakeMoneyConversionService(string? enabledTargetCurrencyCode)
            {
                this.enabledTargetCurrencyCode = enabledTargetCurrencyCode;
            }

            public Task<ServiceResponse<MoneyConversionResult>> ConvertFromBaseAsync(
                Guid storeId,
                decimal amount,
                string targetCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                if (!string.Equals(targetCurrencyCode, this.enabledTargetCurrencyCode, StringComparison.Ordinal))
                {
                    return Task.FromResult(new ServiceResponse<MoneyConversionResult>(false, "No active exchange rate is configured.")
                    {
                        ResponseType = ServiceResponseType.Conflict,
                    });
                }

                return Task.FromResult(new ServiceResponse<MoneyConversionResult>(true, "Currency conversion resolved.")
                {
                    Payload = new MoneyConversionResult(
                        amount,
                        "USD",
                        amount * 0.9m,
                        targetCurrencyCode,
                        0.9m,
                        DateTimeOffset.UtcNow,
                        null),
                    ResponseType = ServiceResponseType.Success,
                });
            }
        }
    }
}
