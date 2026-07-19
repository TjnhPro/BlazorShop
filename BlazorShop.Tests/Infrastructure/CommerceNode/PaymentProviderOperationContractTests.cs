namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Xunit;

    public sealed class PaymentProviderOperationContractTests
    {
        [Fact]
        public async Task DefaultUnsupportedOperation_ReturnsTypedFailure()
        {
            IStorefrontPaymentProvider provider = new MinimalProvider();

            var result = await provider.RefundAsync(CreateOperationRequest());

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("payment.operation_not_supported", result.Payload!.SafeFailureCode);
        }

        [Fact]
        public async Task CodCreatePaymentSession_ReturnsSynchronousCapturedRecommendation()
        {
            var provider = new CodStorefrontPaymentProvider();

            var result = await provider.CreatePaymentSessionAsync(CreateSessionRequest(PaymentMethodKeys.Cod));

            Assert.True(result.Success, result.Message);
            Assert.Equal(PaymentProviderActionTypes.None, result.Payload!.ActionType);
            Assert.Equal("captured", result.Payload.RecommendedState);
            Assert.Contains(PaymentMethodKeys.Cod, result.Payload.MetadataJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CodCreateHostedSession_ReturnsUnsupportedFailure()
        {
            var provider = new CodStorefrontPaymentProvider();

            var result = await provider.CreateHostedSessionAsync(CreateSessionRequest(PaymentMethodKeys.Cod));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
        }

        [Fact]
        public void ProviderDescriptor_SystemNameMatchesProviderKey()
        {
            IStorefrontPaymentProvider[] providers =
            [
                new CodStorefrontPaymentProvider(),
                new MinimalProvider(),
            ];

            foreach (var provider in providers)
            {
                Assert.Equal(provider.ProviderKey, provider.Descriptor.SystemName);
            }
        }

        [Fact]
        public void ProviderDescriptor_DoesNotExposeSecretOrCredentialProperties()
        {
            var propertyNames = typeof(PaymentProviderDescriptor)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.DoesNotContain(propertyNames, name => name.Contains("secret", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(propertyNames, name => name.Contains("password", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(propertyNames, name => name.Contains("credential", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(propertyNames, name => name.Contains("settings", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void CodDescriptor_MatchesOfflineCapturedOperationBehavior()
        {
            var provider = new CodStorefrontPaymentProvider();
            var descriptor = provider.Descriptor;

            Assert.Equal(PaymentMethodKeys.Cod, descriptor.SystemName);
            Assert.Equal("Cash on Delivery", descriptor.DisplayName);
            Assert.Equal(PaymentProviderMethodTypes.Offline, descriptor.MethodType);
            Assert.Equal(10, descriptor.DefaultDisplayOrder);
            Assert.True(descriptor.SupportsCapture);
            Assert.False(descriptor.RequiresWebhookSignature);
            Assert.True(descriptor.ActiveByDefault);
        }

        private static PaymentProviderOperationRequest CreateOperationRequest()
        {
            return new PaymentProviderOperationRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                PaymentMethodKeys.Stripe,
                PaymentMethodKeys.Stripe,
                12.34m,
                "USD",
                "operation-key");
        }

        private static CreatePaymentProviderSessionRequest CreateSessionRequest(string providerKey)
        {
            return new CreatePaymentProviderSessionRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                providerKey,
                providerKey,
                12.34m,
                "USD",
                "operation-key",
                [new PaymentProviderSessionLine(Guid.NewGuid(), "Product", 1, 12.34m)]);
        }

        private sealed class MinimalProvider : IStorefrontPaymentProvider
        {
            public string ProviderKey => "minimal";

            public PaymentProviderDescriptor Descriptor { get; } = new(
                "minimal",
                "Minimal",
                Description: null,
                IconUrl: null,
                DefaultDisplayOrder: 100,
                SupportedCurrencyCodes: [],
                SupportedCountryCodes: [],
                MinOrderTotal: null,
                MaxOrderTotal: null,
                PaymentProviderMethodTypes.Redirect,
                RecurringCapable: false,
                SupportsAuthorize: false,
                SupportsCapture: false,
                SupportsVoid: false,
                SupportsRefund: false,
                SupportsPartialRefund: false,
                RequiresWebhookSignature: false);

            public Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
                CreatePaymentProviderSessionRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
