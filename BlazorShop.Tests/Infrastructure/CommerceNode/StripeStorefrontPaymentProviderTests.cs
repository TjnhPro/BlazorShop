namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Options;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;
    using BlazorShop.Infrastructure.Services;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    using Stripe.Checkout;

    using Xunit;

    public sealed class StripeStorefrontPaymentProviderTests
    {
        [Fact]
        public async Task CreateHostedSessionAsync_SendsAmountCurrencyAndIdempotencyMetadata()
        {
            var checkout = new FakeStripeCheckoutSessionService();
            var provider = CreateProvider(checkout, stripeSecret: "sk_test_123", clientBaseUrl: "https://shop.example.com");
            var request = CreateRequest();

            var result = await provider.CreateHostedSessionAsync(request);

            Assert.True(result.Success);
            Assert.Equal("redirect", result.Payload!.NextActionType);
            Assert.Equal("https://checkout.stripe.test/session", result.Payload.NextActionUrl);
            Assert.NotNull(checkout.Options);
            Assert.Equal("usd", checkout.Options!.LineItems[0].PriceData.Currency);
            Assert.Equal(1234, checkout.Options.LineItems[0].PriceData.UnitAmount);
            Assert.Equal("payment", checkout.Options.Mode);
            Assert.Equal(request.PaymentAttemptId.ToString(), checkout.Options.Metadata["paymentAttemptId"]);
            Assert.Equal("stripe-idem-key", checkout.Options.Metadata["idempotencyKey"]);
            Assert.Contains("paymentAttemptId=", checkout.Options.SuccessUrl, StringComparison.Ordinal);
            Assert.Contains("paymentAttemptId=", checkout.Options.CancelUrl, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CreatePaymentSessionAsync_MapsHostedSessionToRedirectOperation()
        {
            var checkout = new FakeStripeCheckoutSessionService();
            var provider = CreateProvider(checkout, stripeSecret: "sk_test_123", clientBaseUrl: "https://shop.example.com");

            var result = await provider.CreatePaymentSessionAsync(CreateRequest());

            Assert.True(result.Success, result.Message);
            Assert.Equal("redirect", result.Payload!.ActionType);
            Assert.Equal("https://checkout.stripe.test/session", result.Payload.ActionUrl);
            Assert.Equal("cs_test_123", result.Payload.ProviderSessionId);
            Assert.Equal("pi_test_123", result.Payload.ProviderReference);
            Assert.Equal("requires_action", result.Payload.RecommendedState);
        }

        [Fact]
        public async Task CreateHostedSessionAsync_UsesCurrencyDecimalDigitsForMinorUnits()
        {
            var checkout = new FakeStripeCheckoutSessionService();
            var provider = CreateProvider(checkout, stripeSecret: "sk_test_123", clientBaseUrl: "https://shop.example.com");

            var result = await provider.CreateHostedSessionAsync(CreateRequest(currencyCode: "JPY"));

            Assert.True(result.Success);
            Assert.NotNull(checkout.Options);
            Assert.Equal("jpy", checkout.Options!.LineItems[0].PriceData.Currency);
            Assert.Equal(12, checkout.Options.LineItems[0].PriceData.UnitAmount);
        }

        [Fact]
        public async Task CreateHostedSessionAsync_WhenStripeSecretMissing_ReturnsConflict()
        {
            var provider = CreateProvider(new FakeStripeCheckoutSessionService(), stripeSecret: "", clientBaseUrl: "https://shop.example.com");

            var result = await provider.CreateHostedSessionAsync(CreateRequest());

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.Conflict, result.ResponseType);
        }

        private static StripeStorefrontPaymentProvider CreateProvider(
            IStripeCheckoutSessionService checkout,
            string stripeSecret,
            string clientBaseUrl)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Stripe:SecretKey"] = stripeSecret,
                })
                .Build();

            return new StripeStorefrontPaymentProvider(
                checkout,
                new PaymentMinorUnitConverter(
                    new CurrencyMetadataService(),
                    new MoneyRoundingService(new CurrencyMetadataService())),
                Options.Create(new ClientAppOptions { BaseUrl = clientBaseUrl }),
                configuration);
        }

        private static CreatePaymentProviderSessionRequest CreateRequest(string currencyCode = "USD")
        {
            return new CreatePaymentProviderSessionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                PaymentMethodKeys.Stripe,
                PaymentMethodKeys.Stripe,
                12.34m,
                currencyCode,
                "stripe-idem-key",
                [new PaymentProviderSessionLine(Guid.NewGuid(), "T-Shirt", 1, 12.34m)]);
        }

        private sealed class FakeStripeCheckoutSessionService : IStripeCheckoutSessionService
        {
            public SessionCreateOptions? Options { get; private set; }

            public Task<Session> CreateAsync(SessionCreateOptions options, CancellationToken cancellationToken = default)
            {
                this.Options = options;
                return Task.FromResult(new Session
                {
                    Id = "cs_test_123",
                    Url = "https://checkout.stripe.test/session",
                    PaymentIntentId = "pi_test_123",
                });
            }
        }
    }
}
