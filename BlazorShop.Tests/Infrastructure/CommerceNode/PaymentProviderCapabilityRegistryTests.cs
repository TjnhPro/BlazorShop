namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class PaymentProviderCapabilityRegistryTests
    {
        [Fact]
        public void List_ReturnsCodOfflineStripeRedirectAndPayPalSkeleton()
        {
            var registry = new PaymentProviderCapabilityRegistry([new FakePaymentProvider(PaymentMethodKeys.Stripe)]);

            var capabilities = registry.List();

            var cod = Assert.Single(capabilities, item => item.SystemName == PaymentMethodKeys.Cod);
            Assert.True(cod.Installed);
            Assert.True(cod.Active);
            Assert.Equal(PaymentProviderMethodTypes.Offline, cod.MethodType);
            Assert.False(cod.RequiresWebhookSignature);

            var stripe = Assert.Single(capabilities, item => item.SystemName == PaymentMethodKeys.Stripe);
            Assert.True(stripe.Installed);
            Assert.True(stripe.Active);
            Assert.Equal(PaymentProviderMethodTypes.Redirect, stripe.MethodType);
            Assert.True(stripe.RequiresWebhookSignature);

            var paypal = Assert.Single(capabilities, item => item.SystemName == PaymentMethodKeys.PayPal);
            Assert.False(paypal.Installed);
            Assert.False(paypal.Active);
            Assert.Equal(PaymentProviderMethodTypes.Redirect, paypal.MethodType);
            Assert.True(paypal.RequiresWebhookSignature);
        }

        [Fact]
        public void Get_WhenProviderUnknown_ReturnsValidationFailure()
        {
            var registry = new PaymentProviderCapabilityRegistry([]);

            var result = registry.Get("bank_transfer");

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Payment provider is not supported.", result.Message);
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
