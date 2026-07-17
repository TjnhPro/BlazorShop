namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.Extensions.Configuration;

    using Xunit;

    public sealed class PaymentWebhookSignatureVerifierTests
    {
        [Fact]
        public async Task VerifyAsync_WhenSignatureRequiredAndMissing_ReturnsValidationFailure()
        {
            var verifier = CreateVerifier("webhook-secret");

            var result = await verifier.VerifyAsync(PaymentMethodKeys.Stripe, "{\"id\":\"evt_1\"}", providerSignature: null);

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
        }

        [Fact]
        public async Task VerifyAsync_WhenSignatureRequiredAndInvalid_ReturnsValidationFailure()
        {
            var verifier = CreateVerifier("webhook-secret");

            var result = await verifier.VerifyAsync(PaymentMethodKeys.Stripe, "{\"id\":\"evt_1\"}", "sha256=invalid");

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
        }

        [Fact]
        public async Task VerifyAsync_WhenSignatureRequiredAndValid_ReturnsSuccess()
        {
            const string payload = "{\"id\":\"evt_1\"}";
            const string secret = "webhook-secret";
            var verifier = CreateVerifier(secret);

            var result = await verifier.VerifyAsync(PaymentMethodKeys.Stripe, payload, $"sha256={ComputeSignature(payload, secret)}");

            Assert.True(result.Success, result.Message);
        }

        [Fact]
        public async Task VerifyAsync_WhenProviderDoesNotRequireSignature_AllowsMissingSignature()
        {
            var verifier = CreateVerifier("webhook-secret");

            var result = await verifier.VerifyAsync(PaymentMethodKeys.Cod, "{\"id\":\"evt_1\"}", providerSignature: null);

            Assert.True(result.Success, result.Message);
        }

        private static PaymentWebhookSignatureVerifier CreateVerifier(string? secret)
        {
            var configurationValues = new Dictionary<string, string?>
            {
                ["Stripe:WebhookSecret"] = secret,
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationValues)
                .Build();

            return new PaymentWebhookSignatureVerifier(
                new PaymentProviderCapabilityRegistry([new FakePaymentProvider(PaymentMethodKeys.Stripe)]),
                configuration);
        }

        private static string ComputeSignature(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        }

        private sealed class FakePaymentProvider : IStorefrontPaymentProvider
        {
            public FakePaymentProvider(string providerKey)
            {
                this.ProviderKey = providerKey;
            }

            public string ProviderKey { get; }

            public Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
                CreatePaymentProviderSessionRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
