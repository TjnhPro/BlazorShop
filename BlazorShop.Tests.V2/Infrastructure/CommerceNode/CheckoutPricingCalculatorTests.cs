namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class CheckoutPricingCalculatorTests
    {
        [Fact]
        public async Task ResolveShippingOptionsAsync_WhenCartDoesNotRequireShipping_ReturnsShippingNotRequiredTotals()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            context.Products.Add(new Product
            {
                Id = productId,
                StoreId = storeId,
                Name = "Digital product",
                Slug = $"digital-{Guid.NewGuid():N}",
                Price = 12m,
                ShippingRequired = false,
                IsPublished = true,
            });
            await context.SaveChangesAsync();
            ShippingOptionsRequest? captured = null;
            var calculator = CreateCalculator(
                context,
                new FakeShippingCalculator(request =>
                {
                    captured = request;
                    return new ShippingCalculationResult(false, [], [], []);
                }));
            var cart = CreateCart(storeId, CreateLine(productId, quantity: 1, total: 12m));
            var session = CreateSession(storeId, cart.Id, currencyCode: "USD");

            var result = await calculator.ResolveShippingOptionsAsync(session, cart, selectedKey: null, CancellationToken.None);

            Assert.False(result.ShippingRequired);
            Assert.Empty(result.Options);
            Assert.Empty(result.Errors);
            Assert.NotNull(captured);
            Assert.False(Assert.Single(captured!.PackageLines).ShippingRequired);
        }

        [Fact]
        public async Task ResolveShippingOptionsAsync_WhenPhysicalCartHasSelectedOption_MapsSelectedShippingTotal()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            var calculator = CreateCalculator(
                context,
                new FakeShippingCalculator(_ => new ShippingCalculationResult(
                    true,
                    [CreateShippingOption("ground", 7.25m, "USD")],
                    [],
                    [])));
            var cart = CreateCart(storeId, CreateLine(productId, quantity: 2, total: 20m));
            var session = CreateSession(storeId, cart.Id, currencyCode: "USD");

            var result = await calculator.ResolveShippingOptionsAsync(session, cart, selectedKey: "ground", CancellationToken.None);

            var option = Assert.Single(result.Options);
            Assert.True(result.ShippingRequired);
            Assert.True(option.Selected);
            Assert.Equal(7.25m, option.Price);
            Assert.Equal("USD", option.CurrencyCode);
        }

        [Fact]
        public async Task ResolveShippingOptionsAsync_WhenShippingProviderFails_ReturnsValidationErrorData()
        {
            var storeId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            await using var context = CreateContext();
            var calculator = CreateCalculator(
                context,
                new FakeShippingCalculator(_ => new ServiceResponse<ShippingCalculationResult>(false, "Provider failed.")
                {
                    ResponseType = ServiceResponseType.Conflict,
                }));
            var cart = CreateCart(storeId, CreateLine(productId, quantity: 1, total: 20m));
            var session = CreateSession(storeId, cart.Id, currencyCode: "USD");

            var result = await calculator.ResolveShippingOptionsAsync(session, cart, selectedKey: null, CancellationToken.None);

            Assert.True(result.ShippingRequired);
            Assert.Empty(result.Options);
            Assert.Equal(["Provider failed."], result.Errors);
        }

        [Fact]
        public async Task CalculateShippingAsync_ConvertsBaseShippingRateToCheckoutCurrencyAndRounds()
        {
            await using var context = CreateContext();
            var calculator = CreateCalculator(
                context,
                new FakeShippingCalculator(_ => new ShippingCalculationResult(
                    true,
                    [CreateShippingOption("ground", 10m, "USD")],
                    [],
                    [])),
                new FakeMoneyConversionService("EUR", 0.91m));

            var result = await calculator.CalculateShippingAsync(
                Guid.NewGuid(),
                cartId: null,
                cartPublicId: null,
                CreateAddress(),
                checkoutCurrencyCode: "EUR",
                checkoutSubtotal: 25m,
                rateCurrencyCode: "USD",
                rateSubtotal: 25m,
                [new ShippingPackageLine(Guid.NewGuid(), null, 1, ShippingRequired: true, FreeShipping: false)],
                CancellationToken.None);

            var option = Assert.Single(result.Options);
            Assert.Equal(9.10m, option.Price);
            Assert.Equal("EUR", option.CurrencyCode);
        }

        [Fact]
        public async Task CalculateShippingTaxAsync_UsesCurrentZeroTaxPolicy()
        {
            await using var context = CreateContext();
            var calculator = CreateCalculator(context, new FakeShippingCalculator(_ => new ShippingCalculationResult(false, [], [], [])));

            var result = await calculator.CalculateShippingTaxAsync(
                Guid.NewGuid(),
                CreateAddress(),
                "USD",
                subtotal: 20m,
                shippingTotal: 7m,
                CancellationToken.None);

            Assert.Equal(0m, result.TaxTotal);
        }

        private static CheckoutPricingCalculator CreateCalculator(
            CommerceNodeDbContext context,
            IShippingCalculator shippingCalculator,
            IMoneyConversionService? moneyConversionService = null)
        {
            return new CheckoutPricingCalculator(
                context,
                new MoneyRoundingService(new CurrencyMetadataService()),
                moneyConversionService ?? new FakeMoneyConversionService("USD", 1m),
                shippingCalculator,
                new ZeroShippingTaxCalculator());
        }

        private static StorefrontCartSessionDto CreateCart(Guid storeId, StorefrontCartLineDto line)
        {
            return new StorefrontCartSessionDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                storeId,
                null,
                null,
                CartSessionStates.Active,
                1,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                [line],
                line.CurrencyCodeSnapshot ?? "USD",
                line.Quantity,
                line.LineTotal ?? 0m,
                DiscountTotal: 0m,
                ShippingEstimate: 0m,
                TaxEstimate: 0m,
                GrandTotal: line.LineTotal ?? 0m);
        }

        private static StorefrontCartLineDto CreateLine(Guid productId, int quantity, decimal total)
        {
            return new StorefrontCartLineDto(
                Guid.NewGuid(),
                productId,
                null,
                $"line-{Guid.NewGuid():N}",
                null,
                null,
                null,
                null,
                null,
                null,
                quantity,
                total / quantity,
                "USD",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                UnitPrice: total / quantity,
                LineSubtotal: total,
                LineTotal: total);
        }

        private static CheckoutSession CreateSession(Guid storeId, Guid cartId, string currencyCode)
        {
            return new CheckoutSession
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = storeId,
                CartSessionId = cartId,
                State = CheckoutSessionStates.Ready,
                CartVersion = 1,
                LastValidatedCartVersion = 1,
                CurrencyCode = currencyCode,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
            };
        }

        private static StorefrontCheckoutShippingAddressDto CreateAddress()
        {
            return new StorefrontCheckoutShippingAddressDto(
                "Buyer",
                "buyer@example.test",
                "123456789",
                "100 Main St",
                null,
                "Test City",
                "CA",
                "90001",
                "US");
        }

        private static ShippingOptionDto CreateShippingOption(string key, decimal rate, string currencyCode)
        {
            return new ShippingOptionDto(
                key,
                "internal",
                key,
                "Ground",
                null,
                rate,
                currencyCode,
                "3-5 days",
                [],
                [],
                null);
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"checkout-pricing-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class FakeShippingCalculator : IShippingCalculator
        {
            private readonly Func<ShippingOptionsRequest, ServiceResponse<ShippingCalculationResult>> calculate;

            public FakeShippingCalculator(Func<ShippingOptionsRequest, ShippingCalculationResult> calculate)
                : this(request => new ServiceResponse<ShippingCalculationResult>(true, "Shipping calculated.")
                {
                    Payload = calculate(request),
                    ResponseType = ServiceResponseType.Success,
                })
            {
            }

            public FakeShippingCalculator(Func<ShippingOptionsRequest, ServiceResponse<ShippingCalculationResult>> calculate)
            {
                this.calculate = calculate;
            }

            public Task<ServiceResponse<ShippingCalculationResult>> GetOptionsAsync(
                ShippingOptionsRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.calculate(request));
            }
        }

        private sealed class FakeMoneyConversionService : IMoneyConversionService
        {
            private readonly string targetCurrencyCode;
            private readonly decimal rate;

            public FakeMoneyConversionService(string targetCurrencyCode, decimal rate)
            {
                this.targetCurrencyCode = targetCurrencyCode;
                this.rate = rate;
            }

            public Task<ServiceResponse<MoneyConversionResult>> ConvertFromBaseAsync(
                Guid storeId,
                decimal amount,
                string targetCurrencyCode,
                CancellationToken cancellationToken = default)
            {
                if (!string.Equals(targetCurrencyCode, this.targetCurrencyCode, StringComparison.Ordinal))
                {
                    return Task.FromResult(new ServiceResponse<MoneyConversionResult>(false, "No exchange rate.")
                    {
                        ResponseType = ServiceResponseType.Conflict,
                    });
                }

                return Task.FromResult(new ServiceResponse<MoneyConversionResult>(true, "Converted.")
                {
                    Payload = new MoneyConversionResult(
                        amount,
                        "USD",
                        amount * this.rate,
                        targetCurrencyCode,
                        this.rate,
                        DateTimeOffset.UtcNow,
                        null,
                        "manual",
                        "test-rate"),
                    ResponseType = ServiceResponseType.Success,
                });
            }
        }
    }
}
